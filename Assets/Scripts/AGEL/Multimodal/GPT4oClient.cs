using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AGEL.Multimodal
{
    [CreateAssetMenu(menuName = "AGEL/Model Clients/GPT-4o Client")]
    public class GPT4oClient : BaseModelClient
    {
        public override ModelType ModelType => ModelType.GPT4o;
        
        [Header("GPT-4o Settings")]
        public string modelName = "gpt-4o";
        public string apiEndpoint = "https://api.openai.com/v1/chat/completions";
        
        [Serializable]
        private class GPTMessage
        {
            public string role;
            public List<GPTContent> content;
        }
        
        [Serializable]
        private class GPTContent
        {
            public string type; // "text" or "image_url"
            public string text;
            public GPTImageUrl image_url;
        }
        
        [Serializable]
        private class GPTImageUrl
        {
            public string url; // base64 encoded image
        }
        
        [Serializable]
        private class GPTRequest
        {
            public string model;
            public List<GPTMessage> messages;
            public float temperature;
            public int max_tokens;
            public bool stream = false;
        }
        
        [Serializable]
        private class GPTResponse
        {
            public List<GPTChoice> choices;
            public GPTUsage usage;
        }
        
        [Serializable]
        private class GPTChoice
        {
            public GPTMessage message;
        }
        
        [Serializable]
        private class GPTUsage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
        
        public override IEnumerator Initialize()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("OpenAI API key is not set!");
                IsInitialized = false;
                yield break;
            }
            
            defaultHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {apiKey}" },
                { "OpenAI-Beta", "2024-06-13" } // Required for GPT-4o
            };
            
            IsInitialized = true;
            Debug.Log($"GPT-4o client initialized with model: {modelName}");
        }
        
        protected override object PrepareRequestData(MediaContent[] messages, float temperature, int maxTokens, string systemPrompt)
        {
            var request = new GPTRequest
            {
                model = modelName,
                temperature = temperature,
                max_tokens = maxTokens,
                messages = new List<GPTMessage>()
            };
            
            // Add system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                request.messages.Add(new GPTMessage
                {
                    role = "system",
                    content = new List<GPTContent>
                    {
                        new GPTContent { type = "text", text = systemPrompt }
                    }
                });
            }
            
            // Process user messages
            foreach (var msg in messages)
            {
                var content = new List<GPTContent>();
                
                if (msg.type == MediaType.Text)
                {
                    content.Add(new GPTContent { type = "text", text = msg.data });
                }
                else if (msg.type == MediaType.Image)
                {
                    content.Add(new GPTContent
                    {
                        type = "image_url",
                        image_url = new GPTImageUrl 
                        { 
                            url = $"data:{msg.mimeType};base64,{msg.data}" 
                        }
                    });
                }
                
                request.messages.Add(new GPTMessage
                {
                    role = "user",
                    content = content
                });
            }
            
            return request;
        }
        
        protected override UnityWebRequest CreateRequest(string jsonPayload)
        {
            var request = new UnityWebRequest(apiEndpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
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
                var response = JsonUtility.FromJson<GPTResponse>(jsonResponse);
                if (response?.choices == null || response.choices.Count == 0)
                {
                    return new ModelResponse
                    {
                        success = false,
                        error = "No valid response from GPT-4o API"
                    };
                }
                
                var content = response.choices[0].message.content;
                string textResponse = "";
                
                // Concatenate all text content
                foreach (var item in content)
                {
                    if (item.type == "text")
                    {
                        textResponse += item.text + "\n";
                    }
                }
                
                return new ModelResponse
                {
                    success = true,
                    content = textResponse.Trim(),
                    tokensUsed = response.usage?.total_tokens ?? 0
                };
            }
            catch (Exception e)
            {
                return new ModelResponse
                {
                    success = false,
                    error = $"Failed to parse GPT-4o response: {e.Message}"
                };
            }
        }
    }
}
