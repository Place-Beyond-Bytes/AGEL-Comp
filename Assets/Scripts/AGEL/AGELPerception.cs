using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    public class AGELPerception : MonoBehaviour
    {
        [Header("Perception Settings")]
        public float perceptionRadius = 10f;
        public LayerMask objectLayerMask = -1;
        public LayerMask playerLayerMask = -1;
        
        [Header("Debug")]
        public bool showPerceptionRange = false;
        public bool enableDebugLogs = false;
        
        private Transform playerTransform;
        private StatsManager statsManager;
        private InventoryManager inventoryManager;
        
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
        }
        
        public State Observe()
        {
            State currentState = new State();
            
            try
            {
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
                
                // Get nearby objects
                currentState.nearbyObjects = GetNearbyObjects();
                
                // Get inventory items
                currentState.inventoryItems = GetInventoryItems();
                
                // Get environment state
                currentState.environmentState = GetEnvironmentState();
                
                if (enableDebugLogs)
                {
                    Debug.Log($"AGEL Perception: Observed state with {currentState.nearbyObjects.Count} nearby objects, {currentState.inventoryItems.Count} inventory items");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error in AGEL Perception: " + e.Message);
            }
            
            return currentState;
        }
        
        private List<GameObject> GetNearbyObjects()
        {
            List<GameObject> nearbyObjects = new List<GameObject>();
            
            if (playerTransform == null)
                return nearbyObjects;
            
            // Find all objects within perception radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, perceptionRadius, objectLayerMask);
            
            foreach (Collider2D collider in colliders)
            {
                GameObject obj = collider.gameObject;
                
                // Skip the player
                if (obj == playerTransform.gameObject)
                    continue;
                
                // Check if object is relevant for AGEL
                if (IsRelevantObject(obj))
                {
                    nearbyObjects.Add(obj);
                }
            }
            
            return nearbyObjects;
        }
        
        private bool IsRelevantObject(GameObject obj)
        {
            // Check if object has relevant components or tags
            if (obj.GetComponent<Loot>() != null) return true;
            if (obj.GetComponent<Enemy_Health>() != null) return true;
            if (obj.GetComponent<Enemy_Combat>() != null) return true;
            if (obj.CompareTag("Enemy")) return true;
            if (obj.CompareTag("Item")) return true;
            if (obj.CompareTag("Hazard")) return true;
            
            // Check for specific object types by name
            string objName = obj.name.ToLower();
            if (objName.Contains("fire") || objName.Contains("poison") || objName.Contains("mushroom"))
                return true;
            
            return false;
        }
        
        private List<ItemSO> GetInventoryItems()
        {
            List<ItemSO> items = new List<ItemSO>();
            
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
        
        private Dictionary<string, object> GetEnvironmentState()
        {
            Dictionary<string, object> envState = new Dictionary<string, object>();
            
            // Add time-based information
            envState["time"] = Time.time;
            envState["dayTime"] = Mathf.Sin(Time.time * 0.1f); // Simple day/night cycle
            
            // Add player state information
            if (statsManager != null)
            {
                envState["playerHealth"] = statsManager.currentHealth;
                envState["playerMaxHealth"] = statsManager.maxHealth;
                envState["playerSpeed"] = statsManager.speed;
                envState["playerDamage"] = statsManager.damage;
            }
            
            // Add gold from inventoryManager
            if (inventoryManager != null)
            {
                envState["gold"] = inventoryManager.gold;
            }
            
            // Add nearby hazards
            int hazardCount = 0;
            int enemyCount = 0;
            int itemCount = 0;
            
            if (playerTransform != null)
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, perceptionRadius);
                
                foreach (Collider2D collider in colliders)
                {
                    GameObject obj = collider.gameObject;
                    if (obj == playerTransform.gameObject) continue;
                    
                    if (obj.CompareTag("Hazard") || obj.name.ToLower().Contains("fire"))
                        hazardCount++;
                    else if (obj.CompareTag("Enemy") || obj.GetComponent<Enemy_Health>() != null)
                        enemyCount++;
                    else if (obj.CompareTag("Item") || obj.GetComponent<Loot>() != null)
                        itemCount++;
                }
            }
            
            envState["nearbyHazards"] = hazardCount;
            envState["nearbyEnemies"] = enemyCount;
            envState["nearbyItems"] = itemCount;
            
            return envState;
        }
        
        // Helper methods for specific perception tasks
        public bool IsNearHazard()
        {
            if (playerTransform == null) return false;
            
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, perceptionRadius);
            
            foreach (Collider2D collider in colliders)
            {
                GameObject obj = collider.gameObject;
                if (obj == playerTransform.gameObject) continue;
                
                if (obj.CompareTag("Hazard") || obj.name.ToLower().Contains("fire"))
                    return true;
            }
            
            return false;
        }
        
        public bool IsNearEnemy()
        {
            if (playerTransform == null) return false;
            
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, perceptionRadius);
            
            foreach (Collider2D collider in colliders)
            {
                GameObject obj = collider.gameObject;
                if (obj == playerTransform.gameObject) continue;
                
                if (obj.CompareTag("Enemy") || obj.GetComponent<Enemy_Health>() != null)
                    return true;
            }
            
            return false;
        }
        
        public bool IsLowHealth()
        {
            if (statsManager == null) return false;
            
            float healthPercentage = (float)statsManager.currentHealth / statsManager.maxHealth;
            return healthPercentage < 0.3f; // Below 30% health
        }
        
        public bool HasHealingItems()
        {
            if (inventoryManager == null) return false;
            
            foreach (var slot in inventoryManager.itemSlots)
            {
                if (slot.itemSO != null && slot.quantity > 0)
                {
                    // Check if item is a healing item (not a mushroom)
                    if (slot.itemSO.currentHealth > 0 && !slot.itemSO.itemName.ToLower().Contains("mushroom"))
                        return true;
                }
            }
            
            return false;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (showPerceptionRange && playerTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(playerTransform.position, perceptionRadius);
            }
        }
    }
} 