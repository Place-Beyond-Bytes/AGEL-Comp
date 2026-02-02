using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace AGEL
{
    public class OllamaClient : MonoBehaviour
    {
        public string model = "llava";
        public string apiUrl = "http://localhost:11434/api/generate";
        // PostHog settings
        public string posthogApiKey = ""; // Set in Inspector
        public string posthogHost = "https://app.posthog.com"; // Or your self-hosted instance
        public bool enableDebugLogs = false;

        public IEnumerator GenerateCompletion(string prompt, Action<string> onComplete, float timeout = 20f)
        {
            if (enableDebugLogs)
                Debug.Log("Prompt sent to Ollama:\n" + prompt);
            File.AppendAllText("Ollama_Prompts.txt", prompt + System.Environment.NewLine + "---" + System.Environment.NewLine);
            var payload = $"{{\"model\":\"{model}\",\"prompt\":\"{EscapeJson(prompt)}\",\"stream\":false}}";
            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                float startTime = Time.realtimeSinceStartup;
                var request = www.SendWebRequest();
                while (!request.isDone)
                {
                    if (Time.realtimeSinceStartup - startTime > timeout)
                    {
                        if (enableDebugLogs)
                            Debug.LogError("Ollama API request timed out.");
                        onComplete?.Invoke("");
                        yield break;
                    }
                    yield return null;
                }

                string response = www.downloadHandler.text;
                if (enableDebugLogs)
                    Debug.Log("Raw Ollama response:\n" + response);
                File.AppendAllText("Ollama_Responses.txt", response + System.Environment.NewLine + "---" + System.Environment.NewLine);
                string content = ExtractContentFromOllamaResponse(response);
                if (enableDebugLogs)
                    Debug.Log("Parsed LLM output:\n" + content);
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (enableDebugLogs)
                        Debug.LogError($"Ollama API Error: {www.error}\n{response}");
                    onComplete?.Invoke("");
                }
                else
                {
                    onComplete?.Invoke(content);
                }
                // Send event to PostHog (success or error)
                if (!string.IsNullOrEmpty(posthogApiKey))
                    StartCoroutine(SendPostHogEvent("ollama_llm_interaction", prompt, content));
            }
        }

        public IEnumerator GenerateCompletionWithImage(string prompt, Texture2D image, Action<string> onComplete, float timeout = 20f)
        {
            if (enableDebugLogs)
                Debug.Log("Prompt sent to Ollama (with image):\n" + prompt);
            byte[] pngData = image.EncodeToPNG();
            string base64Image = System.Convert.ToBase64String(pngData);
            // Log the base64 image for debugging
            File.AppendAllText("Ollama_Images_Base64.txt", base64Image + System.Environment.NewLine + "---" + System.Environment.NewLine);
            // Save the image to disk for reference
            File.WriteAllBytes($"Ollama_Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss_fff}.png", pngData);
            // Build multimodal payload
            string payload = $"{{\"model\":\"{model}\",\"prompt\":\"{EscapeJson(prompt)}\",\"images\":[\"{base64Image}\"],\"stream\":false}}";
            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                float startTime = Time.realtimeSinceStartup;
                var request = www.SendWebRequest();
                while (!request.isDone)
                {
                    if (Time.realtimeSinceStartup - startTime > timeout)
                    {
                        if (enableDebugLogs)
                            Debug.LogError("Ollama API request timed out.");
                        onComplete?.Invoke("");
                        yield break;
                    }
                    yield return null;
                }
                string response = www.downloadHandler.text;
                if (enableDebugLogs)
                    Debug.Log("Raw Ollama response (with image):\n" + response);
                File.AppendAllText("Ollama_Responses.txt", response + System.Environment.NewLine + "---" + System.Environment.NewLine);
                string content = ExtractContentFromOllamaResponse(response);
                if (enableDebugLogs)
                    Debug.Log("Parsed LLM output (with image):\n" + content);
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (enableDebugLogs)
                        Debug.LogError($"Ollama API Error: {www.error}\n{response}");
                    onComplete?.Invoke("");
                }
                else
                {
                    onComplete?.Invoke(content);
                }
                if (!string.IsNullOrEmpty(posthogApiKey))
                    StartCoroutine(SendPostHogEvent("ollama_llm_interaction_image", prompt, content));
            }
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (char.IsControl(c))
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private string ExtractContentFromOllamaResponse(string json)
        {
            const string marker = "\"response\":\"";
            int idx = json.IndexOf(marker);
            if (idx >= 0)
            {
                int start = idx + marker.Length;
                int end = json.IndexOf("\"", start);
                if (end > start)
                {
                    return json.Substring(start, end - start);
                }
            }
            return "";
        }

        // PostHog event sender
        public IEnumerator SendPostHogEvent(string eventName, string prompt, string response)
        {
            if (string.IsNullOrEmpty(posthogApiKey) || string.IsNullOrEmpty(posthogHost)) yield break;
            string url = posthogHost.TrimEnd('/') + "/capture/";
            string distinctId = SystemInfo.deviceUniqueIdentifier;
            string payload = $"{{\"api_key\":\"{posthogApiKey}\",\"event\":\"{eventName}\",\"distinct_id\":\"{distinctId}\",\"properties\":{{\"prompt\":\"{EscapeJson(prompt)}\",\"response\":\"{EscapeJson(response)}\"}}}}";
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"PostHog event failed: {www.error}\n{www.downloadHandler.text}");
                }
            }
        }
    }
} 