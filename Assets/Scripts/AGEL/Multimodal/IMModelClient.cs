using System;
using System.Collections;
using UnityEngine;

namespace AGEL.Multimodal
{
    public enum ModelType
    {
        LLaVA,
        DeepSeekVL,
        GPT4o,
        Gemini25Pro
    }

    public enum MediaType
    {
        Text,
        Image,
        Audio
    }

    [Serializable]
    public class MediaContent
    {
        public MediaType type;
        public string data; // Base64 encoded for images/audio, plain text for text
        public string mimeType; // e.g., "image/png", "text/plain"
    }

    [Serializable]
    public class ModelResponse
    {
        public string content;
        public string error;
        public bool success;
        public float processingTime;
        public int tokensUsed;
    }

    public interface IMModelClient
    {
        IEnumerator GenerateResponseAsync(
            MediaContent[] messages,
            Action<ModelResponse> onComplete,
            float temperature = 0.7f,
            int maxTokens = 1024,
            string systemPrompt = null
        );

        ModelType ModelType { get; }
        bool IsInitialized { get; }
        IEnumerator Initialize();
    }
}
