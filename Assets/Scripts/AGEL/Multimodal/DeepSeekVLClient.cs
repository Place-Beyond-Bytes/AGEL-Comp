using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AGEL.Multimodal
{
    [CreateAssetMenu(menuName = "AGEL/Model Clients/DeepSeek VL Client")]
    public class DeepSeekVLClient : BaseModelClient
    {
        public override ModelType ModelType => ModelType.DeepSeekVL;
        
        [Header("DeepSeek VL Settings")]
        public string modelName = "deepseek-vl-7b-chat";
        public string apiEndpoint = "https://api.deepseek.com/v1/chat/completions";
        
        [Serializable]
        private class DeepSeekMessage
        {
            public string role;
            public List<DeepSeekContent> content;
        }
        
        [Serializable]
        private class DeepSeekContent
        {
            public string type; // "text" or "image_url"
            public string text;
            public DeepSeekImageUrl image_url;
        }
        
        [Serializable]
        private class DeepSeekImageUrl
        {
            public string url; // base64 encoded image
        }
        
        [Serializable]
        private class DeepSeekRequest
        {
            public string model;
            public List<DeepSeekMessage> messages;
            public float temperature;
            public int max_tokens;
        }
        
        [Serializable]
        private class DeepSeekResponse
        {
            public List<DeepSeekChoice> choices;
            public DeepSeekUsage usage;
        }
        
        [Serializable]
        private class DeepSeekChoice
        {
            public DeepSeekMessage message;
        }
        
        [Serializable]
        private class DeepSeekUsage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
        
        public override IEnumerator Initialize()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("DeepSeek API key is not set!");
                IsInitialized = false;
                yield break;
            }
            
            defaultHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {apiKey}" },
                { "Content-Type", "application/json" }
            };
            
            IsInitialized = true;
            Debug.Log($"DeepSeek VL client initialized with model: {modelName}");
        }
        
        protected override object PrepareRequestData(MediaContent[] messages, float temperature, int maxTokens, string systemPrompt)
        {
            var request = new DeepSeekRequest
            {
                model = modelName,
                temperature = temperature,
                max_tokens = maxTokens,
                messages = new List<DeepSeekMessage>()
            };
            
            // Add system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                request.messages.Add(new DeepSeekMessage
                {
                    role = "system",
                    content = new List<DeepSeekContent>
                    {
                        new DeepSeekContent { type = "text", text = systemPrompt }
                    }
                });
            }
            
            // Process user messages
            foreach (var msg in messages)
            {
                var content = new List<DeepSeekContent>();
                
                if (msg.type == MediaType.Text)
                {
                    content.Add(new DeepSeekContent 
                    { 
                        type = "text", 
                        text = msg.data 
                    });
                }
                else if (msg.type == MediaType.Image)
                {
                    content.Add(new DeepSeekContent
                    {
                        type = "image_url",
                        image_url = new DeepSeekImageUrl 
                        { 
                            url = $"data:{msg.mimeType};base64,{msg.data}" 
                        }
                    });
                }
                
                request.messages.Add(new DeepSeekMessage
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
                var response = JsonUtility.FromJson<DeepSeekResponse>(jsonResponse);
                if (response?.choices == null || response.choices.Count == 0)
                {
                    return new ModelResponse
                    {
                        success = false,
                        error = "No valid response from DeepSeek API"
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
                    error = $"Failed to parse DeepSeek response: {e.Message}"
                };
            }
        }
    }
}
