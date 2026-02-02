using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AGEL;
using System.IO;
using System.Linq;

public class AGELAgent : MonoBehaviour
{
    public static AGELAgent Instance;

    [Header("AGEL Configuration")]
    public int episodicMemorySize = 7;
    public float learningInterval = 1.0f; // How often to trigger learning
    
    [Header("Components")]
    public AGELPerception perception;
    public AGELLLMCore llmCore;
    public AGELActionModule actionModule;
    public AGELGrounding grounding;
    public OllamaClient ollamaClient; // Assign in Inspector or via code
    `r`n    private AGELCompCoordinator agelComp; // AGEL-Comp coordinator (CPG + ILP + NTP)
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    [Header("AGEL Toggle")]
    public bool enableAGEL = false;
    
    // Core AGEL Components
    private WorldModel worldModel;
    private EpisodicMemory episodicMemory;
    private string currentGoal;
    private string currentObjective = "";
    
    // Learning state
    private float lastLearningTime;
    
    private readonly string experimentsDir = "experiments";
    private readonly string screenshotsDir = "experiments/Screenshots";
    
    private List<string> objectives = new List<string>();
    private int currentObjectiveIndex = 0;
    private bool runningObjectives = false;
    
    // Replace objectives/goals setup
    public string mainObjective = "Explore the world, survive and become rich!";
    public List<string> goals = new List<string> {
        "Find the bag of gold and grab it",
        "Find the goblin and kill it",
        "Find and grab the mushroom, meat, and pumpkin",
        "Consume mushroom, then meat",
        "Check the stats and consume pumpkin",
        "Kill two goblins and one sheep",
        "Find Purple Bob and interact with him",
        "Find Brother Blue and report back to Purple Bob",
        "Kill one more goblin",
        "Kill the goblin by standing in top direction, convert to archer, and shoot"
    };
    public int currentGoalIndex = 0;
    private bool runningGoals = false;
    
    private int lastGoldCheck = -1;
    
    public bool goblinDefeatedForGoal = false;
    public int goblinsKilledForGoal = 0;
    public int sheepKilledForGoal = 0;
    
    public bool receivedSecret = false;
    public bool foundBlueQuestComplete = false;
    
    public bool goblinKilledByArrowForGoal = false;
    
    private bool justCompletedGoal = false;
    
    private void Awake()
    {
        Instance = this;
        InitializeAGEL();
        // Subscribe to goblin defeat event
        Enemy_Health.OnMonsterDefeated += OnMonsterDefeatedForGoal;
        // Log the main objective at the start
        Debug.Log($"Main Objective: {mainObjective}");
        if (goals.Count > 0)
            Debug.Log($"Starting goal: {goals[0]}");
        if (enableAGEL)
            StartCoroutine(DelayedStartGoals());
    }
    
    private void Start()
    {
        // Set initial goal
        SetGoal("Survive and explore the world safely");
    }
    
    private void Update()
    {
        if (justCompletedGoal) {
            justCompletedGoal = false;
            return;
        }
        // Main AGEL loop
        // if (!isLearning)
        // {
        //     PerceiveAndAct();
        // }
        // Trigger learning periodically
        // if (Time.time - lastLearningTime > learningInterval)
        // {
        //     LearnFromExperience();
        //     lastLearningTime = Time.time;
        // }

        // Universal goal completion check (works even if AGEL is off)
        if (!enableAGEL && goals.Count > 0 && currentGoalIndex < goals.Count)
        {
            string currentGoal = goals[currentGoalIndex];
            State state = perception.Observe();
            if (IsGoalAchieved(currentGoal, state))
            {
                Debug.Log($"Goal {currentGoalIndex + 1} completed: {currentGoal} goal_status=Complete");
                if (ExpManager.Instance != null)
                    ExpManager.Instance.LevelUpDirectly();
                currentGoalIndex++;
                goblinDefeatedForGoal = false;
                justCompletedGoal = true;
                if (currentGoalIndex < goals.Count)
                {
                    Debug.Log($"Goal {currentGoalIndex + 1}: {goals[currentGoalIndex]}");
                }
                else
                {
                    Debug.Log("All goals completed! Objective achieved: " + mainObjective);
                }
                return;
            }
        }
    }
    
