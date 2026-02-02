using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AGEL
{
    public class AGELPerceptionEnhanced : MonoBehaviour, IDisposable
    {
        #region Nested Types
        
        [Serializable]
        public class ObjectMemory
        {
            public GameObject gameObject;
            public Vector3 lastKnownPosition;
            public float lastSeenTime;
            public float importance;
            public Dictionary<string, object> properties = new Dictionary<string, object>();
            public float[] embedding;
            
            public ObjectMemory(GameObject obj, float[] embedding = null)
            {
                gameObject = obj;
                lastKnownPosition = obj.transform.position;
                lastSeenTime = Time.time;
                importance = 1.0f;
                this.embedding = embedding ?? new float[128]; // Default embedding size
            }
            
            public void UpdateFrom(GameObject obj, float decay = 0.95f)
            {
                lastKnownPosition = obj.transform.position;
                lastSeenTime = Time.time;
                importance = 1.0f + (importance * decay);
            }
        }
        
        #endregion

        [Header("Perception Settings")]
        public float perceptionRadius = 10f;
        public LayerMask objectLayerMask = -1;
        public LayerMask playerLayerMask = -1;
        public float memoryDecayRate = 0.95f;
        public float maxMemoryAge = 300f; // 5 minutes
        public int maxTrackedObjects = 100;
        
        [Header("Grounding Settings")]
        public float minGroundingConfidence = 0.3f;
        public int maxGroundingAttempts = 3;
        public bool useNeuralGrounding = true;
        
        [Header("Debug")]
        public bool showPerceptionRange = false;
        public bool showObjectMemory = false;
        public bool enableDebugLogs = false;
        
        // Memory and state
        private Dictionary<int, ObjectMemory> objectMemory = new Dictionary<int, ObjectMemory>();
        private Dictionary<string, float[]> conceptEmbeddings = new Dictionary<string, float[]>();
        private Queue<int> memoryAccessQueue = new Queue<int>();
        
        // Cached components
        private Transform playerTransform;
        private StatsManager statsManager;
        private InventoryManager inventoryManager;
        private AGELGrounding groundingModule;
        private NeuralEmbeddingGenerator neuralEmbedder;

        private void Start()
        {
            // Find player and components
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                playerTransform = transform; // Fallback to self
            }
            
            statsManager = StatsManager.Instance;
            inventoryManager = InventoryManager.Instance;
            
            // Initialize neural components
            neuralEmbedder = new NeuralEmbeddingGenerator(embeddingSize: 128);
            groundingModule = new AGELGrounding();
            
            // Initialize common concept embeddings
            InitializeConceptEmbeddings();
            
            if (enableDebugLogs)
            {
                Debug.Log("AGEL Perception: Initialized with neural grounding and memory system");
            }
        }
        
        private void InitializeConceptEmbeddings()
        {
            // Initialize with common game concepts
            string[] concepts = {
                "enemy", "item", "hazard", "npc", "container",
                "health", "damage", "speed", "weapon", "armor",
                "potion", "key", "door", "chest", "trap"
            };
            
            foreach (var concept in concepts)
            {
                conceptEmbeddings[concept] = neuralEmbedder.GenerateEmbedding(concept);
            }
        }

        public State Observe()
        {
            var currentState = new State();
            
            try
            {
                // Update object memory with current observations
                UpdateObjectMemory();
                
                // Get player information
                if (playerTransform != null)
                {
                    currentState.playerPosition = playerTransform.position;
                }
                
                if (statsManager != null)
                {
                    currentState.playerHealth = statsManager.currentHealth;
                    currentState.playerMaxHealth = statsManager.maxHealth;
                }
                
                // Get nearby objects with enhanced perception
                currentState.nearbyObjects = GetPerceivedObjects();
                
                // Get inventory items with semantic information
                currentState.inventoryItems = GetInventoryItems();
                
                // Get enhanced environment state
                currentState.environmentState = GetEnhancedEnvironmentState();
                
                // Generate grounded facts and rules
                currentState.groundedFacts = GenerateGroundedFacts(currentState);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"AGEL Perception: Observed state with {currentState.nearbyObjects.Count} objects, " +
                             $"{currentState.inventoryItems.Count} items, {currentState.groundedFacts.Count} facts");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in AGEL Perception: {e.Message}\n{e.StackTrace}");
            }
            
            return currentState;
        }
        
        private void UpdateObjectMemory()
        {
            // Remove old memories
            var toRemove = objectMemory
                .Where(m => Time.time - m.Value.lastSeenTime > maxMemoryAge)
                .Select(p => p.Key)
                .ToList();
                
            foreach (var key in toRemove)
            {
                objectMemory.Remove(key);
            }
            
            // Update importance of all objects
            foreach (var mem in objectMemory.Values)
            {
                mem.importance *= memoryDecayRate;
            }
            
            // Add/update objects in perception range
            var colliders = Physics2D.OverlapCircleAll(
                playerTransform.position, 
                perceptionRadius, 
                objectLayerMask);
                
            foreach (var collider in colliders)
            {
                var obj = collider.gameObject;
                if (obj == playerTransform.gameObject) continue;
                
                int id = obj.GetInstanceID();
                if (objectMemory.TryGetValue(id, out var memory))
                {
                    memory.UpdateFrom(obj, memoryDecayRate);
                }
                else if (objectMemory.Count < maxTrackedObjects)
                {
                    var embedding = neuralEmbedder.GenerateEmbedding(GetObjectDescription(obj));
                    objectMemory[id] = new ObjectMemory(obj, embedding);
                    memoryAccessQueue.Enqueue(id);
                }
            }
        }
        
        private string GetObjectDescription(GameObject obj)
        {
            // Generate a rich description of the object for embedding
            var components = obj.GetComponents<Component>();
            var componentNames = string.Join(", ", components.Select(c => c.GetType().Name));
            
            return $"{obj.name} ({obj.tag}) with components: {componentNames}";
        }
        
        private List<GameObject> GetPerceivedObjects()
        {
            var perceivedObjects = new List<GameObject>();
            
            if (playerTransform == null)
                return perceivedObjects;
                
            // Get objects in perception range with importance above threshold
            var relevantObjects = objectMemory.Values
                .Where(mem => 
                    Vector3.Distance(playerTransform.position, mem.lastKnownPosition) <= perceptionRadius * 1.5f &&
                    mem.importance > 0.1f)
                .OrderByDescending(mem => mem.importance)
                .Take(20) // Limit to most important objects
                .ToList();
                
            foreach (var memory in relevantObjects)
            {
                // If object was destroyed, skip or handle accordingly
                if (memory.gameObject == null) continue;
                
                // Update position and add to perceived objects
                memory.UpdateFrom(memory.gameObject);
                perceivedObjects.Add(memory.gameObject);
                
                // Visualize perception (debug)
                if (showObjectMemory)
                {
                    Debug.DrawLine(
                        playerTransform.position, 
                        memory.gameObject.transform.position, 
                        Color.Lerp(Color.red, Color.green, memory.importance),
                        0.1f);
                }
            }
            
            return perceivedObjects;
        }
        
        private List<ItemSO> GetInventoryItems()
        {
            var items = new List<ItemSO>();
            
            if (inventoryManager != null && inventoryManager.itemSlots != null)
            {
                foreach (var slot in inventoryManager.itemSlots)
                {
                    if (slot.itemSO != null && slot.quantity > 0)
                    {
                        items.Add(slot.itemSO);
                    }
                }
            }
            
            return items;
        }
        
        private Dictionary<string, object> GetEnhancedEnvironmentState()
        {
            var envState = new Dictionary<string, object>();
            
            try
            {
                // Time and location
                envState["time"] = Time.time;
                envState["dayTime"] = Mathf.Sin(Time.time * 0.1f);
                envState["playerPosition"] = playerTransform?.position ?? Vector3.zero;
                
                // Player state with more detail
                if (statsManager != null)
                {
                    var playerState = new Dictionary<string, object>
                    {
                        ["health"] = statsManager.currentHealth,
                        ["maxHealth"] = statsManager.maxHealth,
                        ["speed"] = statsManager.speed,
                        ["damage"] = statsManager.damage,
                        ["isAlive"] = statsManager.currentHealth > 0,
                        ["lastDamageTime"] = statsManager.lastDamageTime,
                        ["lastHealTime"] = statsManager.lastHealTime
                    };
                    envState["player"] = playerState;
                }
                
                // Inventory summary
                if (inventoryManager != null)
                {
                    var inventory = new Dictionary<string, object>
                    {
                        ["gold"] = inventoryManager.gold,
                        ["itemCount"] = inventoryManager.itemSlots?.Count(s => s.itemSO != null) ?? 0,
                        ["weaponEquipped"] = inventoryManager.weaponSlot?.itemSO != null,
                        ["armorEquipped"] = inventoryManager.armorSlot?.itemSO != null,
                        ["hasConsumables"] = inventoryManager.itemSlots?.Any(s => s.itemSO is ConsumableSO) ?? false
                    };
                    envState["inventory"] = inventory;
                }
                
                // Object counts with types and distances
                var objectCounts = new Dictionary<string, object>();
                var nearby = GetPerceivedObjects();
                
                foreach (var obj in nearby)
                {
                    string type = GetObjectType(obj);
                    if (string.IsNullOrEmpty(type)) continue;
                    
                    if (!objectCounts.ContainsKey(type))
                        objectCounts[type] = 0;
                        
                    objectCounts[type] = (int)objectCounts[type] + 1;
                }
                
                // Add spatial relationships
                var spatialInfo = new Dictionary<string, object>();
                foreach (var obj in nearby.Take(5)) // Limit to closest 5 objects
                {
                    if (obj == null) continue;
                    
                    float distance = Vector3.Distance(playerTransform.position, obj.transform.position);
                    string direction = GetDirection(playerTransform.position, obj.transform.position);
                    
                    spatialInfo[obj.name] = new {
                        type = GetObjectType(obj),
                        distance,
                        direction,
                        position = obj.transform.position
                    };
                }
                
                envState["objectCounts"] = objectCounts;
                envState["spatialRelationships"] = spatialInfo;
                
                // Add danger assessment
                envState["dangerLevel"] = CalculateDangerLevel();
                
                // Add resource availability
                envState["resourceAvailability"] = AssessResources();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in GetEnhancedEnvironmentState: {e.Message}");
            }
            
            return envState;
        }
        
        private string GetObjectType(GameObject obj)
        {
            if (obj.CompareTag("Enemy")) return "enemy";
            if (obj.CompareTag("Item")) return "item";
            if (obj.CompareTag("Hazard")) return "hazard";
            if (obj.CompareTag("NPC")) return "npc";
            if (obj.CompareTag("Container")) return "container";
            
            // Check components for more specific typing
            if (obj.GetComponent<Enemy_Health>()) return "enemy";
            if (obj.GetComponent<Loot>()) return "item";
            if (obj.GetComponent<Hazard>()) return "hazard";
            
            return "unknown";
        }
        
        private string GetDirection(Vector3 from, Vector3 to)
        {
            Vector3 dir = (to - from).normalized;
            float angle = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);
            
            if (angle < -157.5f) return "south";
            if (angle < -112.5f) return "southwest";
            if (angle < -67.5f) return "west";
            if (angle < -22.5f) return "northwest";
            if (angle < 22.5f) return "north";
            if (angle < 67.5f) return "northeast";
            if (angle < 112.5f) return "east";
            if (angle < 157.5f) return "southeast";
            return "south";
        }
        
        private float CalculateDangerLevel()
        {
            float danger = 0f;
            
            // Base danger on nearby enemies and hazards
            var nearby = GetPerceivedObjects();
            foreach (var obj in nearby)
            {
                if (obj.CompareTag("Enemy") || obj.GetComponent<Enemy_Health>())
                    danger += 0.5f;
                else if (obj.CompareTag("Hazard"))
                    danger += 0.3f;
            }
            
            // Adjust for player health
            if (statsManager != null)
            {
                float healthRatio = (float)statsManager.currentHealth / statsManager.maxHealth;
                danger *= (1.2f - healthRatio); // More dangerous at low health
            }
            
            return Mathf.Clamp01(danger);
        }
        
        private Dictionary<string, object> AssessResources()
        {
            var resources = new Dictionary<string, object>();
            
            // Check inventory for resources
            if (inventoryManager != null)
            {
                resources["hasHealthItems"] = inventoryManager.itemSlots
                    .Any(s => s.itemSO is ConsumableSO c && c.consumableType == ConsumableType.Health);
                    
                resources["hasManaItems"] = inventoryManager.itemSlots
                    .Any(s => s.itemSO is ConsumableSO c && c.consumableType == ConsumableType.Mana);
            }
            
            // Check environment for resources
            var nearby = GetPerceivedObjects();
            resources["nearbyHealth"] = nearby.Any(o => o.name.ToLower().Contains("potion") || o.name.ToLower().Contains("health"));
            resources["nearbyWeapons"] = nearby.Any(o => o.GetComponent<Weapon>() != null);
            resources["nearbyContainers"] = nearby.Count(o => o.CompareTag("Container"));
            
            return resources;
        }
        
        #region Grounding and Causal Attribution
        
        private List<FOLRule> GenerateGroundedFacts(State state)
        {
            var facts = new List<FOLRule>();
            
            try
            {
                // Ground object properties
                foreach (var obj in state.nearbyObjects)
                {
                    if (obj == null) continue;
                    
                    string objName = $"obj_{obj.GetInstanceID()}";
                    string type = GetObjectType(obj);
                    
                    // Add type information
                    facts.Add(new FOLRule($"is_a({objName}, {type})", 0.9f));
                    
                    // Add properties based on components
                    if (obj.TryGetComponent<Enemy_Health>(out var enemyHealth))
                    {
                        facts.Add(new FOLRule($"has_health({objName}, {enemyHealth.currentHealth})", 0.9f));
                        facts.Add(new FOLRule($"is_hostile({objName})", 0.95f));
                    }
                    
                    if (obj.TryGetComponent<Loot>(out var loot) && loot.itemSO != null)
                    {
                        facts.Add(new FOLRule($"is_collectible({objName})", 1.0f));
                        facts.Add(new FOLRule($"item_type({objName}, {loot.itemSO.itemName.ToLower()})", 0.9f));
                    }
                    
                    // Add spatial relationships
                    if (playerTransform != null)
                    {
                        float distance = Vector3.Distance(playerTransform.position, obj.transform.position);
                        string relPos = distance < perceptionRadius * 0.3f ? "near" : 
                                      distance < perceptionRadius * 0.7f ? "medium" : "far";
                        
                        facts.Add(new FOLRule($"distance_from_player({objName}, {relPos})", 0.8f));
                    }
                }
                
                // Add player state facts
                if (statsManager != null)
                {
                    float healthRatio = (float)statsManager.currentHealth / statsManager.maxHealth;
                    string healthStatus = healthRatio > 0.7 ? "high" :
                                         healthRatio > 0.3 ? "medium" : "low";
                    
                    facts.Add(new FOLRule($"player_health({healthStatus})", 1.0f));
                }
                
                // Add inventory facts
                if (inventoryManager != null)
                {
                    var items = inventoryManager.itemSlots
                        .Where(s => s.itemSO != null)
                        .Select(s => s.itemSO.itemName.ToLower())
                        .Distinct();
                    
                    foreach (var item in items)
                    {
                        facts.Add(new FOLRule($"has_item({item})", 0.9f));
                    }
                    
                    if (inventoryManager.weaponSlot?.itemSO != null)
                    {
                        facts.Add(new FOLRule(
                            $"equipped_weapon({inventoryManager.weaponSlot.itemSO.itemName.ToLower()})", 
                            1.0f));
                    }
                }
                
                // Use neural grounding for more complex facts
                if (useNeuralGrounding)
                {
                    var neuralFacts = groundingModule.GenerateNeuralFacts(
                        state, 
                        objectMemory.Values.ToList(),
                        neuralEmbedder);
                    
                    facts.AddRange(neuralFacts);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in GenerateGroundedFacts: {e.Message}");
            }
            
            return facts;
        }
        
        public (List<FOLRule> facts, List<FOLRule> rules) GroundPerception(State state)
        {
            var facts = GenerateGroundedFacts(state);
            var rules = new List<FOLRule>();
            
            // Add causal rules based on game mechanics
            rules.Add(new FOLRule("causes(attack(X), damage(X, D)) :- is_enemy(X), has_weapon(equipped)", 0.8f));
            rules.Add(new FOLRule("causes(use_health_potion, heal_player(25)) :- has_item(health_potion)", 0.9f));
            rules.Add(new FOLRule("prevents(low_health, use_health_potion) :- player_health(high)", 0.7f));
            
            return (facts, rules);
        }
        
        #endregion
        
        #region Helper Methods
        
        public bool IsNearHazard()
        {
            if (playerTransform == null) return false;
            
            // Check both current perception and memory
            bool hasHazard = objectMemory.Values
                .Where(m => m.importance > 0.5f)
                .Any(m => 
                    m.gameObject != null && 
                    (m.gameObject.CompareTag("Hazard") || 
                     m.gameObject.name.ToLower().Contains("fire")) &&
                    Vector3.Distance(playerTransform.position, m.lastKnownPosition) < perceptionRadius);
                    
            return hasHazard;
        }
        
        public void Dispose()
        {
            // Clean up resources
            objectMemory.Clear();
            conceptEmbeddings.Clear();
            memoryAccessQueue.Clear();
            
            if (neuralEmbedder is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        #endregion
    }
    
    // Neural Embedding Generator for semantic embeddings
    public class NeuralEmbeddingGenerator : IDisposable
    {
        private readonly int _embeddingSize;
        private readonly Dictionary<string, float[]> _embeddingCache;
        
        public NeuralEmbeddingGenerator(int embeddingSize = 128)
        {
            _embeddingSize = embeddingSize;
            _embeddingCache = new Dictionary<string, float[]>();
        }
        
        public float[] GenerateEmbedding(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new float[_embeddingSize];
                
            // Check cache first
            if (_embeddingCache.TryGetValue(text, out var cached))
                return cached;
                
            // Generate a simple embedding (in practice, use a neural network)
            var embedding = new float[_embeddingSize];
            for (int i = 0; i < _embeddingSize; i++)
            {
                // Simple deterministic hash-based embedding
                int hash = (text.GetHashCode() + i * 31) % 1000;
                embedding[i] = (hash % 2000 - 1000) / 1000f; // Normalize to [-1, 1]
            }
            
            // Cache the result
            _embeddingCache[text] = embedding;
            return embedding;
        }
        
        public float Similarity(string a, string b)
        {
            var embA = GenerateEmbedding(a);
            var embB = GenerateEmbedding(b);
            return CosineSimilarity(embA, embB);
        }
        
        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length || a.Length == 0)
                return 0;
                
            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            
            return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB) + 1e-6f);
        }
        
        public void Dispose()
        {
            _embeddingCache.Clear();
        }
    }
    
    // Grounding module for neural-symbolic integration
    public class AGELGrounding
    {
        public List<FOLRule> GenerateNeuralFacts(State state, List<AGELPerceptionEnhanced.ObjectMemory> memories, NeuralEmbeddingGenerator embedder)
        {
            var facts = new List<FOLRule>();
            
            try
            {
                // Generate facts about object relationships
                foreach (var mem1 in memories.Take(10)) // Limit for performance
                {
                    if (mem1.gameObject == null) continue;
                    
                    string obj1 = $"obj_{mem1.gameObject.GetInstanceID()}";
                    var emb1 = mem1.embedding;
                    
                    // Find similar objects
                    foreach (var mem2 in memories.Take(5))
                    {
                        if (mem2 == mem1 || mem2.gameObject == null) continue;
                        
                        string obj2 = $"obj_{mem2.gameObject.GetInstanceID()}";
                        float similarity = embedder.CosineSimilarity(emb1, mem2.embedding);
                        
                        if (similarity > 0.7f)
                        {
                            facts.Add(new FOLRule($"similar_to({obj1}, {obj2})", similarity));
                        }
                    }
                }
                
                // Add temporal facts based on state changes
                if (state.environmentState.TryGetValue("player_health", out var health) && 
                    state.environmentState.TryGetValue("player_health_previous", out var prevHealth))
                {
                    float delta = (float)health - (float)prevHealth;
                    if (delta < -10) facts.Add(new FOLRule("player_took_heavy_damage", 0.9f));
                    else if (delta > 10) facts.Add(new FOLRule("player_healed_significantly", 0.9f));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in GenerateNeuralFacts: {e.Message}");
            }
            
            return facts;
        }
    }
}
