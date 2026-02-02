using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AGEL
{
    /// <summary>
    /// Enhanced episodic memory system with vector embeddings and similarity search
    /// </summary>
    public class EnhancedEpisodicMemory : IDisposable
    {
        private readonly List<Episode> episodes;
        private readonly int maxSize;
        private int currentIndex;
        private readonly NeuralEmbeddingGenerator embeddingGenerator;
        private readonly Dictionary<int, float[]> episodeEmbeddings;
        private readonly int embeddingSize = 128;
        private readonly float similarityThreshold = 0.7f;
        private readonly int maxSearchResults = 5;
        
        // For contrastive learning
        private readonly Queue<Episode> recentEpisodes;
        private readonly int contrastiveBatchSize = 16;
        private readonly float learningRate = 0.01f;
        
        public int Count => episodes.Count;
        
        public EnhancedEpisodicMemory(int size, int embeddingDim = 128)
        {
            maxSize = size;
            embeddingSize = embeddingDim;
            episodes = new List<Episode>(size);
            episodeEmbeddings = new Dictionary<int, float[]>();
            embeddingGenerator = new NeuralEmbeddingGenerator(embeddingSize);
            recentEpisodes = new Queue<Episode>(contrastiveBatchSize * 2);
            currentIndex = 0;
        }
        
        public void Record(Episode episode)
        {
            if (episode == null) return;
            
            // Generate embedding for the episode
            var embedding = embeddingGenerator.GenerateEmbedding(episode.ToString());
            
            // Check for similar episodes to avoid duplicates
            if (episodes.Count > 0)
            {
                var mostSimilar = FindSimilarEpisodes(embedding, 1, similarityThreshold).FirstOrDefault();
                if (mostSimilar != null)
                {
                    // Update existing similar episode instead of adding duplicate
                    int index = episodes.IndexOf(mostSimilar);
                    episodes[index] = episode; // Update the episode
                    episodeEmbeddings[episode.GetHashCode()] = embedding; // Update embedding
                    
                    // Add to recent episodes for contrastive learning
                    UpdateRecentEpisodes(episode);
                    return;
                }
            }
            
            // Add new episode
            if (episodes.Count < maxSize)
            {
                episodes.Add(episode);
                episodeEmbeddings[episode.GetHashCode()] = embedding;
            }
            else
            {
                // Remove oldest episode
                var oldestEpisode = episodes[currentIndex];
                episodeEmbeddings.Remove(oldestEpisode.GetHashCode());
                
                // Add new episode
                episodes[currentIndex] = episode;
                episodeEmbeddings[episode.GetHashCode()] = embedding;
                currentIndex = (currentIndex + 1) % maxSize;
            }
            
            // Add to recent episodes for contrastive learning
            UpdateRecentEpisodes(episode);
            
            // Perform contrastive learning if we have enough recent episodes
            if (recentEpisodes.Count >= contrastiveBatchSize)
            {
                PerformContrastiveLearning();
            }
        }
        
        private void UpdateRecentEpisodes(Episode episode)
        {
            recentEpisodes.Enqueue(episode);
            if (recentEpisodes.Count > contrastiveBatchSize * 2)
            {
                recentEpisodes.Dequeue();
            }
        }
        
        public Episode GetRecentEpisode()
        {
            if (episodes.Count == 0)
                return null;
                
            if (episodes.Count < maxSize)
            {
                return episodes[episodes.Count - 1];
            }
            else
            {
                int recentIndex = (currentIndex - 1 + maxSize) % maxSize;
                return episodes[recentIndex];
            }
        }
        
        public List<Episode> FindSimilarEpisodes(string query, int maxResults = 5, float minSimilarity = 0.5f)
        {
            var queryEmbedding = embeddingGenerator.GenerateEmbedding(query);
            return FindSimilarEpisodes(queryEmbedding, maxResults, minSimilarity);
        }
        
        public List<Episode> FindSimilarEpisodes(float[] queryEmbedding, int maxResults = 5, float minSimilarity = 0.5f)
        {
            var results = new List<(Episode episode, float score)>();
            
            foreach (var episode in episodes)
            {
                if (episode == null) continue;
                
                if (episodeEmbeddings.TryGetValue(episode.GetHashCode(), out var episodeEmbedding))
                {
                    float similarity = CosineSimilarity(queryEmbedding, episodeEmbedding);
                    if (similarity >= minSimilarity)
                    {
                        results.Add((episode, similarity));
                    }
                }
            }
            
            // Sort by similarity score (descending) and take top results
            return results
                .OrderByDescending(r => r.score)
                .Take(maxResults)
                .Select(r => r.episode)
                .ToList();
        }
        
        public List<Episode> FindContrastingEpisodes(Episode target, int maxResults = 3)
        {
            if (!episodeEmbeddings.TryGetValue(target.GetHashCode(), out var targetEmbedding))
                return new List<Episode>();
                
            // Find episodes with low similarity to the target
            var results = episodes
                .Where(e => e != null && e != target)
                .Select(e => (episode: e, similarity: episodeEmbeddings.TryGetValue(e.GetHashCode(), out var emb) ? 
                    CosineSimilarity(targetEmbedding, emb) : 0))
                .OrderBy(x => x.similarity) // Order by least similar first
                .Take(maxResults)
                .Select(x => x.episode)
                .ToList();
                
            return results;
        }
        
        public List<Episode> GetEpisodesByTimeWindow(TimeSpan window, int maxResults = 10)
        {
            var now = DateTime.Now;
            return episodes
                .Where(e => e != null && (now - e.Timestamp) <= window)
                .OrderByDescending(e => e.Timestamp)
                .Take(maxResults)
                .ToList();
        }
        
        public Dictionary<string, object> GetMemoryStatistics()
        {
            return new Dictionary<string, object>
            {
                ["total_episodes"] = episodes.Count,
                ["memory_capacity"] = maxSize,
                ["embedding_dimension"] = embeddingSize,
                ["avg_similarity"] = CalculateAverageSimilarity(),
                ["recent_activity"] = recentEpisodes.Count
            };
        }
        
        private float CalculateAverageSimilarity()
        {
            if (episodes.Count < 2) return 0f;
            
            float totalSimilarity = 0;
            int comparisons = 0;
            var episodeList = episodes.Where(e => e != null).ToList();
            
            for (int i = 0; i < episodeList.Count; i++)
            {
                if (!episodeEmbeddings.TryGetValue(episodeList[i].GetHashCode(), out var emb1)) continue;
                
                for (int j = i + 1; j < episodeList.Count; j++)
                {
                    if (episodeEmbeddings.TryGetValue(episodeList[j].GetHashCode(), out var emb2))
                    {
                        totalSimilarity += CosineSimilarity(emb1, emb2);
                        comparisons++;
                    }
                }
            }
            
            return comparisons > 0 ? totalSimilarity / comparisons : 0f;
        }
        
        private void PerformContrastiveLearning()
        {
            try
            {
                // Sample positive and negative pairs from recent episodes
                var batch = recentEpisodes.Take(contrastiveBatchSize).ToList();
                if (batch.Count < 2) return;
                
                // For each episode in batch, find a positive (similar) and negative (dissimilar) example
                foreach (var anchor in batch)
                {
                    if (!episodeEmbeddings.TryGetValue(anchor.GetHashCode(), out var anchorEmbedding)) continue;
                    
                    // Find positive example (most similar in recent episodes)
                    var positive = batch
                        .Where(e => e != anchor)
                        .OrderByDescending(e => 
                            episodeEmbeddings.TryGetValue(e.GetHashCode(), out var emb) ? 
                            CosineSimilarity(anchorEmbedding, emb) : 0)
                        .FirstOrDefault();
                        
                    if (positive == null) continue;
                    
                    // Find negative example (least similar in memory)
                    var negative = episodes
                        .Where(e => e != null && e != anchor && !batch.Contains(e))
                        .OrderBy(e =>
                            episodeEmbeddings.TryGetValue(e.GetHashCode(), out var emb) ?
                            CosineSimilarity(anchorEmbedding, emb) : 1)
                        .FirstOrDefault();
                        
                    if (negative == null) continue;
                    
                    // Update embeddings using contrastive loss
                    UpdateEmbeddings(anchor, positive, negative);
                }
                
                // Clear recent episodes after processing
                recentEpisodes.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in contrastive learning: {e.Message}");
            }
        }
        
        private void UpdateEmbeddings(Episode anchor, Episode positive, Episode negative)
        {
            if (!episodeEmbeddings.TryGetValue(anchor.GetHashCode(), out var anchorEmb) ||
                !episodeEmbeddings.TryGetValue(positive.GetHashCode(), out var posEmb) ||
                !episodeEmbeddings.TryGetValue(negative.GetHashCode(), out var negEmb))
                return;
                
            // Simple contrastive update (in practice, use a proper contrastive loss)
            for (int i = 0; i < embeddingSize; i++)
            {
                // Move anchor closer to positive
                anchorEmb[i] += learningRate * (posEmb[i] - anchorEmb[i]);
                posEmb[i] += learningRate * (anchorEmb[i] - posEmb[i]);
                
                // Move anchor away from negative
                anchorEmb[i] -= learningRate * (negEmb[i] - anchorEmb[i]) * 0.5f;
                negEmb[i] -= learningRate * (anchorEmb[i] - negEmb[i]) * 0.5f;
                
                // Clamp to unit sphere
                float norm = Mathf.Sqrt(anchorEmb.Sum(x => x * x));
                if (norm > 1e-6f)
                {
                    for (int j = 0; j < embeddingSize; j++)
                    {
                        anchorEmb[j] /= norm;
                    }
                }
            }
        }
        
        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length || a.Length == 0)
                return 0;
                
            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            
            return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB) + 1e-6f);
        }
        
        public void Dispose()
        {
            episodes.Clear();
            episodeEmbeddings.Clear();
            recentEpisodes.Clear();
        }
    }
}