    private void InitializeAGEL()
    {
        // Initialize World Model (no static rules)
        worldModel = new WorldModel();`r`n        // Initialize AGEL-Comp coordinator and sync from current world model`r`n        agelComp = new AGELCompCoordinator();`r`n        agelComp.SyncFromWorldModel(worldModel);
        // Initialize Episodic Memory
        episodicMemory = new EpisodicMemory(episodicMemorySize);
        // Initialize components if not assigned
        if (perception == null) perception = GetComponent<AGELPerception>();
        if (llmCore == null) llmCore = GetComponent<AGELLLMCore>();
        if (actionModule == null) actionModule = GetComponent<AGELActionModule>();
        if (grounding == null) grounding = GetComponent<AGELGrounding>();
        if (ollamaClient == null) ollamaClient = FindObjectOfType<OllamaClient>();
        lastLearningTime = Time.time;
        if (enableDebugLogs)
            Debug.Log("AGEL Agent initialized with episodic memory size: " + episodicMemorySize);
    }
    
    private void InitializeBasicWorldModel()
    {
        // Add basic safety rules
        worldModel.AddRule(new FOLRule("is_harmful(fire)", 0.8f));
        worldModel.AddRule(new FOLRule("is_harmful(poison)", 0.9f));
        worldModel.AddRule(new FOLRule("is_beneficial(healing_item)", 0.7f));
        worldModel.AddRule(new FOLRule("causes_harm(approaching(X)) :- is_harmful(X)", 0.9f));
        worldModel.AddRule(new FOLRule("causes_benefit(consuming(X)) :- is_beneficial(X)", 0.8f));
        
        if (enableDebugLogs)
            Debug.Log("Basic World Model initialized with safety rules");
    }
    
    public void SetGoal(string goal)
    {
        currentGoal = goal;
        if (enableDebugLogs)
            Debug.Log("AGEL Goal set: " + goal);
    }
    
    public void SetObjective(string objective)
    {
        currentObjective = objective;
        if (enableDebugLogs)
            Debug.Log($"AGEL Objective set: {objective}");
    }
    
    public void SetObjectives(List<string> newObjectives)
    {
        objectives = newObjectives;
        currentObjectiveIndex = 0;
        runningObjectives = false;
        if (enableDebugLogs)
            Debug.Log($"AGEL Objectives set: {string.Join(", ", objectives)}");
    }

    public void StartObjectives()
    {
        if (!enableAGEL) return;
        if (objectives.Count > 0 && !runningObjectives)
        {
            runningObjectives = true;
            StartCoroutine(RunObjectivesSequentially());
        }
    }

    private IEnumerator RunObjectivesSequentially()
    {
        while (currentObjectiveIndex < objectives.Count)
        {
            currentObjective = objectives[currentObjectiveIndex];
            if (enableDebugLogs)
                Debug.Log($"Starting objective: {currentObjective}");
            yield return StartCoroutine(LLMPerceiveAndAct());
            currentObjectiveIndex++;
        }
        runningObjectives = false;
        if (enableDebugLogs)
            Debug.Log("All objectives completed!");
    }
    
