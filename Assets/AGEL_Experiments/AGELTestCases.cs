using System.Collections.Generic;
using UnityEngine;

namespace AGEL.Experiments
{
    [CreateAssetMenu(menuName = "AGEL/Test Cases")]
    public class AGELTestCases : ScriptableObject
    {
        [Header("H1: Handling Ambiguity")]
        public List<PrimitiveTask> ambiguityTestPrimitives = new List<PrimitiveTask>
        {
            new PrimitiveTask
            {
                taskId = "identify_hazard",
                description = "Identify which object in the environment is harmful",
                parameters = new Dictionary<string, object>
                {
                    ["environment"] = new string[] { "fire", "barrel", "mushroom" },
                    ["expected_hazard"] = "fire"
                }
            }
        };

        [Header("H2a: NTP Verification Impact")]
        public List<PrimitiveTask> ntpTestPrimitives = new List<PrimitiveTask>
        {
            new PrimitiveTask
            {
                taskId = "avoid_contradiction",
                description = "Avoid taking actions that contradict learned rules",
                parameters = new Dictionary<string, object>
                {
                    ["learned_rule"] = "fire_causes_damage",
                    ["action"] = "walk_into_fire"
                }
            }
        };

        [Header("H2b: ILP Learning Impact")]
        public List<CompositeTask> systematicityTests = new List<CompositeTask>
        {
            new CompositeTask
            {
                taskId = "compose_learned_actions",
                description = "Combine learned actions in novel ways",
                requiresSystematicity = true,
                requiredPrimitives = new List<string> { "pickup_item", "use_item" },
                parameters = new Dictionary<string, object>
                {
                    ["novel_combination"] = true,
                    ["expected_behavior"] = "use_learned_rules"
                }
            }
        };

        [Header("Productivity Tests")]
        public List<CompositeTask> productivityTests = new List<CompositeTask>
        {
            new CompositeTask
            {
                taskId = "extended_sequence",
                description = "Execute a sequence longer than any seen in training",
                requiresProductivity = true,
                requiredPrimitives = new List<string> 
                { 
                    "navigate_to", "pickup_item", "combine_items", 
                    "use_item", "interact_with_npc" 
                },
                parameters = new Dictionary<string, object>
                {
                    ["sequence_length"] = 5,
                    ["novel_sequence"] = true
                }
            }
        };

        [Header("Rule Learning Tests")]
        public List<PrimitiveTask> ruleLearningTests = new List<PrimitiveTask>
        {
            new PrimitiveTask
            {
                taskId = "learn_from_single_example",
                description = "Learn a new rule from minimal examples",
                parameters = new Dictionary<string, object>
                {
                    ["training_examples"] = 1,
                    ["test_scenario"] = "novel_object",
                    ["expected_rule"] = "novel_object_property"
                }
            }
        };

        public AGELCompositionalityExperiment CreateCompositionalityTest()
        {
            var experiment = ScriptableObject.CreateInstance<AGELCompositionalityExperiment>();
            
            // Set up the experiment with all test cases
            experiment.name = "AGEL_Compositionality_Test";
            experiment.testSystematicity = true;
            experiment.testProductivity = true;
            experiment.testAmbiguity = true;
            
            // Add all primitive tasks
            var allPrimitives = new List<PrimitiveTask>();
            allPrimitives.AddRange(ambiguityTestPrimitives);
            allPrimitives.AddRange(ntpTestPrimitives);
            allPrimitives.AddRange(ruleLearningTests);
            
            // Remove duplicates by taskId
            var uniquePrimitives = new Dictionary<string, PrimitiveTask>();
            foreach (var task in allPrimitives)
            {
                if (!uniquePrimitives.ContainsKey(task.taskId))
                {
                    uniquePrimitives[task.taskId] = task;
                }
            }
            
            experiment.primitiveTasks = new List<PrimitiveTask>(uniquePrimitives.Values);
            
            // Add composite tasks
            var allComposites = new List<CompositeTask>();
            allComposites.AddRange(systematicityTests);
            allComposites.AddRange(productivityTests);
            
            // Remove duplicates by taskId
            var uniqueComposites = new Dictionary<string, CompositeTask>();
            foreach (var task in allComposites)
            {
                if (!uniqueComposites.ContainsKey(task.taskId))
                {
                    uniqueComposites[task.taskId] = task;
                }
            }
            
            experiment.compositeTasks = new List<CompositeTask>(uniqueComposites.Values);
            
            return experiment;
        }
    }
}
