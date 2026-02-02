using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGEL.Multimodal;

namespace AGEL.Experiments
{
    [CreateAssetMenu(menuName = "AGEL/Multimodal Experiment")]
    public class MultimodalExperiment : AGELExperiment
    {
        [Header("Model Configuration")]
        public MultimodalManager multimodalManager;
        public List<ModelType> modelsToTest = new List<ModelType>();
        
        [Header("Experiment Settings")]
        public int trialsPerModel = 5;
        public float timeLimitPerTrial = 120f; // seconds
        
        [Header("Test Cases")]
        public List<TestScenario> testScenarios = new List<TestScenario>();
        
        [Header("Results")]
        public List<ExperimentResult> results = new List<ExperimentResult>();
        
        [System.Serializable]
        public class TestScenario
        {
            public string name;
            public string description;
            public MediaContent[] inputMessages;
            public string expectedResponsePattern;
            public string systemPrompt;
        }
        
        [System.Serializable]
        public class ExperimentResult
        {
            public string testName;
            public ModelType modelType;
            public bool success;
            public float responseTime;
            public int tokensUsed;
            public string response;
            public string error;
            public string expectedPattern;
            public bool patternMatched;
            
            // For analysis
            public float similarityScore;
            public List<string> extractedInfo;
        }
        
        public override IEnumerator Run()
        {
            if (multimodalManager == null)
            {
                Debug.LogError("MultimodalManager reference is missing!");
                yield break;
            }
            
            // Initialize the multimodal manager
            yield return multimodalManager.Initialize();
            
            // Run experiments for each model
            foreach (var modelType in modelsToTest)
            {
                // Switch to the target model
                yield return multimodalManager.SwitchModel(modelType);
                
                // Run each test scenario
                foreach (var scenario in testScenarios)
                {
                    for (int i = 0; i < trialsPerModel; i++)
                    {
                        yield return RunTestScenario(modelType, scenario);
                        yield return new WaitForSeconds(1f); // Small delay between trials
                    }
                }
            }
            
            // Analyze results
            AnalyzeResults();
            
            // Save results
            SaveResults();
        }
        
        private IEnumerator RunTestScenario(ModelType modelType, TestScenario scenario)
        {
            var result = new ExperimentResult
            {
                testName = scenario.name,
                modelType = modelType,
                expectedPattern = scenario.expectedResponsePattern
            };
            
            float startTime = Time.time;
            bool completed = false;
            
            // Start the test
            multimodalManager.GenerateResponse(
                scenario.inputMessages,
                response => {
                    result.responseTime = Time.time - startTime;
                    result.success = response.success;
                    
                    if (response.success)
                    {
                        result.response = response.content;
                        result.tokensUsed = response.tokensUsed;
                        
                        // Check if response matches expected pattern
                        result.patternMatched = !string.IsNullOrEmpty(scenario.expectedResponsePattern) && 
                                             System.Text.RegularExpressions.Regex.IsMatch(
                                                 response.content, 
                                                 scenario.expectedResponsePattern, 
                                                 System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        // Additional analysis can be added here
                        result.similarityScore = CalculateSimilarity(
                            response.content, 
                            scenario.expectedResponsePattern);
                    }
                    else
                    {
                        result.error = response.error;
                    }
                    
                    completed = true;
                },
                modelType,
                null, // use default temperature
                null, // use default max tokens
                scenario.systemPrompt
            );
            
            // Wait for completion or timeout
            float timeout = Time.time + timeLimitPerTrial;
            while (!completed && Time.time < timeout)
            {
                yield return null;
            }
            
            if (!completed)
            {
                result.error = "Test timed out";
                result.responseTime = timeLimitPerTrial;
            }
            
            // Record the result
            results.Add(result);
        }
        
        private float CalculateSimilarity(string response, string expectedPattern)
        {
            // Simple similarity metric - can be replaced with more sophisticated NLP metrics
            if (string.IsNullOrEmpty(response) || string.IsNullOrEmpty(expectedPattern))
                return 0f;
                
            // Simple word overlap
            var responseWords = new HashSet<string>(response.ToLower().Split(' ', ',', '.', '!', '?'));
            var expectedWords = new HashSet<string>(expectedPattern.ToLower().Split(' ', ',', '.', '!', '?'));
            
            int intersection = 0;
            foreach (var word in responseWords)
            {
                if (expectedWords.Contains(word))
                    intersection++;
            }
            
            int union = responseWords.Count + expectedWords.Count - intersection;
            
            return union > 0 ? (float)intersection / union : 0f;
        }
        
        private void AnalyzeResults()
        {
            // Calculate statistics for each model and test scenario
            var modelStats = new Dictionary<ModelType, Dictionary<string, List<ExperimentResult>>>();
            
            foreach (var result in results)
            {
                if (!modelStats.ContainsKey(result.modelType))
                    modelStats[result.modelType] = new Dictionary<string, List<ExperimentResult>>();
                
                if (!modelStats[result.modelType].ContainsKey(result.testName))
                    modelStats[result.modelType][result.testName] = new List<ExperimentResult>();
                
                modelStats[result.modelType][result.testName].Add(result);
            }
            
            // Log summary statistics
            Debug.Log("\n=== Experiment Results ===");
            foreach (var model in modelStats)
            {
                Debug.Log($"\nModel: {model.Key}");
                
                foreach (var test in model.Value)
                {
                    int successCount = test.Value.Count(r => r.success && r.patternMatched);
                    float avgResponseTime = test.Value.Average(r => r.responseTime);
                    float avgSimilarity = test.Value.Average(r => r.similarityScore);
                    
                    Debug.Log($"  {test.Key}: {successCount}/{test.Value.Count} passed, " +
                             $"Avg time: {avgResponseTime:F2}s, " +
                             $"Avg similarity: {avgSimilarity:P0}");
                }
            }
        }
        
        private void SaveResults()
        {
            // Save results to a JSON file
            string json = JsonUtility.ToJson(this, true);
            string path = System.IO.Path.Combine(
                Application.persistentDataPath,
                $"AGEL_Experiment_{System.DateTime.Now:yyyyMMdd_HHmmss}.json"
            );
            
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"Results saved to: {path}");
        }
    }
}
