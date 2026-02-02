# Security & API Key Management Guide

⚠️ **CRITICAL**: This project integrates with external LLM APIs that require sensitive authentication credentials. Proper security practices are essential to prevent unauthorized access and financial exposure.

## 🚨 Risk Summary

| Risk | Impact | Severity |
|------|--------|----------|
| **Exposed API Keys** | Unauthorized API usage, billing fraud | 🔴 CRITICAL |
| **Hardcoded Credentials** | Public visibility in source control | 🔴 CRITICAL |
| **Analytics Keys** | Data leakage via PostHog | 🟡 MEDIUM |
| **Unencrypted Config Files** | Local machine compromise | 🟡 MEDIUM |

## ✅ What We've Done

The codebase has been structured with security in mind:

### 1. **Environment-Based Configuration**
- API keys are NOT hardcoded in scripts
- Configuration uses `public` fields in Unity Inspector (not persisted to source)
- Environment variables can be used at runtime

### 2. **.gitignore Configuration**
- `.env` and similar files are ignored
- Config files with sensitive data won't be accidentally committed
- See root `.gitignore` for full exclusion list

### 3. **API Client Architecture**
- `BaseModelClient` abstract class enforces consistent security patterns
- Each model client (GPT4o, Gemini, DeepSeek) follows secure initialization
- API keys are cleared from memory after use where possible

## 📋 API Keys Required

### By Service

| Service | Key Name | Endpoint | Notes |
|---------|----------|----------|-------|
| **OpenAI** | `openai_api_key` | https://api.openai.com/v1 | Required for GPT-4o |
| **Google Gemini** | `gemini_api_key` | https://generativelanguage.googleapis.com | Required for Gemini 2.5 Pro |
| **DeepSeek** | `deepseek_api_key` | https://api.deepseek.com | Optional, for DeepSeek VL |
| **PostHog** | `posthog_api_key` | https://app.posthog.com | Optional, for analytics only |

### Key Format & Location

#### OpenAI (GPT-4o)
- **Format**: `sk-proj-...` (starts with `sk-proj-`)
- **Get**: https://platform.openai.com/api-keys
- **Usage**: Sentence completion, multi-modal understanding

#### Google Gemini  
- **Format**: Long alphanumeric string
- **Get**: https://makersuite.google.com/app/apikey
- **Usage**: Vision & text understanding

#### DeepSeek
- **Format**: Starts with `sk-` or alphanumeric
- **Get**: https://platform.deepseek.com/api_keys
- **Usage**: Vision-language tasks (optional)

#### PostHog (Optional)
- **Format**: Alphanumeric project token
- **Get**: https://app.posthog.com/project/settings
- **Usage**: Telemetry only (can be disabled)

## 🔧 Configuration Methods

### Method 1: Environment Variables (Recommended for Production)

```bash
# Create .env file in project root (DO NOT COMMIT)
export OPENAI_API_KEY="sk-proj-your-key-here"
export GEMINI_API_KEY="your-gemini-key"
export DEEPSEEK_API_KEY="your-deepseek-key"
export POSTHOG_API_KEY="your-posthog-token"
export POSTHOG_HOST="https://app.posthog.com"
```

**Load in C# code:**
```csharp
public class SecureConfigLoader
{
    public static void LoadFromEnvironment()
    {
        string openaiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string geminiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        
        if (string.IsNullOrEmpty(openaiKey))
            Debug.LogWarning("OPENAI_API_KEY not set in environment!");
    }
}
```

### Method 2: ScriptableObject (Development Only)

Create a hidden configuration file (not tracked in git):

```csharp
[CreateAssetMenu(menuName = "AGEL/Credentials")]
public class APICredentials : ScriptableObject
{
    [SerializeField] private string openaiKey;
    [SerializeField] private string geminiKey;
    
    // Never log or serialize this
    public string GetOpenAIKey() => openaiKey;
    public string GetGeminiKey() => geminiKey;
}
```

**Store at**: `Assets/Resources/APICredentials.asset` (add to .gitignore!)

### Method 3: Unity Inspector (Development/Testing)

1. Select model client scriptable object in Inspector
2. Paste API key into `apiKey` field
3. **Note**: Not persisted to project files
4. Do NOT save scene/project with keys visible

## ⛔ What NOT To Do

### ❌ DO NOT:
```csharp
// NEVER - This exposes the key
public string apiKey = "sk-proj-xyz123";

// NEVER - This logs credentials
Debug.Log($"Using API key: {apiKey}");

// NEVER - Commit config files with keys
File.WriteAllText("config.json", JsonUtility.ToJson(credentialsWithKeys));

// NEVER - Hardcode in prefabs/scenes
// (Someone might accidentally save the scene)
myClient.apiKey = "sk-...";
```

### ❌ DO NOT Commit:
- `.env` files
- `*config*.json` with credentials
- `*secrets*` files
- `*credentials*` files
- Prefabs/Scenes with API keys in public fields

## ✅ DO:

