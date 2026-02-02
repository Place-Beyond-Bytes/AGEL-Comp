using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AGEL.Multimodal
{
    [CreateAssetMenu(menuName = "AGEL/Model Clients/LLaVA Client")]
    public class LLaVAClient : BaseModelClient
    {
        public override ModelType ModelType => ModelType.LLaVA;
        
        [Header("LLaVA Specific")]
        public string modelName = "llava-v1.6-mistral-7b";
        public string ollamaEndpoint = "http://localhost:11434/api/generate";
        
        [Serializable]
        private class LLaVARequest
        {
            public string model;
            public string prompt;
            public bool stream = false;
            public List<LLaVAMessage> messages;
        }
        
        [Serializable]
        private class LLaVAMessage
        {
            public string role;
            public string content;
            public List<LLaVAMedia> images;
        }
        
        [Serializable]
        private class LLaVAMedia
        {
            public string type = "image";
            public string data; // base64 encoded
        }
        
        [Serializable]
        private class LLaVAResponse
        {
            public string response;
            public bool done;
            public int total_duration;
            public int load_duration;
            public int prompt_eval_count;
            public int eval_count;
            public int eval_duration;
        }
        
        public override IEnumerator Initialize()
        {
            // Check if Ollama is running
            using (var request = UnityWebRequest.Get("http://localhost:11434/api/tags"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    IsInitialized = true;
                    Debug.Log($"LLaVA client initialized with model: {modelName}");
                }
                else
                {
                    Debug.LogError($"Failed to connect to Ollama: {request.error}");
                    IsInitialized = false;
                }
            }
        }
        
        protected override object PrepareRequestData(MediaContent[] messages, float temperature, int maxTokens, string systemPrompt)
        {
            var request = new LLaVARequest
            {
                model = modelName,
                messages = new List<LLaVAMessage>()
            };
            
            // Add system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                request.messages.Add(new LLaVAMessage
                {
                    role = "system",
                    content = systemPrompt,
                    images = null
                });
            }
            
            // Process each message
            foreach (var msg in messages)
            {
                var llavaMsg = new LLaVAMessage
                {
                    role = "user",
                    content = "",
                    images = new List<LLaVAMedia>()
                };
                
                if (msg.type == MediaType.Text)
                {
                    llavaMsg.content = msg.data;
                }
                else if (msg.type == MediaType.Image)
                {
                    // For LLaVA, we can include both text and image in the same message
                    llavaMsg.content = "[Image]\n" + (string.IsNullOrEmpty(msg.data) ? "" : "Caption: " + msg.data);
                    llavaMsg.images.Add(new LLaVAMedia { data = msg.data });
                }
                
                request.messages.Add(llavaMsg);
            }
            
            return request;
        }
        
        protected override UnityWebRequest CreateRequest(string jsonPayload)
        {
            var request = new UnityWebRequest(ollamaEndpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }
        
        protected override ModelResponse ProcessResponse(string jsonResponse)
        {
            try
            {
                var response = JsonUtility.FromJson<LLaVAResponse>(jsonResponse);
                return new ModelResponse
                {
                    success = response.done,
                    content = response.response,
                    tokensUsed = response.eval_count,
                    processingTime = response.total_duration / 1000f // Convert ms to seconds
                };
            }
            catch (Exception e)
            {
                return new ModelResponse
                {
                    success = false,
                    error = $"Failed to parse response: {e.Message}"
                };
            }
        }
    }
}
