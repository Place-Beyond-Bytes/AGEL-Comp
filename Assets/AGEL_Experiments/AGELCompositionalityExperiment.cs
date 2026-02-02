using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AGEL.Experiments
{
    [CreateAssetMenu(menuName = "AGEL/Compositionality Experiment")]
    public class AGELCompositionalityExperiment : AGELExperiment
    {
        [Header("Compositionality Settings")]
        [Tooltip("Test for systematicity by requiring novel combinations of known primitives")]
        public bool testSystematicity = true;
        
        [Tooltip("Test for productivity by requiring longer sequences than seen in training")]
        public bool testProductivity = true;
        
        [Tooltip("Test for robustness to ambiguous or stochastic events")]
        public bool testAmbiguity = true;
        
        [Header("Task Parameters")]
        public List<PrimitiveTask> primitiveTasks = new List<PrimitiveTask>();
        public List<CompositeTask> compositeTasks = new List<CompositeTask>();
        
        [Header("Evaluation Metrics")]
        [ReadOnly] public float successRate;
        [ReadOnly] public float firstTrySuccessRate;
        [ReadOnly] public float sampleEfficiency;
        [ReadOnly] public float adaptationSpeed;
        [ReadOnly] public float ruleAccuracy;
        [ReadOnly] public float ntpVetoRate;
        
        // Runtime state
        private List<TaskResult> taskResults = new List<TaskResult>();
        private Dictionary<string, int> primitiveSuccessCounts = new Dictionary<string, int>();
        private Dictionary<string, int> primitiveAttemptCounts = new Dictionary<string, int>();
        
        public override IEnumerator Run()
        {
            // Initialize metrics
            InitializeMetrics();
            
            // Run primitive tasks first to establish baseline knowledge
            yield return RunPrimitiveTasks();
            
            // Run composite tasks to test compositionality
            if (testSystematicity || testProductivity)
            {
                yield return RunCompositeTasks();
            }
            
            // Run ambiguous/stochastic tasks if enabled
            if (testAmbiguity)
            {
                yield return RunAmbiguityTests();
            }
            
            // Calculate final metrics
            CalculateMetrics();
        }
        
        private void InitializeMetrics()
        {
            taskResults.Clear();
            primitiveSuccessCounts.Clear();
            primitiveAttemptCounts.Clear();
            
            // Initialize primitive task tracking
            foreach (var task in primitiveTasks)
            {
                primitiveSuccessCounts[task.taskId] = 0;
                primitiveAttemptCounts[task.taskId] = 0;
            }
        }
        
        private IEnumerator RunPrimitiveTasks()
        {
            foreach (var task in primitiveTasks)
            {
                var result = new TaskResult 
                { 
                    taskId = task.taskId, 
                    taskType = TaskType.Primitive,
                    startTime = Time.time
                };
                
                // Run the task
                yield return ExecuteTask(task, result);
                
                // Update metrics
                if (result.success)
                {
                    primitiveSuccessCounts[task.taskId]++;
                    if (result.attempts == 1) result.firstTrySuccess = true;
                }
                
                primitiveAttemptCounts[task.taskId]++;
                taskResults.Add(result);
                
                // Log results
                LogTaskResult(task, result);
                
                // Small delay between tasks
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private IEnumerator RunCompositeTasks()
        {
            foreach (var compositeTask in compositeTasks)
            {
                // Skip if this composite task doesn't match our testing criteria
                if ((testSystematicity && compositeTask.requiresSystematicity) || 
                    (testProductivity && compositeTask.requiresProductivity))
                {
                    var result = new TaskResult 
                    { 
                        taskId = compositeTask.taskId, 
                        taskType = compositeTask.requiresSystematicity ? 
                            TaskType.Systematicity : TaskType.Productivity,
                        startTime = Time.time
                    };
                    
                    // Run the composite task
                    yield return ExecuteCompositeTask(compositeTask, result);
                    
                    // Update metrics
                    taskResults.Add(result);
                    LogTaskResult(compositeTask, result);
                    
                    // Small delay between tasks
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        private IEnumerator RunAmbiguityTests()
        {
            // Create ambiguous/stochastic test cases
            var ambiguousTasks = CreateAmbiguityTestCases();
            
            foreach (var task in ambiguousTasks)
            {
                var result = new TaskResult 
                { 
                    taskId = task.taskId, 
                    taskType = TaskType.Ambiguity,
                    startTime = Time.time
                };
                
                // Run the ambiguous task
                yield return ExecuteTask(task, result);
                
                // Update metrics specific to ambiguity testing
                taskResults.Add(result);
                LogTaskResult(task, result);
                
                // Small delay between tasks
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private IEnumerator ExecuteTask(TaskDefinition task, TaskResult result)
        {
            int attempts = 0;
            bool taskCompleted = false;
            
            while (attempts < maxAttemptsPerTask && !taskCompleted)
            {
                attempts++;
                
                // Get agent's plan
                var plan = agent.PlanNextAction(task.parameters);
                
                // Execute action and get feedback
                var feedback = ExecuteAction(plan);
                
                // Process feedback
                if (feedback.isSuccess)
                {
                    taskCompleted = true;
                    result.success = true;
                    result.attempts = attempts;
                    
                    // Log successful primitive combination for systematicity analysis
                    if (task is CompositeTask compositeTask)
                    {
                        result.usedPrimitives = compositeTask.requiredPrimitives;
                    }
                }
                
                // Learn from feedback if the agent supports it
                    if (agent is ILearnableAgent learnableAgent)
                    {
                        var learningResult = learnableAgent.LearnFromFeedback(feedback);
                        result.rulesLearned += learningResult.rulesAdded;
                        result.rulesRetracted += learningResult.rulesRetracted;
                    }
                    
                    // Track NTP vetoes if applicable
                    if (feedback.wasVetoedByNTP)
                    {
                        result.ntpVetoes++;
                    }
                    
                    // Update status and yield
                    status = $"Task {task.taskId} - Attempt {attempts}: {feedback.message}";
                    yield return null;
                }
                
                if (!taskCompleted)
                {
                    result.success = false;
                    result.attempts = attempts;
                    result.failureReason = $"Failed after {attempts} attempts";
                }
                
                // Calculate task duration
                result.duration = Time.time - result.startTime;
                
                // Update metrics
                UpdateTaskMetrics(task, result);
            }
            
            private IEnumerator ExecuteCompositeTask(CompositeTask task, TaskResult result)
            {
                int attempts = 0;
                bool taskCompleted = false;
                
                while (attempts < maxAttemptsPerTask && !taskCompleted)
                {
                    attempts++;
                    bool allPrimitivesSuccessful = true;
                    
                    // Execute each required primitive in sequence
                    foreach (var primitiveId in task.requiredPrimitives)
                    {
                        var primitive = primitiveTasks.FirstOrDefault(p => p.taskId == primitiveId);
                        if (primitive == null) continue;
                        
                        var primitiveResult = new TaskResult 
                        { 
                            taskId = $"{task.taskId}_{primitiveId}",
                            taskType = TaskType.Primitive,
                            startTime = Time.time
                        };
                        
                        // Execute the primitive
                        yield return ExecuteTask(primitive, primitiveResult);
                        
                        // Check if primitive was successful
                        if (!primitiveResult.success)
                        {
                            allPrimitivesSuccessful = false;
                            break;
                        }
                        
                        // Small delay between primitives
                        yield return new WaitForSeconds(0.1f);
                    }
                    
                    // Check if all primitives were successful
                    if (allPrimitivesSuccessful)
                    {
                        taskCompleted = true;
                        result.success = true;
                        result.attempts = attempts;
                        result.usedPrimitives = new List<string>(task.requiredPrimitives);
                    }
                }
                
                if (!taskCompleted)
                {
                    result.success = false;
                    result.attempts = attempts;
                    result.failureReason = $"Failed to execute one or more required primitives after {attempts} attempts";
                }
                
                // Calculate task duration
                result.duration = Time.time - result.startTime;
                
                // Update metrics
                UpdateTaskMetrics(task, result);
            }
            
            private List<TaskDefinition> CreateAmbiguityTestCases()
            {
                var ambiguousTasks = new List<TaskDefinition>();
                
                // Example: Create tasks with multiple potential causes for feedback
                ambiguousTasks.Add(new TaskDefinition
                {
                    taskId = "ambiguous_damage_source",
                    description = "Determine correct damage source when multiple are present",
                    parameters = new Dictionary<string, object>
                    {
                        ["hazards"] = new string[] { "fire", "poison_gas", "spikes" },
                        ["expected_damage_source"] = "fire"  // The correct answer
                    }
                });
                
                // Add more ambiguous test cases as needed
                
                return ambiguousTasks;
            }
            
            private void UpdateTaskMetrics(TaskDefinition task, TaskResult result)
            {
                // Update success rates
                if (result.success)
                {
                    if (result.attempts == 1)
                    {
                        result.firstTrySuccess = true;
                    }
                    
                    // Update success count for primitives
                    if (task is PrimitiveTask)
                    {
                        primitiveSuccessCounts[task.taskId]++;
                    }
                }
                
                // Update attempt counts
                if (task is PrimitiveTask)
                {
                    primitiveAttemptCounts[task.taskId]++;
                }
            }
            
            private void CalculateMetrics()
            {
                if (taskResults.Count == 0) return;
                
                // Calculate overall success rate
                int successfulTasks = taskResults.Count(r => r.success);
                successRate = (float)successfulTasks / taskResults.Count;
                
                // Calculate first-try success rate
                int firstTrySuccesses = taskResults.Count(r => r.firstTrySuccess);
                firstTrySuccessRate = (float)firstTrySuccesses / taskResults.Count;
                
                // Calculate sample efficiency (actions per task)
                float totalActions = taskResults.Sum(r => r.attempts);
                sampleEfficiency = totalActions / taskResults.Count;
                
                // Calculate adaptation speed (lower is better)
                float totalFailedActions = taskResults.Sum(r => r.attempts - (r.success ? 1 : 0));
                adaptationSpeed = totalFailedActions / taskResults.Count;
                
                // Calculate rule accuracy (if any rules were learned)
                int totalRulesLearned = taskResults.Sum(r => r.rulesLearned);
                int totalRulesRetracted = taskResults.Sum(r => r.rulesRetracted);
                ruleAccuracy = totalRulesLearned > 0 ? 
                    1f - ((float)totalRulesRetracted / totalRulesLearned) : 1f;
                
                // Calculate NTP veto rate
                float totalNTPVetoes = taskResults.Sum(r => r.ntpVetoes);
                ntpVetoRate = totalActions > 0 ? totalNTPVetoes / totalActions : 0f;
                
                // Log metrics
                Log($"\n=== Experiment Metrics ===");
                Log($"Success Rate: {successRate:P1}");
                Log($"First-Try Success Rate: {firstTrySuccessRate:P1}");
                Log($"Sample Efficiency: {sampleEfficiency:F2} actions/task");
                Log($"Adaptation Speed: {adaptationSpeed:F2} (lower is better)");
                Log($"Rule Accuracy: {ruleAccuracy:P1}");
                Log($"NTP Veto Rate: {ntpVetoRate:P1}");
            }
            
            private void LogTaskResult(TaskDefinition task, TaskResult result)
            {
                string resultType = result.taskType switch
                {
                    TaskType.Primitive => "Primitive",
                    TaskType.Systematicity => "Systematicity",
                    TaskType.Productivity => "Productivity",
                    TaskType.Ambiguity => "Ambiguity",
                    _ => "Unknown"
                };
                
                string status = result.success ? "SUCCESS" : "FAILED";
                string attempts = $"({result.attempts} attempt{(result.attempts != 1 ? "s" : "")})";
                string firstTry = result.firstTrySuccess ? " (First Try!)" : "";
                string details = $"{resultType} Task: {task.taskId} - {status} {attempts}{firstTry}";
                
                if (result.success)
                {
                    Log(details);
                }
                else
                {
                    LogError($"{details} - {result.failureReason}");
                }
                
                // Log additional details if available
                if (result.usedPrimitives != null && result.usedPrimitives.Count > 0)
                {
                    Log($"  Used Primitives: {string.Join(", ", result.usedPrimitives)}");
                }
                
                if (result.rulesLearned > 0 || result.rulesRetracted > 0)
                {
                    Log($"  Rules: +{result.rulesLearned}/-{result.rulesRetracted}");
                }
                
                if (result.ntpVetoes > 0)
                {
                    Log($"  NTP Vetoes: {result.ntpVetoes}");
                }
                
                Log($"  Duration: {result.duration:F2}s");
            }
        }
        
        [System.Serializable]
        public class TaskDefinition
        {
            public string taskId;
            public string description;
            public Dictionary<string, object> parameters = new Dictionary<string, object>();
        }
        
        [System.Serializable]
        public class PrimitiveTask : TaskDefinition
        {
            // Additional primitive-specific properties can be added here
        }
        
        [System.Serializable]
        public class CompositeTask : TaskDefinition
        {
            public bool requiresSystematicity;
            public bool requiresProductivity;
            public List<string> requiredPrimitives = new List<string>();
        }
        
        public class TaskResult
        {
            public string taskId;
            public TaskType taskType;
            public bool success;
            public bool firstTrySuccess;
            public int attempts;
            public float duration;
            public string failureReason;
            public List<string> usedPrimitives = new List<string>();
            public int rulesLearned;
            public int rulesRetracted;
            public int ntpVetoes;
            public float startTime;
        }
        
        public enum TaskType
        {
            Primitive,
            Systematicity,
            Productivity,
            Ambiguity
        }
    }