```csharp
// ✅ DO - Use environment variables
string key = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(key))
{
    Debug.LogError("Missing OPENAI_API_KEY environment variable");
    return;
}
myClient.apiKey = key;
myClient.apiKey = null; // Clear after use

// ✅ DO - Validate before use
if (!IsInitialized || string.IsNullOrEmpty(apiKey))
{
    Debug.LogError("Client not properly initialized");
    yield break;
}

// ✅ DO - Use secure string handling
private string _apiKey; // Private field
public bool SetApiKey(string key)
{
    if (string.IsNullOrEmpty(key)) return false;
    _apiKey = key;
    return true;
}
```

## 🔐 API Security Practices

### For Each Model Client:

#### 1. **Authentication Header**
```csharp
// ✅ Secure way (in BaseModelClient)
public override IEnumerator Initialize()
{
    if (string.IsNullOrEmpty(apiKey))
    {
        Debug.LogError($"{ModelType} API key is not set!");
        IsInitialized = false;
        yield break;
    }
    
    defaultHeaders = new Dictionary<string, string>
    {
        { "Authorization", $"Bearer {apiKey}" },
        { "Content-Type", "application/json" }
    };
    
    IsInitialized = true;
}
```

#### 2. **Request Construction**
```csharp
// ✅ Never log the full request with API key
var request = CreateRequest(jsonPayload);
// ❌ DON'T: Debug.Log($"Request: {request}");
Debug.Log("API request sent (payload logged separately)");
```

#### 3. **Error Handling**
```csharp
// ✅ Good - Generic error message
if (www.result != UnityWebRequest.Result.Success)
{
    Debug.LogError($"API Error: {www.responseCode}");
    // Don't expose full response that might contain key details
    yield break;
}

// ❌ Bad - Exposes credentials in error
Debug.LogError($"Failed to authenticate: {www.downloadHandler.text}");
```

#### 4. **Memory Cleanup**
```csharp
// ✅ Clear sensitive data when done
public void Cleanup()
{
    if (defaultHeaders != null)
        defaultHeaders.Clear();
    apiKey = null; // Or secure string clearing
    IsInitialized = false;
}
```

## 🛡️ PostHog Analytics Security

The project includes optional PostHog integration for telemetry:

**Files**: 
- `Assets/Scripts/AGEL/OllamaClient.cs` (lines 168-180)

**What Gets Sent**:
- Event name (e.g., "llm_inference_complete")
- Prompt text (SENSITIVE!)
- Response text (SENSITIVE!)
- Distinct ID (device hash)

### ⚠️ Disable if Using Sensitive Data
```csharp
public class OllamaClient : MonoBehaviour
{
    // Set to empty to disable PostHog
    public string posthogApiKey = ""; // ← Leave empty to disable
    public string posthogHost = ""; // ← Leave empty to disable
}
```

**Or configure in Inspector**:
1. Select OllamaClient
2. Leave `posthogApiKey` empty
3. PostHog will be disabled

## 🚀 Deployment Checklist

### Before Publishing/Sharing:

- [ ] All API keys removed from repository
- [ ] `.env` file created and added to `.gitignore`
- [ ] No hard-coded credentials in any C# files
- [ ] `.env.example` file with placeholder values created
- [ ] Documentation includes security guide (this file)
- [ ] API Configuration guide created
- [ ] All ScriptableObject configs cleared of keys
- [ ] Scenes cleaned of any visible API key references
- [ ] Git history checked (use `git log -p` to search)

### Check for Leaked Keys:
```bash
# Search for common patterns
grep -r "sk-proj-" Assets/
grep -r "Bearer " Assets/
grep -r "apiKey" Assets/ | grep -v ".meta" | grep "="

# Search git history
git log -p | grep -i "api.?key\|sk-proj"
```

## 🔄 Rotation & Updates

### If an API key is compromised:

1. **Immediately revoke** the key in the service dashboard
2. **Generate a new key**
3. **Update `.env` file** (never committed, so only local)
4. **Notify team** if shared repository
5. **Audit logs** to see what was accessed

### Key Rotation Schedule:
- **Production**: Every 30 days
- **Development**: Every 90 days or per compromise
- **After publication**: Before sharing repository

## 📚 References

### Service Documentation:
- [OpenAI API Keys](https://platform.openai.com/docs/guides/authentication)
- [Google Gemini Authentication](https://ai.google.dev/tutorials/setup)
- [DeepSeek API Docs](https://platform.deepseek.com/docs)
- [PostHog Integration](https://posthog.com/docs)

### Security Standards:
- [OWASP API Security](https://owasp.org/www-project-api-security/)
- [Secure Credential Management](https://owasp.org/www-community/attacks/Credential_stuffing)
- [Environment Variable Best Practices](https://12factor.net/config)

## 📞 Security Issues

If you discover a security vulnerability:
1. **DO NOT** open a public issue
2. Email: security@uni-due.de
3. Include: description, reproduction steps, impact assessment

---

