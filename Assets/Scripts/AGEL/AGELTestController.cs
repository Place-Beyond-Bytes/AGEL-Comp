using System.Collections.Generic;
using UnityEngine;
using AGEL;

namespace AGEL
{
    public class AGELTestController : MonoBehaviour
    {
        [Header("AGEL Test Settings")]
        public AGELAgent agelAgent;
        public bool enableAutoTesting = false;
        public float testInterval = 5.0f;
        
        [Header("Test Scenarios")]
        public bool testMushroomConsumption = true;
        public bool testHealingItemUse = true;
        public bool testSafetyRules = true;
        
        [Header("Debug")]
        public bool showDebugInfo = true;
        public KeyCode printWorldModelKey = KeyCode.P;
        public KeyCode printEpisodicMemoryKey = KeyCode.M;
        public KeyCode runTestEpisodeKey = KeyCode.T;
        
        private float lastTestTime;
        
        private void Start()
        {
            if (agelAgent == null)
            {
                agelAgent = FindObjectOfType<AGELAgent>();
            }
            
            if (agelAgent == null)
            {
                Debug.LogError("AGEL Test Controller: No AGEL Agent found in scene!");
            }
        }
        
        private void Update()
        {
            // Manual test controls
            if (Input.GetKeyDown(printWorldModelKey))
            {
                PrintWorldModel();
            }
            
            if (Input.GetKeyDown(printEpisodicMemoryKey))
            {
                PrintEpisodicMemory();
            }
            
            if (Input.GetKeyDown(runTestEpisodeKey))
            {
                RunTestEpisode();
            }
            
            // Auto testing
            if (enableAutoTesting && agelAgent != null)
            {
                if (Time.time - lastTestTime > testInterval)
                {
                    RunAutoTest();
                    lastTestTime = Time.time;
                }
            }
        }
        
        private void RunAutoTest()
        {
            Debug.Log("=== AGEL Auto Test Started ===");
            
            if (testMushroomConsumption)
            {
                TestMushroomConsumption();
            }
            
            if (testHealingItemUse)
            {
                TestHealingItemUse();
            }
            
            if (testSafetyRules)
            {
                TestSafetyRules();
            }
            
            Debug.Log("=== AGEL Auto Test Completed ===");
        }
        
        private void TestMushroomConsumption()
        {
            Debug.Log("Testing Mushroom Consumption Scenario...");
            
            // Simulate consuming a mushroom
            if (InventoryManager.Instance != null)
            {
                // Add mushroom to inventory
                var mushroomItem = Resources.Load<ItemSO>("Assets/Scripts/Inventory & Shop/ItemSOs/Mushroom.asset");
                if (mushroomItem != null)
                {
                    InventoryManager.Instance.AddItem(mushroomItem, 1);
                    Debug.Log("Added mushroom to inventory for testing");
                    
                    // Simulate consuming the mushroom (this should trigger AGEL learning)
                    var slot = InventoryManager.Instance.itemSlots[0];
                    if (slot.itemSO != null && slot.quantity > 0)
                    {
                        InventoryManager.Instance.UseItem(slot);
                        Debug.Log("Consumed mushroom - AGEL should learn from this negative experience");
                    }
                }
            }
        }
        
        private void TestHealingItemUse()
        {
            Debug.Log("Testing Healing Item Use Scenario...");
            
            // Simulate using a healing item
            if (InventoryManager.Instance != null)
            {
                // Add steak to inventory
                var steakItem = Resources.Load<ItemSO>("Assets/Scripts/Inventory & Shop/ItemSOs/Steak.asset");
                if (steakItem != null)
                {
                    InventoryManager.Instance.AddItem(steakItem, 1);
                    Debug.Log("Added steak to inventory for testing");
                    
                    // Simulate using the steak (this should trigger AGEL learning)
                    var slot = InventoryManager.Instance.itemSlots[0];
                    if (slot.itemSO != null && slot.quantity > 0)
                    {
                        InventoryManager.Instance.UseItem(slot);
                        Debug.Log("Used steak - AGEL should learn from this positive experience");
                    }
                }
            }
        }
        
        private void TestSafetyRules()
        {
            Debug.Log("Testing Safety Rules Scenario...");
            
            // Simulate low health situation
            if (StatsManager.Instance != null)
            {
                int originalHealth = StatsManager.Instance.currentHealth;
                StatsManager.Instance.UpdateHealth(-10); // Reduce health
                Debug.Log($"Reduced health from {originalHealth} to {StatsManager.Instance.currentHealth}");
                
                // This should trigger AGEL to consider safety rules
                Debug.Log("Low health situation - AGEL should prioritize safety");
                
                // Restore health
                StatsManager.Instance.UpdateHealth(10);
            }
        }
        
        private void RunTestEpisode()
        {
            Debug.Log("=== Running Manual Test Episode ===");
            
            if (agelAgent != null)
            {
                // Print current state
                PrintWorldModel();
                PrintEpisodicMemory();
                
                // Run a test episode
                Debug.Log("AGEL Agent should process this episode and learn from it");
            }
            else
            {
                Debug.LogError("No AGEL Agent found for testing!");
            }
        }
        
        private void PrintWorldModel()
        {
            if (agelAgent != null)
            {
                agelAgent.PrintWorldModel();
            }
        }
        
        private void PrintEpisodicMemory()
        {
            if (agelAgent != null)
            {
                agelAgent.PrintEpisodicMemory();
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label("AGEL Test Controller", GUI.skin.box);
            
            GUILayout.Label($"AGEL Agent: {(agelAgent != null ? "Found" : "Not Found")}");
            GUILayout.Label($"Auto Testing: {(enableAutoTesting ? "Enabled" : "Disabled")}");
            
            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label($"P - Print World Model");
            GUILayout.Label($"M - Print Episodic Memory");
            GUILayout.Label($"T - Run Test Episode");
            
            GUILayout.Space(10);
            GUILayout.Label("Test Scenarios:");
            GUILayout.Label($"Mushroom Test: {(testMushroomConsumption ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Healing Test: {(testHealingItemUse ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Safety Test: {(testSafetyRules ? "Enabled" : "Disabled")}");
            
            if (agelAgent != null)
            {
                GUILayout.Space(10);
                var worldModel = agelAgent.GetWorldModel();
                var episodicMemory = agelAgent.GetEpisodicMemory();
                
                GUILayout.Label($"World Model Rules: {worldModel.GetRuleCount()}");
                GUILayout.Label($"Episodic Memory: {episodicMemory.GetSize()}/{episodicMemory.GetMaxSize()}");
                GUILayout.Label($"Current Goal: {agelAgent.GetCurrentGoal()}");
            }
            
            GUILayout.EndArea();
        }
    }
} 