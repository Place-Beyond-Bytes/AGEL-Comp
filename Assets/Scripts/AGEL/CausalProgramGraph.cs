using System;
using System.Collections.Generic;
using System.Linq;

namespace AGEL
{
    // Simple causal program graph (CPG) with Horn-clause style rules
    public class CausalProgramGraph
    {
        public class Node
        {
            public string Symbol; // e.g., is_harmful(fire)
            public Node(string symbol) { Symbol = Normalize(symbol); }
        }

        public class Hyperedge
        {
            public List<string> Body; // b1, b2, ...
            public string Head;       // h
            public float Weight;      // confidence/strength
            public string Description;
        }

        // Storage
        private readonly HashSet<string> _facts = new HashSet<string>(); // ground literals h :- true
        private readonly List<Hyperedge> _rules = new List<Hyperedge>();

        public IEnumerable<string> Facts => _facts;
        public IEnumerable<Hyperedge> Rules => _rules;

        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            // remove trailing '.' if present
            if (s.EndsWith(".")) s = s.Substring(0, s.Length - 1);
            return s.Replace(" ", string.Empty);
        }

        // Accepts either a fact like "is_harmful(fire)" or a rule like "causes_harm(X) :- is_harmful(X)"
        public void AddRuleOrFact(string ruleOrFact, float weight = 1.0f, string description = "")
        {
            ruleOrFact = Normalize(ruleOrFact);
            if (string.IsNullOrEmpty(ruleOrFact)) return;

            if (ruleOrFact.Contains(":-"))
            {
                var parts = ruleOrFact.Split(new[] {":-"}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    string head = Normalize(parts[0]);
                    var body = parts[1].Split(',').Select(Normalize).Where(p => !string.IsNullOrEmpty(p)).ToList();
                    _rules.Add(new Hyperedge { Head = head, Body = body, Weight = weight, Description = description });
                    return;
                }
            }
            // Otherwise treat as fact
            _facts.Add(ruleOrFact);
        }

        public void AddFact(string fact, float weight = 1.0f, string description = "")
        {
            fact = Normalize(fact);
            if (!string.IsNullOrEmpty(fact)) _facts.Add(fact);
        }

        public void AddRule(string head, IEnumerable<string> body, float weight = 1.0f, string description = "")
        {
            head = Normalize(head);
            var bodyList = body.Select(Normalize).Where(b => !string.IsNullOrEmpty(b)).ToList();
            _rules.Add(new Hyperedge { Head = head, Body = bodyList, Weight = weight, Description = description });
        }

        // Simple query: can we (approximately) prove goal from facts + rules with bounded depth
        public (float score, List<string> proof) Prove(string goal, int maxDepth = 3)
        {
            goal = Normalize(goal);
            var visited = new HashSet<string>();
            var proof = new List<string>();
            float score = ProveRecursive(goal, maxDepth, visited, proof);
            return (score, proof);
        }

        private float ProveRecursive(string goal, int depth, HashSet<string> visited, List<string> proof)
        {
            if (depth < 0) return 0f;
            if (string.IsNullOrEmpty(goal)) return 0f;

            // Direct fact
            if (_facts.Contains(goal))
            {
                proof.Add($"fact({goal})");
                return 1.0f;
            }

            // Guard against loops
            if (visited.Contains(goal)) return 0f;
            visited.Add(goal);

            float best = 0f;
            Hyperedge bestRule = null;
            List<string> bestSubProof = null;

            foreach (var rule in _rules)
            {
                if (!SoftUnify(rule.Head, goal, out var theta)) continue;

                float minBodyScore = 1.0f;
                var localProof = new List<string>();
                foreach (var b in rule.Body)
                {
                    string grounded = Apply(theta, b);
                    var (score, subProof) = Prove(grounded, depth - 1);
                    minBodyScore = Math.Min(minBodyScore, score);
                    localProof.AddRange(subProof);
                    if (minBodyScore <= 0f) break;
                }
                float ruleScore = minBodyScore * MathfClamp01(rule.Weight);
                if (ruleScore > best)
                {
                    best = ruleScore;
                    bestRule = rule;
                    bestSubProof = localProof;
                }
            }

            if (bestRule != null)
            {
                proof.AddRange(bestSubProof);
                proof.Add($"rule({bestRule.Head}:-{string.Join(",", bestRule.Body)})");
            }

            return best;
        }

        // Very lightweight soft unification for symbols like p(X), p(fire)
        private bool SoftUnify(string a, string b, out Dictionary<string, string> theta)
        {
            theta = new Dictionary<string, string>();
            if (a == b) return true;

            // Parse predicate and arguments
            if (!TryParseLiteral(a, out var pa, out var argsA)) return false;
            if (!TryParseLiteral(b, out var pb, out var argsB)) return false;
            if (pa != pb || argsA.Count != argsB.Count) return false;

            for (int i = 0; i < argsA.Count; i++)
            {
                string xa = argsA[i];
                string xb = argsB[i];
                bool aVar = IsVar(xa);
                bool bVar = IsVar(xb);
                if (aVar && bVar) continue; // X with Y -> ok
                if (aVar) { theta[xa] = xb; continue; }
                if (bVar) { theta[xb] = xa; continue; }
                if (xa != xb) return false;
            }
            return true;
        }

        private static bool TryParseLiteral(string lit, out string pred, out List<string> args)
        {
            pred = lit;
            args = new List<string>();
            int i = lit.IndexOf('(');
            int j = lit.LastIndexOf(')');
            if (i < 0 || j < 0 || j <= i) return false;
            pred = lit.Substring(0, i);
            string inside = lit.Substring(i + 1, j - i - 1);
            args = inside.Split(',').Select(x => x.Trim()).ToList();
            return true;
        }

        private static string Apply(Dictionary<string, string> theta, string lit)
        {
            if (theta == null || theta.Count == 0) return lit;
            if (!TryParseLiteral(lit, out var p, out var args)) return lit;
            for (int i = 0; i < args.Count; i++)
            {
                if (IsVar(args[i]) && theta.TryGetValue(args[i], out var val)) args[i] = val;
            }
            return $"{p}({string.Join(",", args)})";
        }

        private static bool IsVar(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            char c = s[0];
            return char.IsUpper(c);
        }

        private static float MathfClamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        // Export rules into WorldModel-compatible FOLRule strings
        public List<FOLRule> ToWorldModelRules()
        {
            var list = new List<FOLRule>();
            foreach (var f in _facts)
                list.Add(new FOLRule(f, 1.0f));
            foreach (var r in _rules)
                list.Add(new FOLRule($"{r.Head}:-{string.Join(",", r.Body)}", r.Weight, r.Description));
            return list;
        }
    }
}