    public void PerceiveAndAct()
    {
        try
        {
            // Step 1: Perceive current state
            State currentState = perception.Observe();
            
            // Step 2: Generate action plan using LLM Core
            ActionPlan actionPlan = llmCore.Plan(currentGoal, currentState, worldModel);
            `r`n            if (agelComp != null)`r`n            {`r`n                actionPlan = agelComp.VerifyPlan(currentState, actionPlan);`r`n            }
            // Step 3: Translate plan to executable commands
            List<Command> commands = actionModule.Translate(actionPlan);
            
            // Step 4: Execute commands and get feedback
            Feedback feedback = actionModule.Execute(commands);
            
            // Step 5: Record episode
            Episode episode = new Episode(currentState, actionPlan, feedback);
            episodicMemory.Record(episode);
            
            if (enableDebugLogs)
            {
                Debug.Log($"AGEL Episode recorded: State={currentState}, Actions={actionPlan}, Feedback={feedback}");
            }
            // Learn from this episode immediately
            LearnFromExperience();
        }
        catch (Exception e)
        {
            if (enableDebugLogs)
                Debug.LogError("Error in PerceiveAndAct: " + e.Message);
        }
    }
    
    private void LearnFromExperience()
    {
        if (episodicMemory.IsEmpty())
            return;
        try
        {
            // Get recent episode
            Episode recentEpisode = episodicMemory.GetRecentEpisode();
            `r`n            // AGEL-Comp ILP induction and sync`r`n            if (agelComp != null)`r`n            {`r`n                var addedSymbols = agelComp.LearnFromEpisode(recentEpisode, grounding, worldModel);`r`n                if (addedSymbols != null && addedSymbols.Count > 0)`r`n                {`r`n                    foreach (var sym in addedSymbols)`r`n                    {`r`n                        AppendRuleToFile(new FOLRule(sym, 1.0f), "D:/AGEL_WorldModel.txt");`r`n                    }`r`n                    if (enableDebugLogs)`r`n                    {`r`n                        Debug.Log($"AGEL-Comp Learning: Added {addedSymbols.Count} symbol(s)/rule(s) to CPG & WorldModel");`r`n                        foreach (var s in addedSymbols) Debug.Log($"  Added: {s}");`r`n                    }`r`n                }`r`n                return;`r`n            }
            // Generate new rules through grounding
            List<FOLRule> newRules = grounding.GenerateRules(recentEpisode);
            
            // Add new rules to world model
            if (newRules.Count > 0)
            {
                foreach (var rule in newRules)
                {
                    worldModel.AddRule(rule);
                    AppendRuleToFile(rule, "D:/AGEL_WorldModel.txt");
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log($"AGEL Learning: Added {newRules.Count} new rules from experience");
                    foreach (var rule in newRules)
                    {
                        Debug.Log($"  New Rule: {rule} (weight: {rule.weight})");
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (enableDebugLogs)
                Debug.LogError("Error in LearnFromExperience: " + e.Message);
        }
    }
    
    private void AppendRuleToFile(FOLRule rule, string path)
    {
        try
        {
            File.AppendAllText(path, rule.ToString() + System.Environment.NewLine);
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.LogError($"Failed to append rule to file: {e.Message}");
        }
    }
    
    // Public methods for external interaction
    public WorldModel GetWorldModel() => worldModel;
    public EpisodicMemory GetEpisodicMemory() => episodicMemory;
    public string GetCurrentGoal() => currentGoal;
    
    // Debug methods
    public void PrintWorldModel()
    {
        Debug.Log("=== Current World Model ===");
        foreach (var rule in worldModel.GetAllRules())
        {
            Debug.Log($"Rule: {rule} (Weight: {rule.weight})");
        }
    }
    
    public void PrintEpisodicMemory()
    {
        Debug.Log("=== Episodic Memory ===");
        var episodes = episodicMemory.GetAllEpisodes();
        for (int i = 0; i < episodes.Count; i++)
        {
            Debug.Log($"Episode {i}: {episodes[i]}");
        }
    }

    private List<string> LoadActionSpace()
    {
        var actions = new List<string>();
        string path = "ActionSpace.csv";
        if (File.Exists(path))
        {
            var lines = File.ReadAllLines(path).Skip(1); // skip header
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 2)
                    actions.Add(parts[1].Trim()); // action_name
            }
        }
        return actions;
    }

    private string FindClosestAction(string proposedAction, List<string> actionSpace, out float bestSimilarity)
    {
        bestSimilarity = 0f;
        if (string.IsNullOrEmpty(proposedAction) || actionSpace == null || actionSpace.Count == 0)
            return "wait"; // default fallback
        proposedAction = new string(proposedAction.ToLower().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();
        string bestMatch = "wait";
        foreach (var action in actionSpace)
        {
            string cleanAction = action.ToLower().Trim();
            float similarity = CalculateStringSimilarity(proposedAction, cleanAction);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = action;
            }
        }
        if (enableDebugLogs)
        {
            Debug.Log($"LLM proposed: '{proposedAction}', mapped to: '{bestMatch}' (similarity: {bestSimilarity})");
        }
        return bestMatch;
    }

    private float CalculateStringSimilarity(string s1, string s2)
    {
        // Simple Levenshtein distance-based similarity
        int distance = LevenshteinDistance(s1, s2);
        int maxLength = Mathf.Max(s1.Length, s2.Length);
        return maxLength == 0 ? 1f : 1f - ((float)distance / maxLength);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(
                    d[i - 1, j] + 1,      // deletion
                    d[i, j - 1] + 1,      // insertion
                    d[i - 1, j - 1] + cost // substitution
                );
            }
        }

