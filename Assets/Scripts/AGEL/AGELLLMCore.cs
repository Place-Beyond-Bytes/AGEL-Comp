using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    public class AGELLLMCore : MonoBehaviour
    {
        [Header("LLM Core Settings")]
        public float safetyThreshold = 0.7f;
        public float confidenceThreshold = 0.5f;
        
        [Header("Debug")]
        public bool enableDebugLogs = false;
        
        public ActionPlan Plan(string goal, State currentState, WorldModel worldModel)
        {
            ActionPlan plan = new ActionPlan();
            
            try
            {
                // Analyze current state and world model
                List<FOLRule> relevantRules = AnalyzeState(currentState, worldModel);
                List<FOLRule> safetyRules = worldModel.GetSafetyRules(safetyThreshold);
                
                // Generate reasoning based on current situation
                string reasoning = GenerateReasoning(currentState, relevantRules, safetyRules);
                
                // Generate action plan based on goal and constraints
                List<string> actions = GenerateActions(goal, currentState, relevantRules, safetyRules);
                
                // Calculate confidence based on rule consistency
                float confidence = CalculateConfidence(actions, relevantRules, safetyRules);
                
                plan.actions = actions;
                plan.reasoning = reasoning;
                plan.confidence = confidence;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"AGEL LLM Core: Generated plan with {actions.Count} actions, confidence: {confidence}");
                    Debug.Log($"Reasoning: {reasoning}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error in AGEL LLM Core Planning: " + e.Message);
                plan.actions.Add("wait"); // Safe fallback action
                plan.reasoning = "Error in planning, taking safe action";
                plan.confidence = 0.1f;
            }
            
            return plan;
        }
        
        private List<FOLRule> AnalyzeState(State state, WorldModel worldModel)
        {
            List<FOLRule> relevantRules = new List<FOLRule>();
            
            // Check for low health situation
            if (state.playerHealth < state.playerMaxHealth * 0.3f)
            {
                var healthRules = worldModel.QueryRules("healing");
                relevantRules.AddRange(healthRules);
            }
            
            // Check for nearby hazards
            if (state.environmentState.ContainsKey("nearbyHazards") && 
                (int)state.environmentState["nearbyHazards"] > 0)
            {
                var hazardRules = worldModel.QueryRules("harmful");
                relevantRules.AddRange(hazardRules);
            }
            
            // Check for nearby enemies
            if (state.environmentState.ContainsKey("nearbyEnemies") && 
                (int)state.environmentState["nearbyEnemies"] > 0)
            {
                var enemyRules = worldModel.QueryRules("enemy");
                relevantRules.AddRange(enemyRules);
            }
            
            // Check inventory for mushrooms (poisonous items)
            foreach (var item in state.inventoryItems)
            {
                if (item.itemName.ToLower().Contains("mushroom"))
                {
                    var poisonRules = worldModel.QueryRules("poison");
                    relevantRules.AddRange(poisonRules);
                    break;
                }
            }
            
            return relevantRules;
        }
        
        private string GenerateReasoning(State state, List<FOLRule> relevantRules, List<FOLRule> safetyRules)
        {
            string reasoning = "";
            
            // Health-based reasoning
            if (state.playerHealth < state.playerMaxHealth * 0.3f)
            {
                reasoning += "Player health is low. ";
            }
            
            // Hazard-based reasoning
            if (state.environmentState.ContainsKey("nearbyHazards") && 
                (int)state.environmentState["nearbyHazards"] > 0)
            {
                reasoning += "Hazards detected nearby. ";
            }
            
            // Enemy-based reasoning
            if (state.environmentState.ContainsKey("nearbyEnemies") && 
                (int)state.environmentState["nearbyEnemies"] > 0)
            {
                reasoning += "Enemies detected nearby. ";
            }
            
            // Safety rule reasoning
            if (safetyRules.Count > 0)
            {
                reasoning += $"Considering {safetyRules.Count} safety rules. ";
            }
            
            // Inventory reasoning
            bool hasMushrooms = false;
            bool hasHealingItems = false;
            
            foreach (var item in state.inventoryItems)
            {
                if (item.itemName.ToLower().Contains("mushroom"))
                    hasMushrooms = true;
                else if (item.currentHealth > 0)
                    hasHealingItems = true;
            }
            
            if (hasMushrooms)
                reasoning += "Inventory contains poisonous mushrooms. ";
            if (hasHealingItems)
                reasoning += "Inventory contains healing items. ";
            
            if (string.IsNullOrEmpty(reasoning))
                reasoning = "No immediate concerns detected.";
            
            return reasoning;
        }
        
        private List<string> GenerateActions(string goal, State state, List<FOLRule> relevantRules, List<FOLRule> safetyRules)
        {
            List<string> actions = new List<string>();
            
            // Priority 1: Safety actions (based on safety rules)
            if (safetyRules.Count > 0)
            {
                actions.Add("maintain_safety");
            }
            
            // Priority 2: Health management
            if (state.playerHealth < state.playerMaxHealth * 0.3f)
            {
                bool hasHealingItems = false;
                foreach (var item in state.inventoryItems)
                {
                    if (item.currentHealth > 0 && !item.itemName.ToLower().Contains("mushroom"))
                    {
                        hasHealingItems = true;
                        break;
                    }
                }
                
                if (hasHealingItems)
                {
                    actions.Add("use_healing_item");
                }
                else
                {
                    actions.Add("seek_healing");
                }
            }
            
            // Priority 3: Hazard avoidance
            if (state.environmentState.ContainsKey("nearbyHazards") && 
                (int)state.environmentState["nearbyHazards"] > 0)
            {
                actions.Add("avoid_hazards");
            }
            
            // Priority 4: Enemy management
            if (state.environmentState.ContainsKey("nearbyEnemies") && 
                (int)state.environmentState["nearbyEnemies"] > 0)
            {
                if (state.playerHealth < state.playerMaxHealth * 0.5f)
                {
                    actions.Add("retreat_from_enemies");
                }
                else
                {
                    actions.Add("assess_enemy_threat");
                }
            }
            
            // Priority 5: Exploration and goal pursuit
            if (actions.Count == 0 || actions.Contains("maintain_safety"))
            {
                actions.Add("explore_environment");
            }
            
            // Priority 6: Mushroom handling (avoid consumption)
            bool hasMushrooms = false;
            foreach (var item in state.inventoryItems)
            {
                if (item.itemName.ToLower().Contains("mushroom"))
                {
                    hasMushrooms = true;
                    break;
                }
            }
            
            if (hasMushrooms)
            {
                actions.Add("avoid_consuming_mushrooms");
            }
            
            return actions;
        }
        
        private float CalculateConfidence(List<string> actions, List<FOLRule> relevantRules, List<FOLRule> safetyRules)
        {
            float confidence = 0.5f; // Base confidence
            
            // Increase confidence based on rule consistency
            if (relevantRules.Count > 0)
            {
                float avgRuleWeight = 0f;
                foreach (var rule in relevantRules)
                {
                    avgRuleWeight += rule.weight;
                }
                avgRuleWeight /= relevantRules.Count;
                confidence += avgRuleWeight * 0.3f;
            }
            
            // Increase confidence if safety rules are being followed
            if (safetyRules.Count > 0)
            {
                confidence += 0.2f;
            }
            
            // Decrease confidence if conflicting actions
            if (actions.Contains("use_healing_item") && actions.Contains("avoid_consuming_mushrooms"))
            {
                confidence -= 0.1f;
            }
            
            // Clamp confidence to valid range
            confidence = Mathf.Clamp01(confidence);
            
            return confidence;
        }
        
        // Helper methods for specific planning scenarios
        public bool ShouldAvoidMushrooms(State state)
        {
            foreach (var item in state.inventoryItems)
            {
                if (item.itemName.ToLower().Contains("mushroom"))
                    return true;
            }
            return false;
        }
        
        public bool ShouldUseHealingItem(State state)
        {
            if (state.playerHealth >= state.playerMaxHealth * 0.8f)
                return false; // Don't waste healing items when health is high
                
            foreach (var item in state.inventoryItems)
            {
                if (item.currentHealth > 0 && !item.itemName.ToLower().Contains("mushroom"))
                    return true;
            }
            return false;
        }
        
        public bool ShouldRetreat(State state)
        {
            bool hasEnemies = state.environmentState.ContainsKey("nearbyEnemies") && 
                             (int)state.environmentState["nearbyEnemies"] > 0;
            bool lowHealth = state.playerHealth < state.playerMaxHealth * 0.4f;
            
            return hasEnemies && lowHealth;
        }
    }
} 