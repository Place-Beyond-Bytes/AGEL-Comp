using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AGEL
{
    // Enhanced ILP engine with Meta-Interpretive Learning capabilities
    public class ILPEngine
    {
        // Confidence thresholds
        public float HighConfidenceThreshold = 0.7f;
        public float MediumConfidenceThreshold = 0.4f;
        
        // MIL Configuration
        private readonly List<string> metaRules = new List<string> 
        {
            "P(X,Y) :- Q(X,Z), R(Z,Y)",  // Chain rule
            "P(X,Y) :- Q(X,Y), R(X)",     // Constrained rule
            "P(X,Y) :- Q(Y,X)"           // Inversion
        };
        
        private readonly Dictionary<string, float> predicateGeneralization = new Dictionary<string, float>();
        private readonly List<string> inventedPredicates = new List<string>();
        private const int MAX_HYPOTHESIS_DEPTH = 3;
        private const float MIN_RULE_CONFIDENCE = 0.3f;

        public List<string> Induce(Episode episode, List<FOLRule> grounded, CausalProgramGraph cpg)
        {
            var addedRules = new List<string>();
            float conf = episode?.feedback?.intensity ?? 0f;
            
            // Process grounded rules with MIL
            foreach (var rule in grounded)
            {
                string normalized = CausalProgramGraph.Normalize(rule.rule);
                if (string.IsNullOrEmpty(normalized)) continue;

                // Add the base rule with confidence
                float ruleConfidence = CalculateConfidence(rule, episode);
                
                if (ruleConfidence >= MIN_RULE_CONFIDENCE)
                {
                    // Try to generalize the rule using MIL
                    var generalized = GeneralizeRule(normalized, cpg, ruleConfidence);
                    foreach (var genRule in generalized)
                    {
                        if (cpg.AddRuleOrFact(genRule.rule, genRule.confidence, $"Generalized from {normalized}"))
                        {
                            addedRules.Add(genRule.rule);
                        }
                    }
                    
                    // Also add the original rule if it's not already covered
                    if (generalized.All(r => r.rule != normalized))
                    {
                        cpg.AddRuleOrFact(normalized, ruleConfidence, rule.description);
                        addedRules.Add(normalized);
                    }
                }
            }

            // Perform predicate invention if we have enough data
            if (conf > MediumConfidenceThreshold && episode != null)
            {
                var newPredicates = InventPredicates(episode, cpg);
                foreach (var pred in newPredicates)
                {
                    if (!inventedPredicates.Contains(pred))
                    {
                        inventedPredicates.Add(pred);
                        // Add type constraints for the new predicate
                        string typeConstraint = $"type({pred}(X)) :- {pred}(X).";
                        cpg.AddRuleOrFact(typeConstraint, 0.8f, "Type constraint for invented predicate");
                        addedRules.Add(typeConstraint);
                    }
                }
            }

            return addedRules.Distinct().ToList();
        }

        private List<(string rule, float confidence)> GeneralizeRule(string rule, CausalProgramGraph cpg, float baseConfidence)
        {
            var results = new List<(string, float)>();
            
            // Try to apply each meta-rule
            foreach (var metaRule in metaRules)
            {
                var generalized = ApplyMetaRule(rule, metaRule, cpg);
                foreach (var genRule in generalized)
                {
                    // Only keep rules that improve or maintain confidence
                    if (genRule.confidence >= baseConfidence * 0.9f) // Allow slight confidence drop for generalization
                    {
                        results.Add(genRule);
                    }
                }
            }
            
            return results.Distinct().ToList();
        }

        private List<(string rule, float confidence)> ApplyMetaRule(string rule, string metaRule, CausalProgramGraph cpg)
        {
            var results = new List<(string, float)>();
            
            // Simple implementation - in practice, this would use more sophisticated unification
            if (metaRule == "P(X,Y) :- Q(X,Z), R(Z,Y)" && rule.Contains(":-"))
            {
                // Try to split the rule body and chain it
                var parts = rule.Split(new[] { ":-" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var head = parts[0].Trim();
                    var body = parts[1].Trim();
                    
                    // Simple case: if body has two predicates, we can chain them
                    var predicates = body.Split(',').Select(p => p.Trim()).ToArray();
                    if (predicates.Length == 2)
                    {
                        string var1 = "A"; // In practice, use proper variable generation
                        string newRule = $"{head} :- {predicates[0].Split('(')[0]}({var1}), {predicates[1]}";
                        results.Add((newRule, 0.8f)); // Confidence adjustment for generalization
                    }
                }
            }
            
            // Add other meta-rule applications here...
            
            return results;
        }

        private List<string> InventPredicates(Episode episode, CausalProgramGraph cpg)
        {
            var newPredicates = new List<string>();
            
            // Simple predicate invention: look for common patterns in the episode
            var actionSequences = episode.actions?.Select(a => a.action).ToList() ?? new List<string>();
            var stateChanges = episode.stateChanges ?? new List<string>();
            
            // Example: If we see "pickup(X) followed by has(X)" often, invent a "carrying(X)" predicate
            if (actionSequences.Count >= 2)
            {
                for (int i = 0; i < actionSequences.Count - 1; i++)
                {
                    if (actionSequences[i].StartsWith("pickup") && 
                        actionSequences[i+1].StartsWith("has(") &&
                        !inventedPredicates.Contains("carrying"))
                    {
                        // Extract the variable from pickup(X)
                        var match = Regex.Match(actionSequences[i], @"pickup\(([^)]+)\)");
                        if (match.Success)
                        {
                            string var = match.Groups[1].Value;
                            string newPred = $"carrying({var})";
                            newPredicates.Add(newPred);
                            
                            // Add rules for the new predicate
                            string rule1 = $"{newPred} :- pickup({var}), has({var}).";
                            cpg.AddRuleOrFact(rule1, 0.9f, "Invented predicate rule");
                        }
                    }
                }
            }
            
            return newPredicates.Distinct().ToList();
        }

        private float CalculateConfidence(FOLRule rule, Episode episode)
        {
            // Base confidence from rule weight
            float confidence = Clamp01(rule.weight);
            
            // Adjust based on episode feedback
            if (episode?.feedback != null)
            {
                // Positive feedback increases confidence more than negative decreases it
                float feedbackFactor = episode.feedback.intensity > 0 ? 1.5f : 0.7f;
                confidence *= feedbackFactor * Math.Abs(episode.feedback.intensity);
            }
            
            // Penalize rules that are too specific or too general
            int varCount = rule.rule.Count(c => char.IsUpper(c) || c == '_');
            if (varCount > 3) confidence *= 0.8f; // Too many variables
            if (varCount == 0) confidence *= 0.7f; // Too few variables (too specific)
            
            return Clamp01(confidence);
        }

        private static float Clamp01(float v) => Math.Max(0, Math.Min(1, v));
    }
}
