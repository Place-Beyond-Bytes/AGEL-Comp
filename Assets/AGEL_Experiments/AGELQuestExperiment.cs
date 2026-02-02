using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace AGEL.Experiments
{
    [CreateAssetMenu(menuName = "AGEL/Quest Experiment")]
    public class AGELQuestExperiment : AGELExperiment
    {
        [Header("Agent Reference")]
        public AGELAgent agent;
        
        [Header("Quest Configuration")]
        [Tooltip("If true, uses the goals defined in AGELAgent. Otherwise, uses the quests defined below.")]
        public bool useAgentGoals = true;
        
        [Tooltip("Only used if useAgentGoals is false")]
        public List<QuestDefinition> customQuests = new List<QuestDefinition>();
        
        [Header("LLM Configuration")]
        [Tooltip("Leave empty to use the default configuration from AGELAgent")]
        public LLMConfiguration llmConfiguration;
        
        [Header("Runtime State")]
        [ReadOnly] public string currentQuest;
        [ReadOnly] public int currentQuestIndex = -1;
        [ReadOnly] public int questAttempts = 0;
        [ReadOnly] public float questStartTime;
        [ReadOnly] public bool isQuestRunning = false;
        
        // Metrics
        [System.Serializable]
        public class QuestResult
        {
            public string questName;
            public bool completed;
            public int attempts;
            public float completionTime;
            public int rulesLearned;
            public int rulesRetracted;
            public float ntpVetoRate;
            public float ilpConfidence;
            public Dictionary<string, object> customMetrics = new Dictionary<string, object>();
            
            public float GetMetricValue(string metricName)
            {
                if (customMetrics.TryGetValue(metricName, out object value))
                {
                    if (value is float f) return f;
                    if (value is int i) return i;
                    if (value is bool b) return b ? 1f : 0f;
                }
                return 0f;
            }
        }
        
        [Header("Results")]
        public List<QuestResult> questResults = new List<QuestResult>();
        
        // Computed metrics
        public int TotalQuests => useAgentGoals ? (agent != null ? agent.goals.Count : 0) : customQuests.Count;
        public int CompletedQuests => questResults.Count(r => r.completed);
        public float SuccessRate => TotalQuests > 0 ? (float)CompletedQuests / TotalQuests : 0f;
        public float AverageAttempts => questResults.Count > 0 ? (float)questResults.Sum(r => r.attempts) / questResults.Count : 0f;
        public float AverageCompletionTime => questResults.Count > 0 ? questResults.Average(r => r.completionTime) : 0f;
        public int TotalRulesLearned => questResults.Sum(r => r.rulesLearned);
        public int TotalRulesRetracted => questResults.Sum(r => r.rulesRetracted);
        public float AverageNTPVetoRate => questResults.Count > 0 ? questResults.Average(r => r.ntpVetoRate) : 0f;
        public float AverageILPConfidence => questResults.Count > 0 ? questResults.Average(r => r.ilpConfidence) : 0f;
        
        public override IEnumerator Run()
        {
            Log($"Starting Quest Experiment with {(useAgentGoals ? "Agent Goals" : "Custom Quests")}");
            
            if (useAgentGoals && agent == null)
            {
                LogError("Agent reference is required when using agent goals");
                yield break;
            }
            
            // Initialize metrics
            questResults.Clear();
            currentQuestIndex = -1;
            
            // Get the list of quests to run
            List<string> questsToRun = useAgentGoals ? agent.goals : customQuests.Select(q => q.name).ToList();
            
            if (questsToRun.Count == 0)
            {
                LogError("No quests to run!");
                yield break;
            }
            
            Log($"Found {questsToRun.Count} quests to run");
            
            // Apply LLM configuration if provided
            if (llmConfiguration != null)
            {
                Log($"Applying LLM Configuration: {llmConfiguration.name}");
                // TODO: Apply LLM configuration to the agent
                // agent.ApplyLLMConfiguration(llmConfiguration);
            }
            
            // Run through each quest in sequence
            for (currentQuestIndex = 0; currentQuestIndex < questsToRun.Count; currentQuestIndex++)
            {
                currentQuest = questsToRun[currentQuestIndex];
                questAttempts = 0;
                questStartTime = Time.time;
                isQuestRunning = true;
                
                Log($"\n=== Starting Quest {currentQuestIndex + 1}/{questsToRun.Count}: {currentQuest} ===");
                
                // Create a new result for this quest
                var questResult = new QuestResult
                {
                    questName = currentQuest,
                    attempts = 0,
                    completed = false,
                    completionTime = 0f,
                    rulesLearned = 0,
                    rulesRetracted = 0,
                    ntpVetoRate = 0f,
                    ilpConfidence = 1.0f
                };
                
                // Start the quest
                yield return StartQuest(currentQuest, questResult);
                
                // Wait for quest completion or failure
                float startTime = Time.time;
                float timeout = 300f; // 5 minutes timeout per quest
                
                while (isQuestRunning && (Time.time - startTime) < timeout)
                {
                    // Check if quest is complete
                    bool isComplete = CheckQuestCompletion(currentQuest);
                    
                    if (isComplete)
                    {
                        questResult.completed = true;
                        questResult.completionTime = Time.time - questStartTime;
                        Log($"Quest completed in {questResult.completionTime:F1} seconds!");
                        break;
                    }
                    
                    // Update metrics periodically
                    if (Time.frameCount % 60 == 0) // Every second (assuming 60 FPS)
                    {
                        // Update any running metrics
                        // questResult.ntpVetoRate = agent?.GetNTPVetoRate() ?? 0f;
                        // questResult.ilpConfidence = agent?.GetILPConfidence() ?? 1.0f;
                    }
                    
                    yield return null; // Wait for next frame
                }
                
                if (!questResult.completed)
                {
                    questResult.completionTime = Time.time - questStartTime;
                    LogWarning($"Quest timed out after {questResult.completionTime:F1} seconds!");
                }
                
                // Record the result
                questResults.Add(questResult);
                
                // Save intermediate results
                SaveResults();
                
                // Brief pause between quests
                yield return new WaitForSeconds(1.0f);
                // Wait for quest completion or failure
                bool questCompleted = false;
                while (!questCompleted)
                {
                    questAttempts++;
                    Log($"Attempt {questAttempts} for quest: {currentQuest.questName}");
                    
                    // Run the quest attempt
                    yield return RunQuestAttempt(currentQuest);
                    
                    // Check if quest was completed
                    questCompleted = CheckQuestCompletion(currentQuest);
                    
                    if (questCompleted)
                    {
                        OnQuestCompleted(true);
                    }
                    else if (questAttempts >= maxAttemptsPerTask)
                    {
                        OnQuestCompleted(false);
                        break;
                    }
                    
                    yield return null;
                }
                
                // Small delay between quests
                yield return new WaitForSeconds(1f);
            }
            
            // Calculate final metrics
            CalculateFinalMetrics();
            
            Log("\n=== Quest Experiment Complete ===");
            Log($"Completed {totalQuestsCompleted} out of {quests.Count} quests");
            Log($"Success Rate: {successRate:P1}");
            Log($"Average Completion Time: {averageCompletionTime:F2}s");
            Log($"Average Attempts per Quest: {averageAttemptsPerQuest:F2}");
            Log($"Total Rules Learned: {rulesLearned} (Retracted: {rulesRetracted})");
        }
        
        private IEnumerator StartQuest(QuestDefinition quest)
        {
            // Here you would trigger the quest in your game
            // For example: QuestManager.Instance.StartQuest(quest.questId);
            
            Log($"Quest started: {quest.questName}");
            Log($"Objective: {quest.objective}");
            
            // Reset any quest-specific state
            questAttempts = 0;
            questStartTime = Time.time;
            
            yield return null;
        }
        
        private IEnumerator RunQuestAttempt(QuestDefinition quest)
        {
            // This is where the agent will attempt to complete the quest
            // The actual implementation would depend on your game's architecture
            
            // For now, we'll simulate the agent working on the quest
            float startTime = Time.time;
            bool success = false;
            
            // Simulate the agent working on the quest
            Log($"Agent is attempting quest: {quest.questName}");
            
            // In a real implementation, you would:
            // 1. Let the agent observe the environment
            // 2. Let the agent plan and execute actions
            // 3. Monitor for quest completion or failure
            
            // Simulate some processing time
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
            
            // Simulate learning (if applicable)
            if (agent is ILearnableAgent learnableAgent)
            {
                var learningResult = SimulateLearning(quest);
                rulesLearned += learningResult.rulesAdded;
                rulesRetracted += learningResult.rulesRetracted;
                
                if (learningResult.rulesAdded > 0 || learningResult.rulesRetracted > 0)
                {
                    Log($"Agent learned {learningResult.rulesAdded} new rules, retracted {learningResult.rulesRetracted}");
                }
            }
            
            // Simulate NTP vetoes (if applicable)
            if (agent is AGELAgent ag && ag.useNTP)
            {
                // Random chance of NTP veto for demonstration
                if (UnityEngine.Random.value < 0.2f) // 20% chance
                {
                    Log("NTP vetoed an action that would have violated learned rules");
                }
            }
            
            // Simulate success or failure
            success = UnityEngine.Random.value > 0.3f; // 70% success rate for demo
            
            // Record the attempt
            var attempt = new QuestAttempt
            {
                attemptNumber = questAttempts,
                duration = Time.time - startTime,
                success = success,
                rulesLearned = agent is ILearnableAgent ? (agent as ILearnableAgent).GetRulesLearned() : null
            };
            
            // Add to current quest result
            var currentResult = questResults.Find(r => r.questId == quest.questId);
            if (currentResult == null)
            {
                currentResult = new QuestResult { questId = quest.questId };
                questResults.Add(currentResult);
            }
            currentResult.attempts.Add(attempt);
            
            // Update status
            if (success)
            {
                Log($"Quest '{quest.questName}' completed successfully in attempt {questAttempts}!");
            }
            else
            {
                Log($"Quest attempt {questAttempts} failed. " + 
                    (questAttempts < maxAttemptsPerTask ? "Retrying..." : "Maximum attempts reached."));
            }
        }
        
        private bool CheckQuestCompletion(QuestDefinition quest)
        {
            // In a real implementation, you would check with your quest system
            // For example: return QuestManager.Instance.IsQuestComplete(quest.questId);
            
            // For demo purposes, we'll use the success flag from the last attempt
            var currentResult = questResults.Find(r => r.questId == quest.questId);
            if (currentResult != null && currentResult.attempts.Count > 0)
            {
                return currentResult.attempts[currentResult.attempts.Count - 1].success;
            }
            return false;
        }
        
        private void OnQuestCompleted(bool success)
        {
            float duration = Time.time - questStartTime;
            var quest = currentQuest;
            
            // Update metrics
            if (success)
            {
                totalQuestsCompleted++;
                Log($"Quest '{quest.questName}' completed in {duration:F2}s after {questAttempts} attempts!");
            }
            else
            {
                totalQuestsFailed++;
                Log($"Quest '{quest.questName}' failed after {maxAttemptsPerTask} attempts.");
            }
            
            // Update the current quest result
            var currentResult = questResults.Find(r => r.questId == quest.questId);
            if (currentResult != null)
            {
                currentResult.completed = success;
                currentResult.totalDuration = duration;
                currentResult.totalAttempts = questAttempts;
            }
            
            // Update running averages
            UpdateRunningMetrics();
        }
        
        private void InitializeMetrics()
        {
            totalQuestsCompleted = 0;
            totalQuestsFailed = 0;
            averageCompletionTime = 0f;
            successRate = 0f;
            averageAttemptsPerQuest = 0f;
            rulesLearned = 0;
            rulesRetracted = 0;
            questResults.Clear();
        }
        
        private void UpdateRunningMetrics()
        {
            int totalQuests = totalQuestsCompleted + totalQuestsFailed;
            if (totalQuests > 0)
            {
                successRate = (float)totalQuestsCompleted / totalQuests;
                
                // Calculate average completion time for successful quests
                float totalTime = 0f;
                int completedWithTime = 0;
                foreach (var result in questResults)
                {
                    if (result.completed)
                    {
                        totalTime += result.totalDuration;
                        completedWithTime++;
                    }
                }
                averageCompletionTime = completedWithTime > 0 ? totalTime / completedWithTime : 0f;
                
                // Calculate average attempts per quest
                int totalAttempts = 0;
                foreach (var result in questResults)
                {
                    totalAttempts += result.totalAttempts;
                }
                averageAttemptsPerQuest = totalQuests > 0 ? (float)totalAttempts / totalQuests : 0f;
            }
        }
        
        private void CalculateFinalMetrics()
        {
            // Final calculation of all metrics
            UpdateRunningMetrics();
            
            // Additional metrics can be calculated here if needed
        }
        
        private LearningResult SimulateLearning(QuestDefinition quest)
        {
            // In a real implementation, this would be handled by your ILP engine
            // For demo purposes, we'll simulate learning with some randomness
            
            var result = new LearningResult();
            
            // Simulate learning new rules
            if (UnityEngine.Random.value > 0.7f) // 30% chance to learn something
            {
                result.rulesAdded = 1;
                
                // Small chance to retract a rule (simulating incorrect learning)
                if (UnityEngine.Random.value > 0.9f) // 10% chance
                {
                    result.rulesRetracted = 1;
                }
            }
            
            return result;
        }
        
        [System.Serializable]
        public class QuestDefinition
        {
            public string questId;
            public string questName;
            [TextArea] public string objective;
            [TextArea] public string description;
            public int difficulty = 1; // 1-5 scale
            public List<string> requiredItems = new List<string>();
            public List<string> requiredActions = new List<string>();
            public bool testSystematicity = false;
            public bool testProductivity = false;
            public bool testAmbiguity = false;
        }
        
        [System.Serializable]
        public class QuestResult
        {
            public string questId;
            public bool completed;
            public float totalDuration;
            public int totalAttempts;
            public List<QuestAttempt> attempts = new List<QuestAttempt>();
        }
        
        [System.Serializable]
        public class QuestAttempt
        {
            public int attemptNumber;
            public float duration;
            public bool success;
            public List<string> rulesLearned;
        }
    }
}
