using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace AGEL.Multimodal
{
    public abstract class BaseModelClient : IMModelClient
    {
        public abstract ModelType ModelType { get; }
        public bool IsInitialized { get; protected set; }
        public abstract IEnumerator Initialize();

        [Header("Base Settings")]
        [Tooltip("Base URL for the model API")]
        public string baseUrl = "";
        
        [Tooltip("API key (if required)")]
        public string apiKey = "";
        
        [Tooltip("Default model parameters")]
        public float defaultTemperature = 0.7f;
        public int defaultMaxTokens = 1024;
        
        [Header("Performance")]
        public float timeout = 30f;
        public int maxRetries = 2;
        
        protected Dictionary<string, string> defaultHeaders = new Dictionary<string, string>();
        
        public virtual IEnumerator GenerateResponseAsync(
            MediaContent[] messages,
            Action<ModelResponse> onComplete,
            float temperature = 0.7f,
            int maxTokens = 1024,
            string systemPrompt = null)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"{ModelType} client not initialized. Initializing...");
                yield return Initialize();
                
                if (!IsInitialized)
                {
                    onComplete?.Invoke(new ModelResponse 
                    { 
                        success = false, 
                        error = "Failed to initialize model client" 
                    });
                    yield break;
                }
            }

            // Convert messages to model-specific format
            var requestData = PrepareRequestData(messages, temperature, maxTokens, systemPrompt);
            string jsonPayload = JsonUtility.ToJson(requestData);
            
            // Make the API request
            using (UnityWebRequest request = CreateRequest(jsonPayload))
            {
                float startTime = Time.realtimeSinceStartup;
                int retryCount = 0;
                bool requestSucceeded = false;
                string errorMsg = "";
                
                while (retryCount <= maxRetries && !requestSucceeded)
                {
                    if (retryCount > 0)
                    {
                        Debug.Log($"Retry {retryCount} for {ModelType}...");
                        yield return new WaitForSeconds(1f * retryCount); // Exponential backoff
                    }
                    
                    var operation = request.SendWebRequest();
                    float requestStartTime = Time.realtimeSinceStartup;
                    
                    while (!operation.isDone)
                    {
                        if (Time.realtimeSinceStartup - requestStartTime > timeout)
                        {
                            request.Abort();
                            errorMsg = "Request timed out";
                            break;
                        }
                        yield return null;
                    }
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        requestSucceeded = true;
                        var response = ProcessResponse(request.downloadHandler.text);
                        response.processingTime = Time.realtimeSinceStartup - startTime;
                        onComplete?.Invoke(response);
                        yield break;
                    }
                    else
                    {
                        errorMsg = $"Error: {request.error}\nResponse: {request.downloadHandler?.text}";
                        retryCount++;
                    }
                }
                
                // If we get here, all retries failed
                onComplete?.Invoke(new ModelResponse 
                { 
                    success = false, 
                    error = $"Failed after {maxRetries + 1} attempts. Last error: {errorMsg}",
                    processingTime = Time.realtimeSinceStartup - startTime
                });
            }
        }
        
        protected abstract object PrepareRequestData(MediaContent[] messages, float temperature, int maxTokens, string systemPrompt);
        protected abstract UnityWebRequest CreateRequest(string jsonPayload);
        protected abstract ModelResponse ProcessResponse(string jsonResponse);
        
        protected string Base64Encode(string plainText) 
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        
        protected string Base64Decode(string base64EncodedData) 
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
