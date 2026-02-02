using UnityEngine;

namespace AGEL.Experiments
{
    [System.Serializable]
    public class LLMConfiguration
    {
        public string name;
        public string modelName;
        public float temperature;
        public int maxTokens;
        public bool useNTP;  // Whether to use Neural Theorem Prover
        public bool useILP;  // Whether to use Inductive Logic Programming
        public float ntpThreshold;  // Confidence threshold for NTP
        
        // Any other LLM-specific parameters
        public int beamWidth = 1;
        public float topP = 0.9f;
        public float frequencyPenalty = 0.0f;
        public float presencePenalty = 0.0f;
        
        public LLMConfiguration(string name, string modelName, float temperature = 0.7f, 
                              bool useNTP = true, bool useILP = true, float ntpThreshold = 0.7f)
        {
            this.name = name;
            this.modelName = modelName;
            this.temperature = temperature;
            this.useNTP = useNTP;
            this.useILP = useILP;
            this.ntpThreshold = ntpThreshold;
        }
    }
}
