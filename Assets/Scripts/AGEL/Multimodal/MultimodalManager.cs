using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AGEL.Multimodal
{
    [CreateAssetMenu(menuName = "AGEL/Multimodal Manager")]
    public class MultimodalManager : ScriptableObject
    {
        [Header("Model Clients")]
        public LLaVAClient llavaClient;
        public DeepSeekVLClient deepSeekClient;
        public GPT4oClient gpt4oClient;
        public GeminiClient geminiClient;
        
        [Header("Active Configuration")]
        public ModelType activeModelType = ModelType.LLaVA;
        public float temperature = 0.7f;
        public int maxTokens = 1024;
        public string systemPrompt = "You are a helpful AI assistant in a game environment.";
        
        private Dictionary<ModelType, IMModelClient> modelClients = new Dictionary<ModelType, IMModelClient>();
        private IMModelClient activeClient;
        
        public event Action<ModelType> OnModelChanged;
        
        public IEnumerator Initialize()
        {
            // Initialize all model clients
            modelClients[ModelType.LLaVA] = llavaClient;
            modelClients[ModelType.DeepSeekVL] = deepSeekClient;
            modelClients[ModelType.GPT4o] = gpt4oClient;
            modelClients[ModelType.Gemini25Pro] = geminiClient;
            
            // Initialize active client
            if (modelClients.TryGetValue(activeModelType, out var client))
            {
                activeClient = client;
                yield return activeClient.Initialize();
                Debug.Log($"Initialized {activeModelType} as active model");
            }
            else
            {
                Debug.LogError($"No client found for model type: {activeModelType}");
            }
        }
        
        public IEnumerator SwitchModel(ModelType newModelType)
        {
            if (newModelType == activeModelType)
                yield break;
                
            if (modelClients.TryGetValue(newModelType, out var newClient))
            {
                // Initialize new client if needed
                if (!newClient.IsInitialized)
                {
                    yield return newClient.Initialize();
                }
                
                // Switch active client
                activeClient = newClient;
                activeModelType = newModelType;
                
                Debug.Log($"Switched to model: {activeModelType}");
                OnModelChanged?.Invoke(activeModelType);
            }
            else
            {
                Debug.LogError($"Failed to switch to model: {newModelType} - Client not found");
            }
        }
        
        public IEnumerator GenerateResponse(
            MediaContent[] messages,
            Action<ModelResponse> onComplete,
            ModelType? modelType = null,
            float? temperature = null,
            int? maxTokens = null,
            string systemPrompt = null)
        {
            var targetModel = modelType ?? activeModelType;
            var temp = temperature ?? this.temperature;
            var tokens = maxTokens ?? this.maxTokens;
            var prompt = systemPrompt ?? this.systemPrompt;
            
            if (modelClients.TryGetValue(targetModel, out var client))
            {
                yield return client.GenerateResponseAsync(
                    messages,
                    response => {
                        onComplete?.Invoke(response);
                    },
                    temp,
                    tokens,
                    prompt
                );
            }
            else
            {
                onComplete?.Invoke(new ModelResponse 
                { 
                    success = false, 
                    error = $"No client available for model: {targetModel}" 
                });
            }
        }
        
        public IEnumerator GenerateTextResponse(
            string text,
            Action<ModelResponse> onComplete,
            ModelType? modelType = null,
            float? temperature = null,
            int? maxTokens = null,
            string systemPrompt = null)
        {
            var messages = new[]
            {
                new MediaContent 
                { 
                    type = MediaType.Text, 
                    data = text,
                    mimeType = "text/plain"
                }
            };
            
            yield return GenerateResponse(messages, onComplete, modelType, temperature, maxTokens, systemPrompt);
        }
        
        public IEnumerator GenerateImageResponse(
            string textPrompt,
            Texture2D image,
            Action<ModelResponse> onComplete,
            ModelType? modelType = null,
            float? temperature = null,
            int? maxTokens = null,
            string systemPrompt = null)
        {
            // Convert texture to base64
            byte[] imageBytes = image.EncodeToPNG();
            string base64Image = Convert.ToBase64String(imageBytes);
            
            var messages = new[]
            {
                new MediaContent 
                { 
                    type = MediaType.Text, 
                    data = textPrompt,
                    mimeType = "text/plain"
                },
                new MediaContent 
                { 
                    type = MediaType.Image, 
                    data = base64Image,
                    mimeType = "image/png"
                }
            };
            
            yield return GenerateResponse(messages, onComplete, modelType, temperature, maxTokens, systemPrompt);
        }
    }
}
