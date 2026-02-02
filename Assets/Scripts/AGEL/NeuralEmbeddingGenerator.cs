using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AGEL
{
    /// <summary>
    /// Neural embedding generator for episodes and symbols
    /// </summary>
    public class NeuralEmbeddingGenerator
    {
        private readonly int embeddingSize;
        private readonly Dictionary<string, float[]> cachedEmbeddings;
        private readonly System.Random random;
        
        // Simple word-to-vector mapping for basic semantic understanding
        private readonly Dictionary<string, float[]> wordVectors;
        
        public NeuralEmbeddingGenerator(int embeddingSize = 128)
        {
            this.embeddingSize = embeddingSize;
            this.cachedEmbeddings = new Dictionary<string, float[]>();
            this.random = new System.Random();
            this.wordVectors = new Dictionary<string, float[]>();
            
            InitializeWordVectors();
        }
        
        public float[] GenerateEmbedding(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new float[embeddingSize];
                
            // Check cache first
            if (cachedEmbeddings.TryGetValue(text, out var cached))
                return cached;
                
            // Generate new embedding
            var embedding = ComputeEmbedding(text);
            cachedEmbeddings[text] = embedding;
            return embedding;
        }
        
        private float[] ComputeEmbedding(string text)
        {
            var embedding = new float[embeddingSize];
            var words = text.ToLower()
                .Split(new[] { ' ', '(', ')', ',', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToArray();
                
            if (words.Length == 0)
            {
                // Random embedding for empty text
                for (int i = 0; i < embeddingSize; i++)
                {
                    embedding[i] = (float)(random.NextDouble() - 0.5) * 0.1f;
                }
                return embedding;
            }
            
            // Average word vectors
            int validWords = 0;
            foreach (var word in words)
            {
                if (wordVectors.TryGetValue(word, out var wordVec))
                {
                    for (int i = 0; i < embeddingSize; i++)
                    {
                        embedding[i] += wordVec[i];
                    }
                    validWords++;
                }
                else
                {
                    // Generate embedding based on character features
                    var charEmb = GenerateCharacterBasedEmbedding(word);
                    for (int i = 0; i < embeddingSize; i++)
                    {
                        embedding[i] += charEmb[i];
                    }
                    validWords++;
                }
            }
            
            // Normalize
            if (validWords > 0)
            {
                for (int i = 0; i < embeddingSize; i++)
                {
                    embedding[i] /= validWords;
                }
            }
            
            // Add positional encoding for structure
            AddPositionalEncoding(embedding, text);
            
            return embedding;
        }
        
        private void InitializeWordVectors()
        {
            // Initialize semantic word vectors for common AGEL concepts
            var concepts = new Dictionary<string, float[]>
            {
                ["harmful"] = GenerateSemanticVector(0.9f, 0.1f, 0.8f), // high danger, low safety, high intensity
                ["safe"] = GenerateSemanticVector(0.1f, 0.9f, 0.3f),    // low danger, high safety, low intensity
                ["mushroom"] = GenerateSemanticVector(0.8f, 0.2f, 0.7f), // dangerous item
                ["healing"] = GenerateSemanticVector(0.1f, 0.8f, 0.6f),  // beneficial
                ["enemy"] = GenerateSemanticVector(0.7f, 0.3f, 0.8f),    // threatening
                ["health"] = GenerateSemanticVector(0.3f, 0.7f, 0.9f),   // important for survival
                ["damage"] = GenerateSemanticVector(0.9f, 0.1f, 0.9f),   // very dangerous
                ["retreat"] = GenerateSemanticVector(0.2f, 0.8f, 0.5f),  // safety action
                ["attack"] = GenerateSemanticVector(0.6f, 0.4f, 0.8f),   // aggressive action
                ["consume"] = GenerateSemanticVector(0.4f, 0.6f, 0.7f),  // neutral action
                ["avoid"] = GenerateSemanticVector(0.1f, 0.9f, 0.4f),    // safety action
                ["move"] = GenerateSemanticVector(0.2f, 0.7f, 0.3f),     // basic action
                ["wait"] = GenerateSemanticVector(0.1f, 0.8f, 0.1f),     // safe action
                ["explore"] = GenerateSemanticVector(0.3f, 0.6f, 0.5f),  // neutral exploration
                ["fire"] = GenerateSemanticVector(0.9f, 0.1f, 0.9f),     // very dangerous
                ["poison"] = GenerateSemanticVector(0.9f, 0.1f, 0.8f),   // very harmful
                ["beneficial"] = GenerateSemanticVector(0.1f, 0.9f, 0.6f), // positive
                ["causes"] = GenerateSemanticVector(0.5f, 0.5f, 0.8f),   // causal relation
                ["prevents"] = GenerateSemanticVector(0.2f, 0.8f, 0.6f), // protective relation
            };
            
            foreach (var kvp in concepts)
            {
                wordVectors[kvp.Key] = kvp.Value;
            }
        }
        
        private float[] GenerateSemanticVector(float danger, float safety, float intensity)
        {
            var vector = new float[embeddingSize];
            
            // First few dimensions encode semantic features
            if (embeddingSize > 0) vector[0] = danger;      // danger level
            if (embeddingSize > 1) vector[1] = safety;      // safety level  
            if (embeddingSize > 2) vector[2] = intensity;   // intensity/importance
            
            // Fill remaining dimensions with structured noise based on semantic features
            for (int i = 3; i < embeddingSize; i++)
            {
                float phase = (float)(i * Math.PI / embeddingSize);
                vector[i] = (float)(
                    danger * Math.Sin(phase) + 
                    safety * Math.Cos(phase * 2) + 
                    intensity * Math.Sin(phase * 3) +
                    (random.NextDouble() - 0.5) * 0.1 // small random component
                );
            }
            
            // Normalize to unit vector
            float norm = (float)Math.Sqrt(vector.Sum(x => x * x));
            if (norm > 1e-6f)
            {
                for (int i = 0; i < embeddingSize; i++)
                {
                    vector[i] /= norm;
                }
            }
            
            return vector;
        }
        
        private float[] GenerateCharacterBasedEmbedding(string word)
        {
            var embedding = new float[embeddingSize];
            
            // Simple character-based features
            float vowelRatio = word.Count(c => "aeiou".Contains(c)) / (float)Math.Max(1, word.Length);
            float lengthFeature = Math.Min(word.Length / 10f, 1f);
            float firstCharFeature = (word[0] - 'a') / 25f;
            float lastCharFeature = (word[word.Length - 1] - 'a') / 25f;
            
            // Encode features into embedding
            if (embeddingSize > 3) embedding[3] = vowelRatio;
            if (embeddingSize > 4) embedding[4] = lengthFeature;
            if (embeddingSize > 5) embedding[5] = firstCharFeature;
            if (embeddingSize > 6) embedding[6] = lastCharFeature;
            
            // Fill remaining with character hash-based values
            int hash = word.GetHashCode();
            for (int i = 7; i < embeddingSize; i++)
            {
                hash = hash * 1103515245 + 12345; // Linear congruential generator
                embedding[i] = ((hash % 1000) / 1000f - 0.5f) * 0.2f;
            }
            
            return embedding;
        }
        
        private void AddPositionalEncoding(float[] embedding, string text)
        {
            // Add positional information based on text structure
            int colonPos = text.IndexOf(':');
            int parenPos = text.IndexOf('(');
            int commaCount = text.Count(c => c == ',');
            
            // Encode structural features in later dimensions
            int structStart = embeddingSize - 10;
            if (structStart > 0)
            {
                if (colonPos >= 0) embedding[structStart] = colonPos / (float)text.Length;
                if (parenPos >= 0) embedding[structStart + 1] = parenPos / (float)text.Length;
                embedding[structStart + 2] = Math.Min(commaCount / 5f, 1f);
            }
        }
        
        public void UpdateEmbedding(string text, float[] targetEmbedding, float learningRate = 0.01f)
        {
            if (!cachedEmbeddings.TryGetValue(text, out var currentEmbedding))
                return;
                
            // Simple gradient update
            for (int i = 0; i < embeddingSize; i++)
            {
                currentEmbedding[i] += learningRate * (targetEmbedding[i] - currentEmbedding[i]);
            }
        }
        
        public void ClearCache()
        {
            cachedEmbeddings.Clear();
        }
        
        public int GetCacheSize() => cachedEmbeddings.Count;
    }
}