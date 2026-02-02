using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AGEL
{
    // Orchestrates CPG + ILP + NTP per AGEL-Comp spec
    public class AGELCompCoordinator
    {
        public readonly CausalProgramGraph Cpg;
        public readonly ILPEngine Ilp;
        public readonly NeuralTheoremProver Ntp;

        public float HarmVetoThreshold = 0.5f; // if harm is provable above this, filter action

        public AGELCompCoordinator()
        {
            Cpg = new CausalProgramGraph();
            Ilp = new ILPEngine();
            Ntp = new NeuralTheoremProver(Cpg) { AcceptThreshold = 0.5f };
        }

        // Optionally bootstrap from existing WorldModel rules
        public void SyncFromWorldModel(WorldModel wm)
        {
            foreach (var r in wm.GetAllRules())
            {
                Cpg.AddRuleOrFact(r.rule, Mathf.Clamp01(r.weight), r.description);
            }
        }

        // Keep WorldModel updated for legacy components/UI
        public void SyncToWorldModel(WorldModel wm)
        {
            var rules = Cpg.ToWorldModelRules();
            foreach (var r in rules)
            {
                if (!wm.HasRule(r.rule)) wm.AddRule(r);
            }
        }

        // Verify a plan by vetoing actions that are likely harmful according to CPG
        public ActionPlan VerifyPlan(State state, ActionPlan plan)
        {
            if (plan == null || plan.actions == null || plan.actions.Count == 0) return plan;
            var filtered = new List<string>();
            foreach (var a in plan.actions)
            {
                if (IsActionHarmful(a, state, out float harmScore))
                {
                    // veto action
                    continue;
                }
                filtered.Add(a);
            }
            if (filtered.Count == 0) filtered.Add("wait");
            return new ActionPlan(filtered, plan.reasoning, plan.confidence);
        }

        // A simple mapping from actions to safety goals evaluated by NTP over CPG
        private bool IsActionHarmful(string action, State state, out float harmScore)
        {
            harmScore = 0f;
            // Examples: map to predicates consistent with Grounding
            // - avoid_mushrooms is safe
            // - consuming mushrooms harmful
            // - move_* usually safe
            // - retreat/avoid_hazards safe
            string lower = (action ?? "").ToLowerInvariant();

            // If the action itself is a safety action, do not veto
            if (lower.Contains("avoid") || lower.Contains("retreat") || lower.Contains("maintain_safety"))
                return false;

            // If action suggests consumption and inventory has mushrooms, check harm
            bool hasMushroom = state.inventoryItems != null && state.inventoryItems.Any(it => it.itemName.ToLower().Contains("mushroom"));
            if (lower.Contains("consume") || lower.Contains("use") || lower.Contains("eat"))
            {
                if (hasMushroom)
                {
                    var res = Ntp.Prove("causes_harm(consuming(mushroom))", 3);
                    harmScore = res.score;
                    return harmScore >= HarmVetoThreshold;
                }
            }

            // Movement actions default safe; could query hazards later
            if (lower.StartsWith("move_")) return false;

            // Aggressive actions, check generic harm
            if (lower.Contains("attack") || lower.Contains("kill"))
            {
                // Allow; we don't model self-harm here
                return false;
            }

            return false;
        }

        // Learning step: take grounded rules from AGELGrounding and induce with ILP into the CPG
        public List<string> LearnFromEpisode(Episode episode, AGELGrounding grounding, WorldModel wm)
        {
            var grounded = grounding != null && episode != null ? grounding.GenerateRules(episode) : new List<FOLRule>();
            var addedSymbols = Ilp.Induce(episode, grounded, Cpg);
            // Mirror into WorldModel for compatibility with existing UI/logic
            foreach (var sym in addedSymbols)
            {
                wm.AddRule(new FOLRule(sym, 1.0f));
            }
            return addedSymbols;
        }
    }
}
