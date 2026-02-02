# API Configuration Guide

This guide provides step-by-step instructions for configuring external API integrations in AGEL-Comp.

## Table of Contents
1. [OpenAI GPT-4o Setup](#openai-gpt-4o-setup)
2. [Google Gemini Setup](#google-gemini-setup)
3. [DeepSeek VL Setup](#deepseek-vl-setup)
4. [Ollama Local Setup](#ollama-local-setup)
5. [PostHog Analytics Setup (Optional)](#posthog-analytics-setup-optional)
6. [Testing Configuration](#testing-configuration)

---

## OpenAI GPT-4o Setup

### Prerequisites
- OpenAI account: https://platform.openai.com/
- Active billing enabled
- API access enabled (check account status)

### Step 1: Create/Obtain API Key

1. Go to https://platform.openai.com/api-keys
2. Click "Create new secret key"
3. Copy the key (format: `sk-proj-...`)
4. **Store safely** - you won't see it again!

### Step 2: Configure in Project

#### Option A: Environment Variables (Recommended)

**On Windows PowerShell:**
```powershell
[System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-proj-your-key-here", "User")
```

**On macOS/Linux:**
```bash
export OPENAI_API_KEY="sk-proj-your-key-here"
# Add to ~/.bashrc or ~/.zshrc to persist
```

**Or create `.env` file in project root:**
```env
OPENAI_API_KEY=sk-proj-your-key-here
```

#### Option B: Unity Inspector

1. **In Unity Editor**, go to `Assets/Scripts/AGEL/Multimodal/`
2. Find **GPT4oClient** scriptable object (or create one)
3. Select it in the Inspector
4. Paste your API key into the `apiKey` field
5. Leave as is (don't save the scene!)

### Step 3: Verify Configuration

**In Unity, create a test script:**

```csharp
using UnityEngine;
using AGEL.Multimodal;
using System.Collections;

public class GPT4oTest : MonoBehaviour
{
    public void TestConnection()
    {
        StartCoroutine(TestGPT4o());
    }
    
    private IEnumerator TestGPT4o()
    {
        var client = GetComponent<GPT4oClient>();
        yield return client.Initialize();
        
        if (client.IsInitialized)
            Debug.Log("✅ GPT-4o connected!");
        else
            Debug.LogError("❌ GPT-4o connection failed");
    }
}
```

### Troubleshooting

| Error | Solution |
|-------|----------|
| `"OpenAI API key is not set"` | Check environment variable or Inspector field |
| `"401 Unauthorized"` | Invalid/expired API key - regenerate at https://platform.openai.com/api-keys |
| `"429 Rate Limited"` | Reduce request frequency or upgrade account |
| `"Insufficient quota"` | Add billing payment method |

### Cost Management

**Monitor usage:**
- https://platform.openai.com/account/billing/overview
- GPT-4o costs ~$0.03 per 1K input tokens, $0.06 per 1K output tokens

**Set spending limits:**
- https://platform.openai.com/account/billing/limits
- Recommended: Set monthly limit to avoid surprises

---

## Google Gemini Setup

### Prerequisites
- Google Account
- Access to Google AI Studio: https://makersuite.google.com/
- No billing required for Gemini 2.5 Pro API (free tier available)

### Step 1: Create API Key

1. Go to https://makersuite.google.com/app/apikey
2. Click "Create API key"
3. Select "Create API key in new project"
4. Copy the generated key
5. Keep it safe!

### Step 2: Configure in Project

#### Option A: Environment Variable

**Windows PowerShell:**
```powershell
[System.Environment]::SetEnvironmentVariable("GEMINI_API_KEY", "your-key-here", "User")
```

**macOS/Linux:**
```bash
export GEMINI_API_KEY="your-key-here"
```

**Or in `.env`:**
```env
GEMINI_API_KEY=your-gemini-api-key
```

#### Option B: Unity Inspector

1. In `Assets/Scripts/AGEL/Multimodal/`
2. Find or create **GeminiClient** scriptable object
3. Select it in Inspector
4. Paste API key in `apiKey` field

### Step 3: Enable Gemini API (If Required)

1. Go to https://console.cloud.google.com/
2. Select or create a project
3. Enable "Generative Language API"
4. The API key from makersuite.google.com should work automatically

### Step 4: Test Connection

```csharp
public void TestGemini()
{
    StartCoroutine(TestGeminiConnection());
}

private IEnumerator TestGeminiConnection()
{
    var client = GetComponent<GeminiClient>();
    yield return client.Initialize();
    
    if (client.IsInitialized)
        Debug.Log("✅ Gemini 2.5 Pro connected!");
    else
        Debug.LogError("❌ Gemini connection failed");
}
```

### Troubleshooting

| Error | Solution |
|-------|----------|
| `"API key not set"` | Verify environment variable or Inspector field |
| `"Invalid API key"` | Regenerate at https://makersuite.google.com/app/apikey |
| `"RESOURCE_EXHAUSTED"` | Free tier limit reached; wait or upgrade |
| `"FAILED_PRECONDITION"` | API not enabled in project |

### Cost
- **Free tier**: 15 requests per minute
- **Paid tier**: More requests, check Google Cloud pricing

---

## DeepSeek VL Setup

### Prerequisites
- DeepSeek account: https://platform.deepseek.com/
- API billing enabled
- Sufficient credits

### Step 1: Generate API Key

1. Go to https://platform.deepseek.com/api_keys
2. Click "Create new API key"
3. Copy and secure the key (format: starts with `sk-`)
4. Note the organization ID if using teams

### Step 2: Configure

#### Option A: Environment Variable

**Windows:**
```powershell
[System.Environment]::SetEnvironmentVariable("DEEPSEEK_API_KEY", "sk-...", "User")
```

**Unix:**
```bash
export DEEPSEEK_API_KEY="sk-..."
```

#### Option B: Unity Inspector

1. Find **DeepSeekVLClient** in `Assets/Scripts/AGEL/Multimodal/`
2. Select in Inspector
3. Paste API key

### Step 3: Configure Endpoint (If Custom)

The default endpoint is `https://api.deepseek.com/v1/chat/completions`

If using a proxy or custom endpoint:
```csharp
deepseekClient.apiEndpoint = "https://your-proxy.com/deepseek/v1/chat/completions";
```

### Step 4: Test

```csharp
var client = GetComponent<DeepSeekVLClient>();
yield return client.Initialize();
Debug.Log(client.IsInitialized ? "✅ DeepSeek ready" : "❌ DeepSeek failed");
```

---

## Ollama Local Setup

Ollama runs on your local machine - no API key needed!

### Prerequisites
- **Ollama installed**: https://ollama.ai/download
- **Port 11434 available** (default)
- **At least 8GB RAM** for most models

### Step 1: Install & Run Ollama

**Windows/macOS/Linux:**
```bash
# Download and install from https://ollama.ai/

# Start Ollama server
ollama serve

# In another terminal, pull a model
ollama pull llava  # For vision+language
# or
ollama pull mistral  # For text-only
```

### Step 2: Configure in Project

Edit **OllamaClient** in Inspector or code:

```csharp
public class OllamaClient : MonoBehaviour
{
    public string model = "llava";  // or "mistral", "neural-chat", etc.
    public string apiUrl = "http://localhost:11434/api/generate";
    // Keep these empty for local operation
    public string posthogApiKey = "";
    public string posthogHost = "";
}
```

### Step 3: Test

```csharp
var ollama = GetComponent<OllamaClient>();
StartCoroutine(ollama.GenerateCompletion(
    "Hello, what is 2+2?",
    response => Debug.Log($"Ollama: {response}")
));
```

### Available Models

Common models for vision/language tasks:

| Model | Size | Speed | Vision | Notes |
|-------|------|-------|--------|-------|
| `llava` | 4.7GB | Fast | ✅ Yes | Recommended for experiments |
| `mistral` | 4.1GB | Very Fast | ❌ No | Text-only |
| `neural-chat` | 4.1GB | Fast | ❌ No | Good reasoning |
| `llama2` | 3.8GB | Fast | ❌ No | General purpose |
| `wizardlm2` | 4.1GB | Medium | ❌ No | Code-focused |

Pull any model:
```bash
ollama pull <model-name>
```

### Troubleshooting

| Error | Solution |
|-------|----------|
| `"Connection refused"` | Start Ollama: `ollama serve` |
| `"Model not found"` | Pull the model: `ollama pull llava` |
| `"Out of memory"` | Reduce model size or close other apps |
| `"Request timeout"` | Model is running slow; increase timeout |

---

## PostHog Analytics Setup (Optional)

**Note**: PostHog is OPTIONAL and used only for telemetry. You can safely disable it.

### To Enable PostHog

1. Create account at https://posthog.com/
2. Create a new project
3. Get your Project API Key and Host

### Step 1: Configure

```csharp
public class OllamaClient : MonoBehaviour
{
    public string posthogApiKey = "phc_your-key-here";
    public string posthogHost = "https://app.posthog.com"; // or self-hosted
    public bool enableDebugLogs = false;
}
```

Or environment variables:
```env
POSTHOG_API_KEY=phc_your-key-here
POSTHOG_HOST=https://app.posthog.com
```

### To Disable PostHog

Simply leave the keys empty:
```csharp
public string posthogApiKey = "";
public string posthogHost = "";
```

⚠️ **Warning**: PostHog will send prompt and response text! Only enable if:
- Using test/public data
- Have privacy agreements in place
- Running locally only

---

## Testing Configuration

### Unified Test Script

```csharp
using UnityEngine;
using AGEL.Multimodal;
using System.Collections;

public class APIConfigurationTest : MonoBehaviour
{
    [SerializeField] private MultimodalManager multimodalManager;
    
    public void TestAllConnections()
    {
        StartCoroutine(TestAll());
    }
    
    private IEnumerator TestAll()
    {
        Debug.Log("🔍 Testing all API connections...\n");
        
        // Test Ollama (local)
        yield return TestOllama();
        
        // Test GPT-4o (if key configured)
        yield return TestGPT4o();
        
        // Test Gemini (if key configured)
        yield return TestGemini();
        
        // Test DeepSeek (if key configured)
        yield return TestDeepSeek();
        
        Debug.Log("\n✅ Configuration test complete!");
    }
    
    private IEnumerator TestOllama()
    {
        Debug.Log("Testing Ollama (local)...");
        var ollama = GetComponent<OllamaClient>();
        
        var complete = false;
        ollama.GenerateCompletion("Say 'Hello from Ollama'", 
            response => {
                Debug.Log($"  ✅ Ollama: {response}");
                complete = true;
            });
        
        yield return new WaitUntil(() => complete);
    }
    
    private IEnumerator TestGPT4o()
    {
        Debug.Log("Testing GPT-4o...");
        var gptClient = multimodalManager.GetModelClient(ModelType.GPT4o);
        
        if (gptClient == null)
        {
            Debug.LogWarning("  ⚠️ GPT-4o client not configured");
            yield break;
        }
        
        yield return gptClient.Initialize();
        
        if (gptClient.IsInitialized)
            Debug.Log("  ✅ GPT-4o connected!");
        else
            Debug.LogError("  ❌ GPT-4o connection failed");
    }
    
    private IEnumerator TestGemini()
    {
        Debug.Log("Testing Gemini 2.5 Pro...");
        var geminiClient = multimodalManager.GetModelClient(ModelType.Gemini25Pro);
        
        if (geminiClient == null)
        {
            Debug.LogWarning("  ⚠️ Gemini client not configured");
            yield break;
        }
        
        yield return geminiClient.Initialize();
        
        if (geminiClient.IsInitialized)
            Debug.Log("  ✅ Gemini connected!");
        else
            Debug.LogError("  ❌ Gemini connection failed");
    }
    
    private IEnumerator TestDeepSeek()
    {
        Debug.Log("Testing DeepSeek VL...");
        var deepseekClient = multimodalManager.GetModelClient(ModelType.DeepSeekVL);
        
        if (deepseekClient == null)
        {
            Debug.LogWarning("  ⚠️ DeepSeek client not configured");
            yield break;
        }
        
        yield return deepseekClient.Initialize();
        
        if (deepseekClient.IsInitialized)
            Debug.Log("  ✅ DeepSeek connected!");
        else
            Debug.LogError("  ❌ DeepSeek connection failed");
    }
}
```

### Manual Testing Checklist

- [ ] **Ollama**: Run locally, can generate text
- [ ] **GPT-4o**: API key valid, can initialize
- [ ] **Gemini**: API key valid, can initialize  
- [ ] **DeepSeek**: API key valid, can initialize (if configured)
- [ ] **PostHog**: Events sent (check dashboard if enabled)

---

## Quick Reference

### Environment Variable Names
```
OPENAI_API_KEY          # OpenAI GPT-4o
GEMINI_API_KEY          # Google Gemini 2.5 Pro
DEEPSEEK_API_KEY        # DeepSeek VL
POSTHOG_API_KEY         # PostHog (optional)
POSTHOG_HOST            # PostHog (optional)
```

### Default Endpoints
```
Ollama:    http://localhost:11434/api/generate
OpenAI:    https://api.openai.com/v1/chat/completions
Gemini:    https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent
DeepSeek:  https://api.deepseek.com/v1/chat/completions
PostHog:   https://app.posthog.com/capture/
```

### Recommended Model Combinations

**For Research (High Quality):**
```csharp
activeModelType = ModelType.GPT4o;  // Planning
verifyWith = ModelType.Gemini25Pro; // Verification
```

**For Local Testing (No API Keys):**
```csharp
activeModelType = ModelType.Ollama;  // Local LLM
noExternalAPIs = true;
```

**For Cost Optimization:**
```csharp
activeModelType = ModelType.DeepSeekVL;  // Cheaper alternative
```

---

**Last Updated**: February 2, 2026  
**Questions?** Check [SECURITY.md](SECURITY.md) for security best practices
