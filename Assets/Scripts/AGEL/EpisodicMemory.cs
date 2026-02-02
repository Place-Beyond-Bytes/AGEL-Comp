using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    public class EpisodicMemory
    {
        private List<Episode> episodes;
        private int maxSize;
        private int currentIndex;
        
        public EpisodicMemory(int size)
        {
            maxSize = size;
            episodes = new List<Episode>();
            currentIndex = 0;
        }
        
        public void Record(Episode episode)
        {
            if (episodes.Count < maxSize)
            {
                // Buffer not full yet, just add
                episodes.Add(episode);
            }
            else
            {
                // Buffer is full, replace oldest episode (circular buffer)
                episodes[currentIndex] = episode;
                currentIndex = (currentIndex + 1) % maxSize;
            }
        }
        
        public Episode GetRecentEpisode()
        {
            if (episodes.Count == 0)
                return null;
                
            if (episodes.Count < maxSize)
            {
                // Buffer not full, return the last added episode
                return episodes[episodes.Count - 1];
            }
            else
            {
                // Buffer is full, return the episode before the current index
                int recentIndex = (currentIndex - 1 + maxSize) % maxSize;
                return episodes[recentIndex];
            }
        }
        
        public List<Episode> GetAllEpisodes()
        {
            return new List<Episode>(episodes);
        }
        
        public List<Episode> GetRecentEpisodes(int count)
        {
            if (count >= episodes.Count)
                return GetAllEpisodes();
                
            List<Episode> recentEpisodes = new List<Episode>();
            
            if (episodes.Count < maxSize)
            {
                // Buffer not full, return the last 'count' episodes
                int startIndex = Mathf.Max(0, episodes.Count - count);
                for (int i = startIndex; i < episodes.Count; i++)
                {
                    recentEpisodes.Add(episodes[i]);
                }
            }
            else
            {
                // Buffer is full, return episodes in chronological order
                for (int i = 0; i < count; i++)
                {
                    int index = (currentIndex - count + i + maxSize) % maxSize;
                    recentEpisodes.Add(episodes[index]);
                }
            }
            
            return recentEpisodes;
        }
        
        public Episode GetEpisodeAt(int index)
        {
            if (index < 0 || index >= episodes.Count)
                return null;
                
            if (episodes.Count < maxSize)
            {
                return episodes[index];
            }
            else
            {
                // Buffer is full, calculate the actual index
                int actualIndex = (currentIndex - episodes.Count + index + maxSize) % maxSize;
                return episodes[actualIndex];
            }
        }
        
        public bool IsEmpty()
        {
            return episodes.Count == 0;
        }
        
        public bool IsFull()
        {
            return episodes.Count >= maxSize;
        }
        
        public int GetSize()
        {
            return episodes.Count;
        }
        
        public int GetMaxSize()
        {
            return maxSize;
        }
        
        public void Clear()
        {
            episodes.Clear();
            currentIndex = 0;
        }
        
        // Get episodes with specific feedback characteristics
        public List<Episode> GetEpisodesWithNegativeFeedback()
        {
            List<Episode> negativeEpisodes = new List<Episode>();
            
            foreach (var episode in episodes)
            {
                if (episode.feedback.healthChange < 0 || episode.feedback.damageTaken > 0)
                {
                    negativeEpisodes.Add(episode);
                }
            }
            
            return negativeEpisodes;
        }
        
        public List<Episode> GetEpisodesWithPositiveFeedback()
        {
            List<Episode> positiveEpisodes = new List<Episode>();
            
            foreach (var episode in episodes)
            {
                if (episode.feedback.healthChange > 0 && episode.feedback.success)
                {
                    positiveEpisodes.Add(episode);
                }
            }
            
            return positiveEpisodes;
        }
        
        public List<Episode> GetEpisodesByIntensity(float minIntensity)
        {
            List<Episode> intenseEpisodes = new List<Episode>();
            
            foreach (var episode in episodes)
            {
                if (episode.feedback.intensity >= minIntensity)
                {
                    intenseEpisodes.Add(episode);
                }
            }
            
            return intenseEpisodes;
        }
        
        // Get episodes within a time window
        public List<Episode> GetEpisodesInTimeWindow(float timeWindow)
        {
            List<Episode> recentEpisodes = new List<Episode>();
            float currentTime = Time.time;
            
            foreach (var episode in episodes)
            {
                if (currentTime - episode.timestamp <= timeWindow)
                {
                    recentEpisodes.Add(episode);
                }
            }
            
            return recentEpisodes;
        }
        
        public override string ToString()
        {
            return $"EpisodicMemory[{episodes.Count}/{maxSize} episodes]";
        }
    }
} 