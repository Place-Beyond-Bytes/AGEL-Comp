# AGEL-Comp: A Neuro-Symbolic Framework for Compositional Generalization in Embodied Agents

![IntelliSys 2026](https://img.shields.io/badge/IntelliSys-2026-blue)
![License](https://img.shields.io/badge/License-Research-green)
![Status](https://img.shields.io/badge/Status-Active-brightgreen)

## 📋 Overview

AGEL-Comp is a novel neuro-symbolic architecture designed to address a critical limitation of Large Language Model (LLM)-based agents: **compositional generalization**—the ability to understand and produce novel combinations of known, primitive components.

### The Core Challenge
LLM-powered agents exhibit systemic failures when facing compositional challenges. They rely on statistical pattern matching rather than structured, causal understanding of the world. AGEL-Comp bridges this gap by combining:

1. **Symbolic Reasoning** - Explicit, interpretable world models
2. **Neural Flexibility** - Adaptive LLM-based planning
3. **Empirical Grounding** - Learning from interaction in simulated environments

## 🏗️ Architecture

### Key Components

```
┌─────────────────────────────────────────────────────────────┐
│                    AGEL-Comp Agent                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐      ┌──────────────┐      ┌───────────┐ │
│  │ Perception   │─────▶│  LLM Core    │◀─────│ Feedback  │ │
│  │ Module       │      │              │      │ Signals   │ │
│  └──────────────┘      └──────────────┘      └───────────┘ │
│                               │                              │
│                         ▼     ▼     ▼                        │
│                    ┌─────────────────┐                       │
│                    │  Hybrid Planner │                       │
│                    │   + Verifier    │                       │
│                    └─────────────────┘                       │
│                         │         │                          │
│          ┌──────────────┘         └──────────────┐           │
│          ▼                                       ▼           │
│    ┌──────────────┐                     ┌────────────────┐ │
│    │ LLM Planner  │                     │  NTP Verifier  │ │
│    │ (Generative) │                     │  (Symbolic)    │ │
│    └──────────────┘                     └────────────────┘ │
│                                                              │
│  ┌──────────────┐      ┌──────────────┐      ┌───────────┐ │
│  │Causal Program│      │  Episodic    │      │ Grounding │ │
│  │Graph (CPG)   │◀─────│  Memory      │◀─────│ Function  │ │
│  │ (World Model)│      │              │      │ (ILP)     │ │
│  └──────────────┘      └──────────────┘      └───────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Component Details

#### 1. **Perception Module**
- Translates raw environment state into structured percepts
- Ground literals representing entities and their states
- Feeds into LLM context

#### 2. **World Model (Causal Program Graph)**
- **Directed hypergraph** representation of procedural and causal knowledge
- **Nodes**: Grounded predicates (e.g., `is_harmful(X)`, `fire`)
- **Hyperedges**: Horn clauses functioning as sub-programs
- Supports hierarchical planning and targeted model revision

#### 3. **LLM Core**
- **LLM as Planner**: Generates candidate sub-goals from percepts and goals
- **Neural Theorem Prover (NTP) as Verifier**: Validates logical soundness
- Bridges creative generation with symbolic rigor

#### 4. **Grounding Function (ILP Engine)**
Two-stage grounding process:
- **Causal Attribution**: Solves credit assignment problem for prediction errors
- **Abstractive Induction**: Generalizes specific observations into reusable Horn clauses
- Transforms raw experience into symbolic rules

#### 5. **Episodic Memory**
- Fixed-size buffer storing recent experiences
- Each experience: `(State, Action, Feedback)` tuple
- Provides data for continuous learning

#### 6. **Action Module**
- Translates high-level textual plans into executable commands
- Direct interface with simulation environment API

## 🚀 Getting Started

### Prerequisites
- **Unity** 2022.3 LTS or later
- **C#** 10.0 or later
- **.NET Framework** 4.7.1+
- Python 3.8+ (for analysis scripts)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/ActionRPG-Quest-System.git
   cd ActionRPG-Quest-System/ActionRPG-Quest-System
   ```

2. **Install dependencies**
   ```bash
   # Install required Unity packages via Package Manager
   # Navigate to: Window > TextMesh Pro > Import TMP Essential Resources
   ```

3. **Configure API Keys** (see [API Configuration Guide](API_CONFIGURATION.md))
   ```bash
   # Copy the template
   cp .env.example .env
   # Edit with your API keys (DO NOT commit .env!)
   ```

4. **Open in Unity**
   - File > Open Project
   - Select the `ActionRPG-Quest-System` folder

## 📖 Usage

### Running an Experiment

#### Option 1: Interactive Mode (Recommended for Development)
1. Open the main scene: `Assets/Scenes/MainScene.unity`
2. Attach `AGELAgent` to the player GameObject
3. Configure experiment parameters in the Inspector
4. Press Play to start the agent

#### Option 2: Automated Experiments
```csharp
// Use AGELExperimentManager for batch processing
AGELExperimentManager manager = GetComponent<AGELExperimentManager>();
manager.RunExperiment(config);
```

#### Option 3: Command Line (Automated Testing)
```bash
unity -projectPath . -batchmode -executeMethod AGELExperiment.RunBatch -nographics
```

### Configuration Examples

#### Basic Configuration
```csharp
var config = new LLMConfiguration(
    name: "GPT-4o",
    modelName: "gpt-4o",
    temperature: 0.7f,
    useNTP: true,
    useILP: true,
    ntpThreshold: 0.7f
);
```

#### Multi-Model Setup
```csharp
// Use different models for planning vs verification
var plannerConfig = new LLMConfiguration("gpt-4o", "gpt-4o", 0.9f);
var verifierConfig = new LLMConfiguration("gpt-4-turbo", "gpt-4-turbo", 0.2f);
```

## 📊 Experimental Framework: Retro Quest

The **Retro Quest** environment is a custom 2D RPG simulation designed to systematically probe compositional generalization.

### Environment Features
- **Grid-based world** with dynamic entities
- **Rich action space** (movement, interaction, combat, dialogue)
- **Compositional goal structure** enabling systematic evaluation
- **Feedback signals** encoding outcome intensity and content

### Benchmark Scenarios

| Scenario | Focus | Metrics |
|----------|-------|---------|
| **Basic Composition** | Combining primitive actions | Success rate, plan length |
| **Hierarchical Goals** | Multi-step sub-goal decomposition | Depth accuracy, completeness |
| **Novel Combinations** | Zero-shot compositional transfer | Generalization gap, performance |
| **Adversarial Perturbations** | Robustness to world model changes | Adaptation speed, error recovery |

### Running Evaluations

```csharp
// See: Assets/Scripts/AGEL_Experiments/AGELQuestExperiment.cs

AGELQuestExperiment experiment = new AGELQuestExperiment();
experiment.RunCompositionBenchmark();
experiment.EvaluateSystematicity();
experiment.EvaluateProductivity();
```

## 📈 Results Summary

### Key Findings
- **Compositional Systematicity**: AGEL-Comp achieves >85% success on novel goal combinations
- **Sample Efficiency**: Learns interpretable rules from ~10 examples per rule
- **Robustness**: Maintains >90% performance after adversarial world model perturbations
- **Interpretability**: Generated rules are human-readable and logically sound

### Comparing to Baselines
- **Pure LLM**: ~45% on novel compositions (fails due to distribution shift)
- **Pure ILP**: ~60% on novel compositions (limited by symbolic expressiveness)
- **AGEL-Comp**: ~85% on novel compositions (synergistic neuro-symbolic approach)

### Data Files
- Detailed results: `comprehensive_results/`
- Rule database: `comprehensive_results/cpg_rules_database.csv`
- Evolution analysis: `comprehensive_results/cpg_evolution/`
- Visualizations: `comprehensive_results/plots/`

## 🔒 Security & API Keys

⚠️ **CRITICAL**: This repository interacts with external LLM APIs (OpenAI, Google Gemini, DeepSeek).

### Exposed APIs
The codebase includes integrations with:
- **OpenAI GPT-4o**: `Assets/Scripts/AGEL/Multimodal/GPT4oClient.cs`
- **Google Gemini 2.5 Pro**: `Assets/Scripts/AGEL/Multimodal/GeminiClient.cs`
- **DeepSeek VL**: `Assets/Scripts/AGEL/Multimodal/DeepSeekVLClient.cs`
- **Ollama (Local)**: `Assets/Scripts/AGEL/OllamaClient.cs`
- **PostHog Analytics**: `Assets/Scripts/AGEL/OllamaClient.cs` (optional telemetry)

### API Key Handling ⚠️

**DO NOT:**
- ❌ Commit `.env`, `config.json`, or any files with API keys
- ❌ Hardcode API keys in scripts
- ❌ Share API keys in issues or pull requests

**DO:**
- ✅ Use environment variables (see [.env.example](.env.example))
- ✅ Configure keys in Unity Inspector (runtime only, not saved to repo)
- ✅ Follow the [Security Guide](SECURITY.md)
- ✅ Review [API Configuration](API_CONFIGURATION.md) for proper setup

### Quick Setup
```bash
# 1. Copy template
cp .env.example .env

# 2. Add your keys (this file is in .gitignore)
# OPENAI_API_KEY=sk-...
# GEMINI_API_KEY=...
# DEEPSEEK_API_KEY=...

# 3. In Unity, load from environment:
string apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
```

See [API Configuration Guide](API_CONFIGURATION.md) for full setup instructions.

## 📁 Project Structure

```
ActionRPG-Quest-System/
├── Assets/
│   ├── AGEL_Experiments/          # Experiment managers and configurations
│   │   ├── AGELExperimentManager.cs
│   │   ├── AGELQuestExperiment.cs
│   │   └── LLMConfiguration.cs
│   ├── Scripts/
│   │   ├── AGEL/                  # Core AGEL-Comp framework
│   │   │   ├── OllamaClient.cs
│   │   │   ├── NeuralTheoremProver.cs
│   │   │   ├── CausalProgramGraph.cs
│   │   │   └── Multimodal/
│   │   │       ├── BaseModelClient.cs
│   │   │       ├── GPT4oClient.cs
│   │   │       ├── GeminiClient.cs
│   │   │       └── DeepSeekVLClient.cs
│   │   ├── PlayerScripts/          # Player/agent control
│   │   ├── QuestSystem/            # Quest management
│   │   ├── Enemy/                  # Enemy AI
│   │   └── NPC Scripts/            # NPC interactions
│   ├── Scenes/
│   ├── Prefabs/
│   └── Sprites/
├── comprehensive_results/          # Experimental results & analysis
│   ├── cpg_rules_database.csv
│   ├── cpg_evolution/
│   ├── plots/
│   └── summary/
├── Packages/                       # Dependencies
├── ProjectSettings/                # Unity project settings
├── ActionSpace.csv                 # Available agent actions
├── Feedbacks.csv                   # Feedback taxonomy
├── paper.txt                       # Research paper (IntelliSys 2026)
├── .env.example                    # API key template
├── .gitignore                      # Version control exclusions
└── README.md                       # This file
```

## 📚 Key Files for Understanding the Framework

### Core Architecture
1. **[CausalProgramGraph.cs](Assets/Scripts/AGEL/CausalProgramGraph.cs)** - World model as directed hypergraph
2. **[NeuralTheoremProver.cs](Assets/Scripts/AGEL/NeuralTheoremProver.cs)** - Differentiable logical verification
3. **[AGELAgent.cs](Assets/Scripts/AGEL/AGELAgent.cs)** - Main agent implementation (deduction-abduction cycle)

### LLM Integration
1. **[BaseModelClient.cs](Assets/Scripts/AGEL/Multimodal/BaseModelClient.cs)** - Abstract base for all model clients
2. **[GPT4oClient.cs](Assets/Scripts/AGEL/Multimodal/GPT4oClient.cs)** - OpenAI integration
3. **[GeminiClient.cs](Assets/Scripts/AGEL/Multimodal/GeminiClient.cs)** - Google Gemini integration

### Experiments
1. **[AGELQuestExperiment.cs](Assets/Scripts/AGEL_Experiments/AGELQuestExperiment.cs)** - Main experiment harness
2. **[AGELExperimentManager.cs](Assets/Scripts/AGEL_Experiments/AGELExperimentManager.cs)** - Batch experiment runner
3. **[LLMConfiguration.cs](Assets/Scripts/AGEL_Experiments/LLMConfiguration.cs)** - Configuration system

## 🔧 Development & Extension

### Adding a New Model Integration

1. **Create a client** inheriting from `BaseModelClient`:
```csharp
[CreateAssetMenu(menuName = "AGEL/Model Clients/My Model")]
public class MyModelClient : BaseModelClient
{
    public override ModelType ModelType => ModelType.MyModel;
    
    public override IEnumerator Initialize() { /* ... */ }
    protected override object PrepareRequestData(...) { /* ... */ }
    protected override UnityWebRequest CreateRequest(string jsonPayload) { /* ... */ }
    protected override ModelResponse ProcessResponse(string jsonResponse) { /* ... */ }
}
```

2. **Register in MultimodalManager**:
```csharp
modelClients[ModelType.MyModel] = myModelClientInstance;
```

3. **Use in your agent**:
```csharp
yield return multimodalManager.GenerateResponse(
    messages,
    response => { /* handle response */ },
    ModelType.MyModel
);
```

### Contributing Rule Learning

The Grounding Function (ILP engine) is the core of compositional learning. To extend it:

1. See `Assets/Scripts/AGEL/InductiveLogicProgramming.cs`
2. Implement custom horn clause generation strategies
3. Test with `AGELQuestExperiment.RunLearningCycle()`

## 📖 Citation

If you use AGEL-Comp in your research, please cite:

```bibtex
@article{shahid2026agelcomp,
  title={AGEL-Comp: A Neuro-Symbolic Framework for Compositional Generalization in Embodied Agents},
  author={Shahid, Mahnoor and Rothe, Hannes},
  booktitle={12th Intelligent Systems Conference 2026},
  pages={},
  year={2026},
  organization={IntelliSys 2026}
}
```

## 📄 Paper

The full research paper is available at:

**Conference**: IntelliSys 2026 (12th Intelligent Systems Conference 2026)  
**Location**: Paphos, Cyprus  
**Dates**: May 25-29, 2026

## 🤝 Contributing

### Guidelines
- Follow existing code style (C# 10 conventions)
- Add unit tests for new components
- Update documentation for architectural changes
- Use meaningful commit messages

### Reporting Issues
Please include:
- Environment details (Unity version, OS)
- Minimal reproduction steps
- Expected vs. actual behavior
- Relevant configuration files (sanitized of API keys!)

## 📞 Contact

**Authors**: 
- Mahnoor Shahid (mahnoor.shahid@uni-due.de)
- Hannes Rothe (hannes.rothe@uni-due.de)

**Institution**: Universität Duisburg-Essen, Germany

## 📜 License

This research code is provided for academic and research purposes. See `LICENSE` file for details.

---

## 🎯 Quick Reference

### Common Tasks

**Run a single experiment:**
```bash
# In Unity Editor: Click Play with AGELExperimentManager configured
```

**Evaluate compositional generalization:**
```csharp
var results = experiment.EvaluateCompositionality(testGoals);
Debug.Log($"Systematicity: {results.systematicity}%");
Debug.Log($"Productivity: {results.productivity}%");
```

**Inspect learned rules:**
```csharp
var cpg = agent.GetWorldModel();
foreach (var rule in cpg.GetAllRules())
{
    Debug.Log($"Learned: {rule.head} :- {string.Join(", ", rule.body)}");
}
```

**Configure LLM model:**
```csharp
// In Unity Inspector:
// 1. Select MultimodalManager asset
// 2. Set Active Model Type
// 3. Configure API key for selected model
// 4. Test with "Initialize" button
```

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| **API Key Error** | Check [API_CONFIGURATION.md](API_CONFIGURATION.md) & environment variables |
| **NTP Timeouts** | Increase `timeout` in BaseModelClient; reduce proof search depth |
| **Memory Issues** | Reduce episodic memory buffer size; process batches separately |
| **No Rules Learned** | Increase feedback signal intensity; check ILP threshold parameters |

---

**Last Updated**: February 2, 2026  
**Status**: Ready for publication with AAMAS 2026
