using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    public class AGELGrounding : MonoBehaviour
    {
        [Header("Grounding Settings")]
        public float minFeedbackIntensity = 0.3f;
        public float ruleWeightMultiplier = 1.0f;
        
        [Header("Debug")]
        public bool enableDebugLogs = false;
        
        public List<FOLRule> GenerateRules(Episode episode)
        {
            List<FOLRule> newRules = new List<FOLRule>();
            
            try
            {
                // Only generate rules from episodes with significant feedback
                if (episode.feedback.intensity < minFeedbackIntensity)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"AGEL Grounding: Episode feedback intensity ({episode.feedback.intensity}) below threshold ({minFeedbackIntensity}), skipping rule generation");
                    }
                    return newRules;
                }
                
                // Analyze the episode for cause-effect relationships
                newRules.AddRange(AnalyzeHealthEffects(episode));
                newRules.AddRange(AnalyzeInventoryEffects(episode));
                newRules.AddRange(AnalyzeEnvironmentalEffects(episode));
                newRules.AddRange(AnalyzeActionPatterns(episode));
                
                if (enableDebugLogs && newRules.Count > 0)
                {
                    Debug.Log($"AGEL Grounding: Generated {newRules.Count} new rules from episode");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error in AGEL Grounding: " + e.Message);
            }
            
            return newRules;
        }
        
        private List<FOLRule> AnalyzeHealthEffects(Episode episode)
        {
            List<FOLRule> rules = new List<FOLRule>();
            
            // Analyze health changes
            if (episode.feedback.healthChange < 0)
            {
                // Negative health change - look for harmful actions or items
                float weight = Mathf.Abs(episode.feedback.healthChange) / 10f * episode.feedback.intensity * ruleWeightMultiplier;
                weight = Mathf.Clamp(weight, 0.1f, 1.0f);
                
                // Check if mushrooms were involved
                foreach (var item in episode.state.inventoryItems)
                {
                    if (item.itemName.ToLower().Contains("mushroom"))
                    {
                        rules.Add(new FOLRule("is_harmful(mushroom)", weight, "Mushrooms cause health damage"));
                        rules.Add(new FOLRule("causes_harm(consuming(mushroom))", weight, "Consuming mushrooms is harmful"));
                        break;
                    }
                }
                
                // Check for other harmful patterns
                if (episode.feedback.damageTaken > 0)
                {
                    rules.Add(new FOLRule("causes_harm(unsafe_action)", weight, "Unsafe actions cause damage"));
                }
            }
            else if (episode.feedback.healthChange > 0)
            {
                // Positive health change - look for beneficial actions or items
                float weight = episode.feedback.healthChange / 10f * episode.feedback.intensity * ruleWeightMultiplier;
                weight = Mathf.Clamp(weight, 0.1f, 1.0f);
                
                // Check for healing items
                foreach (var item in episode.state.inventoryItems)
                {
                    if (item.currentHealth > 0 && !item.itemName.ToLower().Contains("mushroom"))
                    {
                        rules.Add(new FOLRule("is_beneficial(healing_item)", weight, "Healing items restore health"));
                        rules.Add(new FOLRule("causes_benefit(consuming(healing_item))", weight, "Consuming healing items is beneficial"));
                        break;
                    }
                }
            }
            
            return rules;
        }
        
        private List<FOLRule> AnalyzeInventoryEffects(Episode episode)
        {
            List<FOLRule> rules = new List<FOLRule>();
            
            // Analyze inventory changes and their effects
            foreach (var item in episode.state.inventoryItems)
            {
                if (item.itemName.ToLower().Contains("mushroom"))
                {
                    // Mushroom-related rules
                    if (episode.feedback.healthChange < 0)
                    {
                        float weight = episode.feedback.intensity * ruleWeightMultiplier;
                        rules.Add(new FOLRule("should_avoid(mushroom)", weight, "Mushrooms should be avoided"));
                        rules.Add(new FOLRule("is_poisonous(mushroom)", weight, "Mushrooms are poisonous"));
                    }
                }
                else if (item.currentHealth > 0)
                {
                    // Healing item rules
                    if (episode.feedback.healthChange > 0)
                    {
                        float weight = episode.feedback.intensity * ruleWeightMultiplier;
                        rules.Add(new FOLRule("should_use_when_low_health(healing_item)", weight, "Use healing items when health is low"));
                    }
                }
            }
            
            return rules;
        }
        
        private List<FOLRule> AnalyzeEnvironmentalEffects(Episode episode)
        {
            List<FOLRule> rules = new List<FOLRule>();
            
            // Analyze environmental factors
            if (episode.state.environmentState.ContainsKey("nearbyHazards") && 
                (int)episode.state.environmentState["nearbyHazards"] > 0)
            {
                if (episode.feedback.healthChange < 0)
                {
                    float weight = episode.feedback.intensity * ruleWeightMultiplier;
                    rules.Add(new FOLRule("hazards_are_dangerous", weight, "Hazards in the environment are dangerous"));
                    rules.Add(new FOLRule("should_avoid_hazards", weight, "Should avoid environmental hazards"));
                }
            }
            
            if (episode.state.environmentState.ContainsKey("nearbyEnemies") && 
                (int)episode.state.environmentState["nearbyEnemies"] > 0)
            {
                if (episode.feedback.healthChange < 0)
                {
                    float weight = episode.feedback.intensity * ruleWeightMultiplier;
                    rules.Add(new FOLRule("enemies_are_threatening", weight, "Enemies pose a threat"));
                    rules.Add(new FOLRule("should_retreat_from_enemies_when_low_health", weight, "Retreat from enemies when health is low"));
                }
            }
            
            // Low health situation rules
            if (episode.state.playerHealth < episode.state.playerMaxHealth * 0.3f)
            {
                float weight = episode.feedback.intensity * ruleWeightMultiplier;
                rules.Add(new FOLRule("low_health_requires_caution", weight, "Low health requires cautious behavior"));
                rules.Add(new FOLRule("should_prioritize_healing_when_low_health", weight, "Prioritize healing when health is low"));
            }
            
            return rules;
        }
        
        private List<FOLRule> AnalyzeActionPatterns(Episode episode)
        {
            List<FOLRule> rules = new List<FOLRule>();
            
            // Analyze action patterns and their outcomes
            foreach (string action in episode.actionPlan.actions)
            {
                float weight = episode.feedback.intensity * ruleWeightMultiplier;
                
                switch (action.ToLower())
                {
                    case "use_healing_item":
                        if (episode.feedback.healthChange > 0)
                        {
                            rules.Add(new FOLRule("healing_actions_are_beneficial", weight, "Using healing items is beneficial"));
                        }
                        break;
                        
                    case "avoid_consuming_mushrooms":
                        if (episode.feedback.healthChange >= 0)
                        {
                            rules.Add(new FOLRule("avoiding_mushrooms_is_safe", weight, "Avoiding mushrooms prevents harm"));
                        }
                        break;
                        
                    case "maintain_safety":
                        if (episode.feedback.success)
                        {
                            rules.Add(new FOLRule("safety_actions_prevent_harm", weight, "Safety actions prevent harm"));
                        }
                        break;
                        
                    case "retreat_from_enemies":
                        if (episode.feedback.healthChange >= 0)
                        {
                            rules.Add(new FOLRule("retreating_from_enemies_is_safe", weight, "Retreating from enemies is safe"));
                        }
                        break;
                        
                    case "avoid_hazards":
                        if (episode.feedback.healthChange >= 0)
                        {
                            rules.Add(new FOLRule("avoiding_hazards_is_safe", weight, "Avoiding hazards prevents damage"));
                        }
                        break;
                }
            }
            
            // Analyze confidence levels
            if (episode.actionPlan.confidence > 0.8f && episode.feedback.success)
            {
                float weight = episode.feedback.intensity * ruleWeightMultiplier;
                rules.Add(new FOLRule("high_confidence_actions_are_reliable", weight, "High confidence actions are reliable"));
            }
            else if (episode.actionPlan.confidence < 0.3f && !episode.feedback.success)
            {
                float weight = episode.feedback.intensity * ruleWeightMultiplier;
                rules.Add(new FOLRule("low_confidence_actions_are_risky", weight, "Low confidence actions are risky"));
            }
            
            return rules;
        }
        
        // Helper methods for specific grounding scenarios
        public bool ShouldGenerateRule(Episode episode)
        {
            return episode.feedback.intensity >= minFeedbackIntensity;
        }
        
        public float CalculateRuleWeight(Episode episode)
        {
            float baseWeight = episode.feedback.intensity;
            float healthFactor = Mathf.Abs(episode.feedback.healthChange) / 10f;
            float damageFactor = episode.feedback.damageTaken / 10f;
            
            float totalWeight = (baseWeight + healthFactor + damageFactor) * ruleWeightMultiplier;
            return Mathf.Clamp(totalWeight, 0.1f, 1.0f);
        }
        
        public List<FOLRule> GenerateSafetyRules(Episode episode)
        {
            List<FOLRule> safetyRules = new List<FOLRule>();
            
            if (episode.feedback.healthChange < 0 || episode.feedback.damageTaken > 0)
            {
                float weight = CalculateRuleWeight(episode);
                
                // Generate safety rules based on the episode
                if (episode.state.playerHealth < episode.state.playerMaxHealth * 0.3f)
                {
                    safetyRules.Add(new FOLRule("low_health_requires_immediate_action", weight, "Low health requires immediate action"));
                }
                
                if (episode.state.environmentState.ContainsKey("nearbyHazards") && 
                    (int)episode.state.environmentState["nearbyHazards"] > 0)
                {
                    safetyRules.Add(new FOLRule("hazards_require_immediate_avoidance", weight, "Hazards require immediate avoidance"));
                }
            }
            
            return safetyRules;
        }
    }
} 