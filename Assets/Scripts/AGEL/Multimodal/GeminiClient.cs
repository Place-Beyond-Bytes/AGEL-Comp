using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AGEL.Multimodal
{
    [CreateAssetMenu(menuName = "AGEL/Model Clients/Gemini 2.5 Pro Client")]
    public class GeminiClient : BaseModelClient
    {
        public override ModelType ModelType => ModelType.Gemini25Pro;
        
        [Header("Gemini Settings")]
        public string modelName = "gemini-2.5-pro";
        public string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent";
        
        [Serializable]
        private class GeminiContent
        {
            public List<GeminiPart> parts;
            public string role;
        }
        
        [Serializable]
        private class GeminiPart
        {
            public string text;
            public GeminiInlineData inlineData;
        }
        
        [Serializable]
        private class GeminiInlineData
        {
            public string mimeType;
            public string data;
        }
        
        [Serializable]
        private class GeminiRequest
        {
            public List<GeminiContent> contents;
            public GeminiGenerationConfig generationConfig;
        }
        
        [Serializable]
        private class GeminiGenerationConfig
        {
            public float temperature;
            public int maxOutputTokens;
        }
        
        [Serializable]
        private class GeminiResponse
        {
            public List<GeminiCandidate> candidates;
            public GeminiUsageMetadata usageMetadata;
        }
        
        [Serializable]
        private class GeminiCandidate
        {
            public GeminiContent content;
        }
        
        [Serializable]
        private class GeminiUsageMetadata
        {
            public int promptTokenCount;
            public int candidatesTokenCount;
            public int totalTokenCount;
        }
        
        public override IEnumerator Initialize()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("Gemini API key is not set!");
                IsInitialized = false;
                yield break;
            }
            
            // Append API key to endpoint
            apiEndpoint = $"{apiEndpoint}?key={apiKey}";
            
            defaultHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };
            
            IsInitialized = true;
            Debug.Log($"Gemini 2.5 Pro client initialized with model: {modelName}");
        }
        
        protected override object PrepareRequestData(MediaContent[] messages, float temperature, int maxTokens, string systemPrompt)
        {
            var request = new GeminiRequest
            {
                contents = new List<GeminiContent>(),
                generationConfig = new GeminiGenerationConfig
                {
                    temperature = temperature,
                    maxOutputTokens = maxTokens
                }
            };
            
            var content = new GeminiContent
            {
                role = "user",
                parts = new List<GeminiPart>()
            };
            
            // Add system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                content.parts.Add(new GeminiPart 
                { 
                    text = $"[System: {systemPrompt}]\n" 
                });
            }
            
            // Process messages
            foreach (var msg in messages)
            {
                if (msg.type == MediaType.Text)
                {
                    content.parts.Add(new GeminiPart { text = msg.data });
                }
                else if (msg.type == MediaType.Image)
                {
                    content.parts.Add(new GeminiPart
                    {
                        inlineData = new GeminiInlineData
                        {
                            mimeType = msg.mimeType,
                            data = msg.data
                        }
                    });
                }
            }
            
            request.contents.Add(content);
            return request;
        }
        
        protected override UnityWebRequest CreateRequest(string jsonPayload)
        {
            var request = new UnityWebRequest(apiEndpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            foreach (var header in defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            return request;
        }
        
        protected override ModelResponse ProcessResponse(string jsonResponse)
        {
            try
            {
                var response = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
                if (response?.candidates == null || response.candidates.Count == 0)
                {
                    return new ModelResponse
                    {
                        success = false,
                        error = "No valid response from Gemini API"
                    };
                }
                
                var parts = response.candidates[0].content.parts;
                string textResponse = "";
                
                // Concatenate all text parts
                foreach (var part in parts)
                {
                    if (!string.IsNullOrEmpty(part.text))
                    {
                        textResponse += part.text + "\n";
                    }
                }
                
                return new ModelResponse
                {
                    success = true,
                    content = textResponse.Trim(),
                    tokensUsed = response.usageMetadata?.totalTokenCount ?? 0
                };
            }
            catch (Exception e)
            {
                return new ModelResponse
                {
                    success = false,
                    error = $"Failed to parse Gemini response: {e.Message}"
                };
            }
        }
    }
}
