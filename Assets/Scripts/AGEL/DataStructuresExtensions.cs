using System;
using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    // Extensions to the existing Episode class to support enhanced functionality
    public partial class Episode
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public List<Command> actions { get; set; } = new List<Command>();
        public List<string> stateChanges { get; set; } = new List<string>();
        
        // Enhanced constructor
        public Episode(State state, ActionPlan actionPlan, Feedback feedback, DateTime? timestamp = null) 
            : this(state, actionPlan, feedback)
        {
            this.Timestamp = timestamp ?? DateTime.Now;
            
            // Convert action plan to commands for compatibility
            if (actionPlan?.actions != null)
            {
                foreach (var action in actionPlan.actions)
                {
                    this.actions.Add(new Command(action));
                }
            }
            
            // Analyze state changes
            AnalyzeStateChanges(state, feedback);
        }
        
        private void AnalyzeStateChanges(State state, Feedback feedback)
        {
            stateChanges = new List<string>();
            
            if (feedback.healthChange != 0)
            {
                stateChanges.Add($"health_change:{feedback.healthChange}");
            }
            
            if (feedback.damageTaken > 0)
            {
                stateChanges.Add($"damage_taken:{feedback.damageTaken}");
            }
            
            if (!feedback.success)
            {
                stateChanges.Add("action_failed");
            }
            
            if (state.playerHealth < state.playerMaxHealth * 0.3f)
            {
                stateChanges.Add("low_health_state");
            }
            
            if (state.environmentState?.ContainsKey("nearbyEnemies") == true && 
                (int)state.environmentState["nearbyEnemies"] > 0)
            {
                stateChanges.Add("enemies_present");
            }
            
            if (state.environmentState?.ContainsKey("nearbyHazards") == true && 
                (int)state.environmentState["nearbyHazards"] > 0)
            {
                stateChanges.Add("hazards_present");
            }
        }
        
        public float GetImportanceScore()
        {
            float importance = feedback.intensity;
            
            // Boost importance for significant events
            if (Math.Abs(feedback.healthChange) > 5) importance += 0.3f;
            if (feedback.damageTaken > 0) importance += 0.2f;
            if (!feedback.success) importance += 0.1f;
            if (stateChanges.Contains("low_health_state")) importance += 0.4f;
            
            return Mathf.Clamp01(importance);
        }
        
        public bool IsNegativeOutcome()
        {
            return feedback.healthChange < 0 || feedback.damageTaken > 0 || !feedback.success;
        }
        
        public bool IsPositiveOutcome()
        {
            return feedback.healthChange > 0 && feedback.success && feedback.damageTaken == 0;
        }
        
        public string GetSemanticSummary()
        {
            var summary = new List<string>();
            
            // Action summary
            if (actionPlan?.actions?.Count > 0)
            {
                summary.Add($"actions:[{string.Join(",", actionPlan.actions)}]");
            }
            
            // Outcome summary
            if (IsNegativeOutcome())
            {
                summary.Add("outcome:negative");
            }
            else if (IsPositiveOutcome())
            {
                summary.Add("outcome:positive");
            }
            else
            {
                summary.Add("outcome:neutral");
            }
            
            // State changes
            if (stateChanges.Count > 0)
            {
                summary.Add($"changes:[{string.Join(",", stateChanges)}]");
            }
            
            return string.Join(" ", summary);
        }
    }
    
    // Extensions for better CPG integration
    public static class CausalProgramGraphExtensions
    {
        public static bool IsFact(this CausalProgramGraph cpg, string goal)
        {
            return cpg.Facts.Contains(CausalProgramGraph.Normalize(goal));
        }
        
        public static IEnumerable<string> GetMatchingRules(this CausalProgramGraph cpg, string goal)
        {
            var normalized = CausalProgramGraph.Normalize(goal);
            var matchingRules = new List<string>();
            
            foreach (var rule in cpg.Rules)
            {
                // Simple pattern matching - in practice would use proper unification
                if (rule.Head.Contains(normalized.Split('(')[0]) || 
                    normalized.Contains(rule.Head.Split('(')[0]))
                {
                    matchingRules.Add($"{rule.Head}:-{string.Join(",", rule.Body)}");
                }
            }
            
            return matchingRules;
        }
        
        public static bool AddRuleOrFact(this CausalProgramGraph cpg, string ruleOrFact, float weight, string description)
        {
            try
            {
                cpg.AddRuleOrFact(ruleOrFact, weight, description);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    // Enhanced training example for NTP
    public class EnhancedTrainingExample : TrainingExample
    {
        public string Context { get; }
        public List<string> NegativeExamples { get; }
        public Dictionary<string, object> Metadata { get; }
        
        public EnhancedTrainingExample(string goal, float targetScore, string context = "", 
            List<string> negativeExamples = null, float importance = 1.0f) 
            : base(goal, targetScore, importance)
        {
            Context = context ?? "";
            NegativeExamples = negativeExamples ?? new List<string>();
            Metadata = new Dictionary<string, object>();
        }
        
        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }
        
        public T GetMetadata<T>(string key, T defaultValue = default(T))
        {
            return Metadata.TryGetValue(key, out var value) && value is T ? (T)value : defaultValue;
        }
    }
}