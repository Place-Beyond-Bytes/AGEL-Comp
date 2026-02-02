using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    public class WorldModel
    {
        private List<FOLRule> rules;
        private Dictionary<string, float> ruleWeights;
        
        public WorldModel()
        {
            rules = new List<FOLRule>();
            ruleWeights = new Dictionary<string, float>();
        }
        
        public void AddRule(FOLRule rule)
        {
            // Check if rule already exists
            var existingRule = rules.Find(r => r.rule == rule.rule);
            if (existingRule != null)
            {
                // Update weight if new rule has higher weight
                if (rule.weight > existingRule.weight)
                {
                    existingRule.weight = rule.weight;
                    ruleWeights[rule.rule] = rule.weight;
                }
            }
            else
            {
                rules.Add(rule);
                ruleWeights[rule.rule] = rule.weight;
            }
        }
        
        public void AddRule(string rule, float weight)
        {
            AddRule(new FOLRule(rule, weight));
        }
        
        public void AddRule(string rule, float weight, string description)
        {
            AddRule(new FOLRule(rule, weight, description));
        }
        
        public List<FOLRule> GetAllRules()
        {
            return new List<FOLRule>(rules);
        }
        
        public List<FOLRule> GetRulesByWeight(float minWeight)
        {
            return rules.FindAll(r => r.weight >= minWeight);
        }
        
        public FOLRule GetRule(string ruleString)
        {
            return rules.Find(r => r.rule == ruleString);
        }
        
        public bool HasRule(string ruleString)
        {
            return rules.Exists(r => r.rule == ruleString);
        }
        
        public float GetRuleWeight(string ruleString)
        {
            return ruleWeights.ContainsKey(ruleString) ? ruleWeights[ruleString] : 0f;
        }
        
        public void RemoveRule(string ruleString)
        {
            var rule = rules.Find(r => r.rule == ruleString);
            if (rule != null)
            {
                rules.Remove(rule);
                ruleWeights.Remove(ruleString);
            }
        }
        
        public void ClearRules()
        {
            rules.Clear();
            ruleWeights.Clear();
        }
        
        public int GetRuleCount()
        {
            return rules.Count;
        }
        
        // Query the world model for specific patterns
        public List<FOLRule> QueryRules(string pattern)
        {
            List<FOLRule> matchingRules = new List<FOLRule>();
            
            foreach (var rule in rules)
            {
                if (rule.rule.Contains(pattern))
                {
                    matchingRules.Add(rule);
                }
            }
            
            return matchingRules;
        }
        
        // Get rules related to a specific object or action
        public List<FOLRule> GetRulesForObject(string objectName)
        {
            return QueryRules(objectName);
        }
        
        public List<FOLRule> GetRulesForAction(string actionName)
        {
            return QueryRules(actionName);
        }
        
        // Get safety rules (rules with high weights that might indicate danger)
        public List<FOLRule> GetSafetyRules(float safetyThreshold = 0.7f)
        {
            return rules.FindAll(r => r.weight >= safetyThreshold);
        }
        
        public override string ToString()
        {
            return $"WorldModel[{rules.Count} rules]";
        }
    }
} 