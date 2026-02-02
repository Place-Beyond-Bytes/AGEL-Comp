using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AGEL.Experiments
{
    [CreateAssetMenu(menuName = "AGEL/Experiment Manager")]
    public class AGELExperimentManager : ScriptableObject
    {
        [Header("Experiment Settings")]
        public List<AGELQuestExperiment> experiments = new List<AGELQuestExperiment>();
        public bool runOnStart = true;
        public int maxQuestsPerRun = 10;
        public int maxAttemptsPerQuest = 5;
        
        [Header("LLM Configurations")]
        public List<LLMConfiguration> llmConfigurations = new List<LLMConfiguration>();
        
        [Header("Output Settings")]
        public string outputFolder = "AGEL_Results";
        public bool saveToFile = true;
        public bool logVerbose = true;
        
        [Header("Runtime State")]
        [ReadOnly] public string currentExperiment;
        [ReadOnly] public string currentLLMConfig;
        [ReadOnly] public string currentGoal;
        [ReadOnly] public int currentQuestIndex;
        [ReadOnly] public string status;
        [ReadOnly] public int completedCount;
        [ReadOnly] public int totalCount;
        
        // Metrics
        [System.Serializable]
        public class QuestMetrics
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
        }
        
        [System.Serializable]
        public class LLMMetrics
        {
            public string llmConfigName;
            public List<QuestMetrics> questMetrics = new List<QuestMetrics>();
            public float overallSuccessRate;
            public float averageAttempts;
            public float averageCompletionTime;
            public float averageRulesLearned;
            public float averageRulesRetracted;
            public float averageNTPVetoRate;
            public float averageILPConfidence;
        }
        
        private List<LLMMetrics> allMetrics = new List<LLMMetrics>();
        private string outputPath;
        private int totalQuests;
        
        private void OnEnable()
        {
            // Initialize default LLM configurations if none exist
            if (llmConfigurations.Count == 0)
            {
                // Baseline: Standard LLM without NTP or ILP
                llmConfigurations.Add(new LLMConfiguration("GPT-4 (Baseline)", "gpt-4", 0.7f, false, false));
                
                // Full system with NTP and ILP
                llmConfigurations.Add(new LLMConfiguration("GPT-4 + NTP + ILP", "gpt-4", 0.7f, true, true, 0.7f));
                
                // Without NTP
                llmConfigurations.Add(new LLMConfiguration("GPT-4 + ILP Only", "gpt-4", 0.7f, false, true, 0.7f));
                
                // Without ILP
                llmConfigurations.Add(new LLMConfiguration("GPT-4 + NTP Only", "gpt-4", 0.7f, true, false, 0.7f));
            }
            
            // Initialize output directory
            outputPath = Path.Combine(Application.persistentDataPath, outputFolder);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
        }
                
                var result = new ExperimentResult
                {
                    experimentName = experiment.experimentName,
                    startTime = DateTime.UtcNow
                };
                
                try
                {
                    Debug.Log($"[AGEL] Starting experiment: {experiment.experimentName}");
                    
                    // Run the experiment (can be coroutine for async operations)
                    var enumerator = experiment.RunExperiment();
                    while (enumerator.MoveNext())
                    {
                        status = enumerator.Current?.ToString() ?? "Running...";
                        yield return null; // Wait for next frame
                    }
                    
                    result.success = true;
                    result.message = "Completed successfully";
                }
                catch (Exception ex)
                {
                    result.success = false;
                    result.message = $"Error: {ex.Message}";
                    Debug.LogError($"[AGEL] Experiment failed: {experiment.experimentName}\n{ex}");
                }
                finally
                {
                    result.endTime = DateTime.UtcNow;
                    results.Add(result);
                    completedCount++;
                    
                    if (saveToFile)
                    {
                        SaveResult(result);
                    }
                    
                    status = result.success ? "Completed" : "Failed";
                    Debug.Log($"[AGEL] Experiment {status}: {experiment.experimentName} ({(result.endTime - result.startTime).TotalSeconds:F2}s)");
                }
                
                yield return null; // Ensure UI updates between experiments
            }
            
            currentExperiment = "All experiments completed";
            status = $"Completed {completedCount}/{totalCount} experiments";
            Debug.Log($"[AGEL] {status}");
        }
        
        private void SaveResult(ExperimentResult result)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"exp_{timestamp}_{result.experimentName.Replace(" ", "_")}.json";
                var filePath = Path.Combine(outputPath, fileName);
                
                var json = JsonUtility.ToJson(result, true);
                File.WriteAllText(filePath, json);
                
                Debug.Log($"[AGEL] Saved results to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AGEL] Failed to save results: {ex.Message}");
            }
        }
    }
    
    [System.Serializable]
    public class ExperimentResult
    {
        public string experimentName;
        public bool success;
        public string message;
        public DateTime startTime;
        public DateTime endTime;
        public Dictionary<string, object> metrics = new Dictionary<string, object>();
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute { }
}

#if UNITY_EDITOR
namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif
