using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    public class AGELActionModule : MonoBehaviour
    {
        [Header("Action Module Settings")]
        public float actionExecutionDelay = 0.1f;
        public bool enableAutoExecution = true;
        
        [Header("Debug")]
        public bool enableDebugLogs = false;
        
        private Transform playerTransform;
        private StatsManager statsManager;
        private InventoryManager inventoryManager;
        private AGELPerception perception;
        
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
            perception = GetComponent<AGELPerception>();
        }
        
        public List<Command> Translate(ActionPlan actionPlan)
        {
            List<Command> commands = new List<Command>();
            
            try
            {
                foreach (string action in actionPlan.actions)
                {
                    Command command = TranslateAction(action);
                    if (command != null)
                    {
                        commands.Add(command);
                    }
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log($"AGEL Action Module: Translated {actionPlan.actions.Count} actions into {commands.Count} commands");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error in AGEL Action Module Translation: " + e.Message);
            }
            
            return commands;
        }
        
        private Command TranslateAction(string action)
        {
            switch (action.ToLower())
            {
                case "move_left":
                    return new Command("move_left");
                case "move_right":
                    return new Command("move_right");
                case "move_up":
                    return new Command("move_up");
                case "move_down":
                    return new Command("move_down");
                case "use_healing_item":
                    return new Command("use_healing_item");
                case "avoid_consuming_mushrooms":
                    return new Command("avoid_mushrooms");
                case "maintain_safety":
                    return new Command("maintain_safety");
                case "avoid_hazards":
                    return new Command("move_away_from_hazards");
                case "retreat_from_enemies":
                    return new Command("retreat");
                case "assess_enemy_threat":
                    return new Command("assess_threat");
                case "explore_environment":
                    return new Command("explore");
                case "seek_healing":
                    return new Command("find_healing");
                case "wait":
                    return new Command("wait");
                case "grab":
                    return new Command("grab");
                case "kill_goblin":
                    return new Command("kill_goblin");
                default:
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"AGEL Action Module: Unknown action '{action}', translating to 'wait'");
                    }
                    return new Command("wait");
            }
        }
        
        public Feedback Execute(List<Command> commands)
        {
            Feedback feedback = new Feedback();
            
            try
            {
                foreach (Command command in commands)
                {
                    Feedback commandFeedback = ExecuteCommand(command);
                    
                    // Accumulate feedback
                    feedback.healthChange += commandFeedback.healthChange;
                    feedback.damageTaken += commandFeedback.damageTaken;
                    feedback.success &= commandFeedback.success;
                    feedback.message += commandFeedback.message + "; ";
                    feedback.intensity = Mathf.Max(feedback.intensity, commandFeedback.intensity);
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log($"AGEL Action Module: Executed {commands.Count} commands, feedback: {feedback}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error in AGEL Action Module Execution: " + e.Message);
                feedback.success = false;
                feedback.message = "Error during execution";
                feedback.intensity = 0.5f;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteCommand(Command command)
        {
            Feedback feedback = new Feedback();
            
            switch (command.action.ToLower())
            {
                case "move_left":
                    if (playerTransform != null)
                    {
                        playerTransform.position += Vector3.left;
                        feedback.success = true;
                        feedback.message = "Moved left";
                        feedback.intensity = 0.2f;
                        AwardExperience(30);
                    }
                    else
                    {
                        feedback.success = false;
                        feedback.message = "Player transform not found";
                    }
                    break;
                case "move_right":
                    if (playerTransform != null)
                    {
                        playerTransform.position += Vector3.right;
                        feedback.success = true;
                        feedback.message = "Moved right";
                        feedback.intensity = 0.2f;
                        AwardExperience(30);
                    }
                    else
                    {
                        feedback.success = false;
                        feedback.message = "Player transform not found";
                    }
                    break;
                case "move_up":
                    if (playerTransform != null)
                    {
                        playerTransform.position += Vector3.up;
                        feedback.success = true;
                        feedback.message = "Moved up";
                        feedback.intensity = 0.2f;
                        AwardExperience(30);
                    }
                    else
                    {
                        feedback.success = false;
                        feedback.message = "Player transform not found";
                    }
                    break;
                case "move_down":
                    if (playerTransform != null)
                    {
                        playerTransform.position += Vector3.down;
                        feedback.success = true;
                        feedback.message = "Moved down";
                        feedback.intensity = 0.2f;
                        AwardExperience(30);
                    }
                    else
                    {
                        feedback.success = false;
                        feedback.message = "Player transform not found";
                    }
                    break;
                case "use_healing_item":
                    feedback = ExecuteUseHealingItem();
                    break;
                    
                case "avoid_mushrooms":
                    feedback = ExecuteAvoidMushrooms();
                    break;
                    
                case "maintain_safety":
                    feedback = ExecuteMaintainSafety();
                    break;
                    
                case "move_away_from_hazards":
                    feedback = ExecuteMoveAwayFromHazards();
                    break;
                    
                case "retreat":
                    feedback = ExecuteRetreat();
                    break;
                    
                case "assess_threat":
                    feedback = ExecuteAssessThreat();
                    break;
                    
                case "explore":
                    feedback = ExecuteExplore();
                    break;
                    
                case "find_healing":
                    feedback = ExecuteFindHealing();
                    break;
                    
                case "wait":
                    feedback = ExecuteWait();
                    break;
                    
                case "grab":
                    feedback = ExecuteGrab();
                    break;
                    
                case "kill_goblin":
                    feedback = ExecuteKillGoblin();
                    break;
                    
                default:
                    feedback.message = $"Unknown command: {command.action}";
                    feedback.success = false;
                    break;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteUseHealingItem()
        {
            Feedback feedback = new Feedback();
            
            if (inventoryManager != null)
            {
                // Find a healing item (not mushroom)
                for (int i = 0; i < inventoryManager.itemSlots.Length; i++)
                {
                    var slot = inventoryManager.itemSlots[i];
                    if (slot.itemSO != null && slot.quantity > 0)
                    {
                        if (slot.itemSO.currentHealth > 0 && !slot.itemSO.itemName.ToLower().Contains("mushroom"))
                        {
                            // Use the healing item
                            inventoryManager.UseItem(slot);
                            feedback.healthChange = slot.itemSO.currentHealth;
                            feedback.success = true;
                            feedback.message = $"Used healing item: {slot.itemSO.itemName}";
                            feedback.intensity = 0.3f;
                            return feedback;
                        }
                    }
                }
                
                feedback.message = "No healing items available";
                feedback.success = false;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteAvoidMushrooms()
        {
            Feedback feedback = new Feedback();
            
            if (inventoryManager != null)
            {
                // Check if any mushrooms were consumed recently
                bool mushroomsAvoided = true;
                
                for (int i = 0; i < inventoryManager.itemSlots.Length; i++)
                {
                    var slot = inventoryManager.itemSlots[i];
                    if (slot.itemSO != null && slot.quantity > 0)
                    {
                        if (slot.itemSO.itemName.ToLower().Contains("mushroom"))
                        {
                            mushroomsAvoided = false;
                            break;
                        }
                    }
                }
                
                feedback.success = mushroomsAvoided;
                feedback.message = mushroomsAvoided ? "Successfully avoided consuming mushrooms" : "Mushrooms still present in inventory";
                feedback.intensity = 0.2f;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteMaintainSafety()
        {
            Feedback feedback = new Feedback();
            
            // Check current safety status
            bool isSafe = true;
            string safetyMessage = "Safety maintained";
            
            if (perception != null)
            {
                if (perception.IsNearHazard())
                {
                    isSafe = false;
                    safetyMessage = "Near hazards detected";
                }
                
                if (perception.IsNearEnemy())
                {
                    isSafe = false;
                    safetyMessage = "Enemies detected nearby";
                }
                
                if (perception.IsLowHealth())
                {
                    isSafe = false;
                    safetyMessage = "Health is low";
                }
            }
            
            feedback.success = isSafe;
            feedback.message = safetyMessage;
            feedback.intensity = isSafe ? 0.1f : 0.6f;
            
            return feedback;
        }
        
        private Feedback ExecuteMoveAwayFromHazards()
        {
            Feedback feedback = new Feedback();
            
            if (perception != null && perception.IsNearHazard())
            {
                // Simulate moving away from hazards
                feedback.success = true;
                feedback.message = "Moved away from hazards";
                feedback.intensity = 0.4f;
            }
            else
            {
                feedback.success = true;
                feedback.message = "No hazards detected";
                feedback.intensity = 0.1f;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteRetreat()
        {
            Feedback feedback = new Feedback();
            
            if (perception != null && perception.IsNearEnemy())
            {
                // Simulate retreating from enemies
                feedback.success = true;
                feedback.message = "Retreated from enemies";
                feedback.intensity = 0.5f;
            }
            else
            {
                feedback.success = true;
                feedback.message = "No enemies to retreat from";
                feedback.intensity = 0.1f;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteAssessThreat()
        {
            Feedback feedback = new Feedback();
            
            if (perception != null)
            {
                bool hasEnemies = perception.IsNearEnemy();
                bool lowHealth = perception.IsLowHealth();
                
                if (hasEnemies && lowHealth)
                {
                    feedback.message = "High threat level: enemies nearby and low health";
                    feedback.intensity = 0.8f;
                }
                else if (hasEnemies)
                {
                    feedback.message = "Moderate threat: enemies detected";
                    feedback.intensity = 0.5f;
                }
                else
                {
                    feedback.message = "Low threat level";
                    feedback.intensity = 0.2f;
                }
                
                feedback.success = true;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteExplore()
        {
            Feedback feedback = new Feedback();
            
            feedback.success = true;
            feedback.message = "Exploring environment";
            feedback.intensity = 0.2f;
            
            return feedback;
        }
        
        private Feedback ExecuteFindHealing()
        {
            Feedback feedback = new Feedback();
            
            if (perception != null && perception.HasHealingItems())
            {
                feedback.success = true;
                feedback.message = "Healing items found in inventory";
                feedback.intensity = 0.3f;
            }
            else
            {
                feedback.success = false;
                feedback.message = "No healing items available";
                feedback.intensity = 0.4f;
            }
            
            return feedback;
        }
        
        private Feedback ExecuteWait()
        {
            Feedback feedback = new Feedback();
            
            feedback.success = true;
            feedback.message = "Waiting";
            feedback.intensity = 0.1f;
            
            return feedback;
        }
        
        private Feedback ExecuteGrab()
        {
            Feedback feedback = new Feedback();
            feedback.success = true;
            feedback.message = "Grabbed item";
            feedback.intensity = 0.3f;
            return feedback;
        }

        private Feedback ExecuteKillGoblin()
        {
            Feedback feedback = new Feedback();
            feedback.success = true;
            feedback.message = "Killed goblin";
            feedback.intensity = 0.5f;
            AwardReputation(25);
            return feedback;
        }

        private void AwardExperience(int amount)
        {
            if (ExpManager.Instance != null)
            {
                ExpManager.Instance.GainExperience(amount);
                if (enableDebugLogs)
                    Debug.Log($"Awarded {amount} experience");
            }
        }

        private void AwardReputation(int amount)
        {
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.GainReputation(amount);
                if (enableDebugLogs)
                    Debug.Log($"Awarded {amount} reputation");
            }
        }
    }
} 