        return d[s1.Length, s2.Length];
    }

    private List<(string feedback_name, string description)> LoadFeedbackList()
    {
        var feedbacks = new List<(string, string)>();
        string path = "Feedbacks.csv";
        if (File.Exists(path))
        {
            var lines = File.ReadAllLines(path).Skip(1); // skip header
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 3)
                    feedbacks.Add((parts[1].Trim(), parts[2].Trim()));
            }
        }
        return feedbacks;
    }

    private List<(string action_name, string key, string description)> LoadActionSpaceDetailed()
    {
        var actions = new List<(string, string, string)>();
        string path = "ActionSpace.csv";
        if (File.Exists(path))
        {
            var lines = File.ReadAllLines(path).Skip(1); // skip header
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 4)
                    actions.Add((parts[1].Trim(), parts[3].Trim(), parts[2].Trim()));
            }
        }
        return actions;
    }

    private string BuildActionSpaceSection(List<(string action_name, string key, string description)> actions)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Action Space (available actions and keys):");
        foreach (var a in actions)
        {
            sb.AppendLine($"- {a.action_name}: {a.key} ({a.description})");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    public IEnumerator LLMPerceiveAndAct(Action onComplete = null)
    {
        if (!enableAGEL)
        {
            onComplete?.Invoke();
            yield break;
        }
        // Ensure experiment directories exist
        if (!Directory.Exists(experimentsDir)) Directory.CreateDirectory(experimentsDir);
        if (!Directory.Exists(screenshotsDir)) Directory.CreateDirectory(screenshotsDir);
        // 1. Pause the game
        Time.timeScale = 0f;
        State initialState = perception.Observe();
        List<string> actionSpace = LoadActionSpace();
        var feedbackList = LoadFeedbackList();
        // Wait for end of frame before capturing screenshot
        yield return new WaitForEndOfFrame();
        string initialScreenshotFile = $"{screenshotsDir}/Ollama_Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        Texture2D initialScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(initialScreenshotFile, initialScreenshot.EncodeToPNG());
        // Build world model string
        var worldModelRules = worldModel.GetAllRules().Select(r => r.ToString()).ToList();
        string planPrompt = BuildActionSequencePromptWithObjective(initialState, worldModelRules, initialScreenshotFile, currentObjective);
        File.AppendAllText($"{experimentsDir}/Ollama_Prompts.txt", planPrompt + System.Environment.NewLine + "---" + System.Environment.NewLine);
        string actionPlanText = null;
        yield return ollamaClient.GenerateCompletionWithImage(planPrompt, initialScreenshot, (result) => actionPlanText = result);
        File.AppendAllText($"{experimentsDir}/Ollama_Responses.txt", actionPlanText + System.Environment.NewLine + "---" + System.Environment.NewLine);
        if (string.IsNullOrEmpty(actionPlanText))
        {
            if (enableDebugLogs)
                Debug.LogError("LLM did not return an action plan.");
            Time.timeScale = 1f;
            onComplete?.Invoke();
            yield break;
        }
        // Parse the LLM's response into a sequence of actions
        var actions = actionPlanText.Split(';').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
        var episodes = new List<Episode>();
        State currentState = initialState;
        State prevState = initialState;
        foreach (var action in actions)
        {
            // Wait for end of frame before each action for accurate screenshot
            yield return new WaitForEndOfFrame();
            string screenshotFile = $"{screenshotsDir}/Ollama_Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(screenshotFile, screenshot.EncodeToPNG());
            // Map to closest action in action space (for robustness, but execute as is if you want full LLM control)
            string mappedAction = FindClosestAction(action, actionSpace, out float similarity);
            bool actionFailed = false;
            string optimalMove = mappedAction;
            string feedbackName = "";
            string feedbackDesc = "";
            float similarityThreshold = 0.7f;
            if (similarity < similarityThreshold)
            {
                mappedAction = "wait";
                actionFailed = true;
                optimalMove = mappedAction;
                feedbackName = "action_failed";
                feedbackDesc = $"Action failed. Optimal move: {optimalMove}";
            }
            else
            {
                var match = feedbackList.FirstOrDefault(fb => mappedAction.Contains(fb.feedback_name) || action.ToLower().Contains(fb.feedback_name));
                if (!string.IsNullOrEmpty(match.feedback_name))
                {
                    feedbackName = match.feedback_name;
                    feedbackDesc = match.description;
                }
                else
                {
                    feedbackName = "no_effect";
                    feedbackDesc = "No significant feedback.";
                }
            }
            // Use mapped action for command execution
            ActionPlan mappedActionPlan = new ActionPlan(new List<string> { mappedAction }, action, 1.0f);
            List<Command> commands = actionModule.Translate(mappedActionPlan);
            Feedback feedback = actionModule.Execute(commands);
            feedback.message = $"{feedbackName}: {feedbackDesc}; action_failed={actionFailed}; optimal_move={optimalMove}";
            // Log the episode
            ActionPlan episodeActionPlan = new ActionPlan(new List<string> { action }, action, 1.0f);
            // Calculate progress feedback
            float progress = CalculateObjectiveProgress(currentObjective, prevState, currentState);
            bool closerToObjective = progress > 0;
            feedback.message += $"; closer_to_objective={closerToObjective}; progress={progress}";
            Episode episode = new Episode(currentState, episodeActionPlan, feedback);
            episodes.Add(episode);
            episodicMemory.Record(episode);
            File.AppendAllText($"{experimentsDir}/AGEL_Episodes.txt", $"Screenshot: {screenshotFile}\nLLM Proposed: {action}\nMapped Action: {mappedAction}\n{episode}{System.Environment.NewLine}---{System.Environment.NewLine}");
            // Update state for next action
            prevState = currentState;
            currentState = perception.Observe();
        }
        // After the sequence, check if the objective is achieved
        bool achieved = IsObjectiveAchieved(currentObjective, currentState);
        if (!achieved)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"Objective not achieved: {currentObjective}. Re-planning...");
            // Optionally, you can add a retry limit to avoid infinite loops
            yield return StartCoroutine(LLMPerceiveAndAct());
            yield break;
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"Objective achieved: {currentObjective}");
        }
        // After the sequence, send the batch of episodes to LLM for FOL grounding
        string folPrompt = BuildFOLRulePromptWithImageAndFeedbackBatch(episodes, feedbackList);
        File.AppendAllText($"{experimentsDir}/AGEL_EpisodicMemoryToLLM.txt", folPrompt + System.Environment.NewLine + "---" + System.Environment.NewLine);
        string folRulesText = null;
        yield return ollamaClient.GenerateCompletion(folPrompt, (result) => folRulesText = result);
        if (!string.IsNullOrEmpty(folRulesText))
        {
            var ruleLines = folRulesText.Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && line.Contains("(") && line.Contains(")") && !line.ToLower().Contains("because") && !line.ToLower().Contains("therefore") && !line.ToLower().Contains("so that"));
            foreach (var rule in ruleLines)
            {
                AppendRuleToFileRaw(rule, $"{experimentsDir}/AGEL_WorldModel.txt");
            }
        }
        // Log the updated world model snapshot
        var allRules = worldModel.GetAllRules();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== World Model Snapshot ===");
        foreach (var rule in allRules) sb.AppendLine(rule.ToString());
        File.AppendAllText($"{experimentsDir}/AGEL_WorldModel_Snapshots.txt", sb.ToString() + System.Environment.NewLine + "---" + System.Environment.NewLine);
        // 4. Resume the game
        Time.timeScale = 1f;
        onComplete?.Invoke();
    }

    // Helper: Build prompt for action sequence with objective
    private string BuildActionSequencePromptWithObjective(State state, List<string> worldModelRules, string screenshotFile, string objective)
    {
        var sb = new System.Text.StringBuilder();
        var actionsDetailed = LoadActionSpaceDetailed();
        sb.Append(BuildActionSpaceSection(actionsDetailed));
        sb.AppendLine($"You are an agent in a game. Here is your current state (see attached screenshot: {screenshotFile}):");
        sb.AppendLine(state.ToString());
        sb.AppendLine();
        sb.AppendLine("World Model (FOL rules, external knowledge):");
        foreach (var rule in worldModelRules) sb.AppendLine(rule);
        sb.AppendLine();
        sb.AppendLine($"Objective: {objective}");
        sb.AppendLine();
        sb.AppendLine("Consult the above world model as your external knowledge. Based on your current state, action space, and objective, generate a short sequence of actions (e.g., 'move_left; grab; move_right; wait') to achieve the objective. Respond with only the action sequence, separated by semicolons.");
        return sb.ToString();
    }

    // Helper: Build FOL rule prompt for a batch of episodes
    private string BuildFOLRulePromptWithImageAndFeedbackBatch(List<Episode> episodes, List<(string feedback_name, string description)> feedbackList)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Given the following sequence of episodes (including state, action, feedback) and their screenshots:");
        foreach (var ep in episodes)
        {
            sb.AppendLine(ep.ToString());
        }
        sb.AppendLine();
        sb.AppendLine("Feedback List (for reference, use to decide rule weightage):");
        foreach (var fb in feedbackList)
            sb.AppendLine($"- {fb.feedback_name}: {fb.description}");
        sb.AppendLine();
        sb.AppendLine("Use the sequence of experiences to generate as many weighted First-Order Logic (FOL) rules as possible that describe the agent's learned knowledge from this experience. Each rule should be a single line and include a weight (e.g., 0.1 to 1.0) based on the feedback and experience.");
        return sb.ToString();
    }

    private void AppendRuleToFileRaw(string rules, string path)
    {
        try
        {
            File.AppendAllText(path, rules + System.Environment.NewLine);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to append rules to file: {e.Message}");
        }
    }

    private IEnumerator DelayedStartGoals()
    {
        if (enableDebugLogs)
            Debug.Log("Waiting 5 seconds before LLM starts playing...");
        yield return new WaitForSeconds(5f);
        StartGoals();
    }

    public void StartGoals()
    {
        if (!enableAGEL) return;
        if (goals.Count > 0 && !runningGoals)
        {
            runningGoals = true;
            StartCoroutine(RunGoalsSequentially());
        }
    }

    private IEnumerator RunGoalsSequentially()
    {
        while (currentGoalIndex < goals.Count)
        {
            string currentGoal = goals[currentGoalIndex];
            Debug.Log($"Goal {currentGoalIndex + 1}: {currentGoal}");
            yield return StartCoroutine(LLMPerceiveAndActWithGoal(currentGoal));
            currentGoalIndex++;
        }
        runningGoals = false;
        Debug.Log("All goals completed! Objective achieved: " + mainObjective);
    }

    private IEnumerator LLMPerceiveAndActWithGoal(string goal)
    {
        State state = perception.Observe();
        bool achieved = IsGoalAchieved(goal, state);
        if (!achieved)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"Goal not achieved: {goal}. Re-planning...");
            yield return StartCoroutine(LLMPerceiveAndActWithGoal(goal));
            yield break;
        }
        else
        {
            Debug.Log($"Goal {currentGoalIndex + 1} completed: {goal} goal_status=Complete");
            if (ExpManager.Instance != null)
                ExpManager.Instance.LevelUpDirectly();
        }
    }

    // Check if the current objective is achieved (simple examples, can be extended)
    private bool IsObjectiveAchieved(string objective, State state)
    {
        objective = objective.ToLower();
        if (objective.Contains("mushroom"))
        {
            // Check if inventory contains a mushroom
            return state.inventoryItems != null && state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("mushroom"));
        }
        if (objective.Contains("gold"))
        {
            // Check if gold is above a threshold (e.g., 1)
            if (state.environmentState != null && state.environmentState.ContainsKey("gold"))
                return (int)state.environmentState["gold"] > 0;
        }
        if (objective.Contains("kill") && objective.Contains("goblin"))
        {
            // Check if goblin is not in nearby objects (very simple)
            return state.nearbyObjects == null || !state.nearbyObjects.Exists(obj => obj.name.ToLower().Contains("goblin"));
        }
        // Default: not achieved
        return false;
    }

    // Calculate progress metric (e.g., distance to mushroom, gold, goblin)
    private float CalculateObjectiveProgress(string objective, State prevState, State currState)
    {
        objective = objective.ToLower();
        if (objective.Contains("mushroom"))
        {
            // Example: count mushrooms in inventory
            int prev = prevState.inventoryItems?.FindAll(item => item.itemName.ToLower().Contains("mushroom")).Count ?? 0;
            int curr = currState.inventoryItems?.FindAll(item => item.itemName.ToLower().Contains("mushroom")).Count ?? 0;
            return curr - prev;
        }
        if (objective.Contains("gold"))
        {
            int prev = prevState.environmentState != null && prevState.environmentState.ContainsKey("gold") ? (int)prevState.environmentState["gold"] : 0;
            int curr = currState.environmentState != null && currState.environmentState.ContainsKey("gold") ? (int)currState.environmentState["gold"] : 0;
            return curr - prev;
        }
        if (objective.Contains("kill") && objective.Contains("goblin"))
        {
            // Example: goblin present before but not after
            bool prev = prevState.nearbyObjects != null && prevState.nearbyObjects.Exists(obj => obj.name.ToLower().Contains("goblin"));
            bool curr = currState.nearbyObjects != null && currState.nearbyObjects.Exists(obj => obj.name.ToLower().Contains("goblin"));
            return (prev && !curr) ? 1 : 0;
        }
        return 0;
    }

    private bool IsGoalAchieved(string goal, State state)
    {
        goal = goal.ToLower();
        // 1. Find the bag of gold and grab it
        if (goal.Contains("bag of gold"))
        {
            // Check if gold is present in environmentState
            if (state.environmentState != null && state.environmentState.ContainsKey("gold"))
            {
                int goldAmount = 0;
                try { goldAmount = (int)state.environmentState["gold"]; } catch { goldAmount = 0; }
                if (goldAmount != lastGoldCheck) {
                    Debug.Log($"[Goal Check] Gold in environmentState: {goldAmount}");
                    lastGoldCheck = goldAmount;
                }
                return goldAmount > 0;
            }
            return false;
        }
        // 2. Find the goblin and kill it
        if (goal.Contains("goblin") && goal.Contains("kill") && !goal.Contains("two") && !goal.Contains("one more"))
        {
            // Only complete when a goblin is actually defeated
            if (goblinDefeatedForGoal)
            {
                goblinDefeatedForGoal = false; // Reset for next time
                return true;
            }
            return false;
        }
        // 3. Find and grab the mushroom, meat, and pumpkin
        if (goal.Contains("mushroom") && goal.Contains("meat") && goal.Contains("pumpkin") && goal.Contains("grab"))
        {
            bool hasMushroom = state.inventoryItems != null && state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("mushroom"));
            bool hasMeat = state.inventoryItems != null && state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("meat") || item.itemName.ToLower().Contains("steak"));
            bool hasPumpkin = state.inventoryItems != null && state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("pumpkin"));
            return hasMushroom && hasMeat && hasPumpkin;
        }
        // 4. Consume mushroom, then meat
        if (goal.Contains("consume mushroom") && goal.Contains("then meat"))
        {
            // Check if mushroom and meat are NOT in inventory (assume consumed)
            bool hasMushroom = state.inventoryItems != null && state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("mushroom"));
            bool hasMeat = state.inventoryItems != null && (state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("meat")) || state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("steak")));
            return !hasMushroom && !hasMeat;
        }
        // 5. Check the stats and then consume pumpkin
        if (goal.Contains("check the stats") && goal.Contains("consume pumpkin"))
        {
            // Check if pumpkin is NOT in inventory (assume consumed)
            bool hasPumpkin = state.inventoryItems != null && state.inventoryItems.Exists(item => item.itemName.ToLower().Contains("pumpkin"));
            return !hasPumpkin;
        }
        // 6. Kill two goblins and one sheep
        if (goal.Contains("kill two goblin") && goal.Contains("one sheep"))
        {
            if (goblinsKilledForGoal >= 2 && sheepKilledForGoal >= 1)
            {
                goblinsKilledForGoal = 0;
                sheepKilledForGoal = 0;
                return true;
            }
            return false;
        }
        // 7. Find Purple NPC and find out the secret to get rich
        if (goal.Contains("purple npc") && goal.Contains("secret"))
        {
            if (receivedSecret)
            {
                receivedSecret = false;
                return true;
            }
            return false;
        }
        // 9. Kill one more goblin
        if (goal.Contains("one more goblin"))
        {
            if (goblinDefeatedForGoal)
            {
                goblinDefeatedForGoal = false;
                return true;
            }
            return false;
        }
        // 10. Kill the goblin by standing in top direction, convert to archer, and shoot
        if (goal.Contains("top direction") && goal.Contains("archer") && goal.Contains("shoot"))
        {
            // For demo: check if no goblin is nearby and player is archer (assume archer if damage > 1 and speed > 1)
            bool goblinAlive = state.nearbyObjects != null && state.nearbyObjects.Exists(obj => obj.name.ToLower().Contains("goblin"));
            bool isArcher = state.environmentState != null && state.environmentState.ContainsKey("playerDamage") && state.environmentState.ContainsKey("playerSpeed") && (int)state.environmentState["playerDamage"] > 1 && (int)state.environmentState["playerSpeed"] > 1;
            return !goblinAlive && isArcher;
        }
        // Default: not achieved
        return false;
    }

    private void OnDestroy()
    {
        Enemy_Health.OnMonsterDefeated -= OnMonsterDefeatedForGoal;
    }

    private void OnMonsterDefeatedForGoal(int exp)
    {
        // This is called for any monster, but only set flag if a goblin was killed
        // We'll set the flag in Enemy_Health when a goblin is killed
        goblinDefeatedForGoal = true;
    }
} 