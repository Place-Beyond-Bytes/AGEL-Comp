using System.Collections.Generic;
using UnityEngine;

namespace AGEL
{
    [System.Serializable]
    public class State
    {
        public Vector3 playerPosition;
        public float playerHealth;
        public float playerMaxHealth;
        public List<GameObject> nearbyObjects;
        public List<ItemSO> inventoryItems;
        public Dictionary<string, object> environmentState;
        
        public State()
        {
            nearbyObjects = new List<GameObject>();
            inventoryItems = new List<ItemSO>();
            environmentState = new Dictionary<string, object>();
        }
        
        public override string ToString()
        {
            return $"State[Health:{playerHealth}/{playerMaxHealth}, Position:{playerPosition}, Objects:{nearbyObjects.Count}]";
        }
    }
    
    [System.Serializable]
    public class ActionPlan
    {
        public List<string> actions;
        public string reasoning;
        public float confidence;
        
        public ActionPlan()
        {
            actions = new List<string>();
            reasoning = "";
            confidence = 0.0f;
        }
        
        public ActionPlan(List<string> actions, string reasoning, float confidence)
        {
            this.actions = actions;
            this.reasoning = reasoning;
            this.confidence = confidence;
        }
        
        public override string ToString()
        {
            return $"ActionPlan[Actions:{string.Join(",", actions)}, Confidence:{confidence}]";
        }
    }
    
    [System.Serializable]
    public class Feedback
    {
        public float healthChange;
        public float damageTaken;
        public bool success;
        public string message;
        public float intensity; // 0.0 to 1.0, how strong the feedback is
        
        public Feedback()
        {
            healthChange = 0f;
            damageTaken = 0f;
            success = true;
            message = "";
            intensity = 0.0f;
        }
        
        public Feedback(float healthChange, float damageTaken, bool success, string message, float intensity)
        {
            this.healthChange = healthChange;
            this.damageTaken = damageTaken;
            this.success = success;
            this.message = message;
            this.intensity = intensity;
        }
        
        public override string ToString()
        {
            return $"Feedback[Health:{healthChange}, Damage:{damageTaken}, Success:{success}, Intensity:{intensity}]";
        }
    }
    
    [System.Serializable]
    public class Episode
    {
        public State state;
        public ActionPlan actionPlan;
        public Feedback feedback;
        public float timestamp;
        
        public Episode(State state, ActionPlan actionPlan, Feedback feedback)
        {
            this.state = state;
            this.actionPlan = actionPlan;
            this.feedback = feedback;
            this.timestamp = Time.time;
        }
        
        public override string ToString()
        {
            return $"Episode[State:{state}, Actions:{actionPlan}, Feedback:{feedback}]";
        }
    }
    
    [System.Serializable]
    public class FOLRule
    {
        public string rule;
        public float weight;
        public string description;
        
        public FOLRule(string rule, float weight)
        {
            this.rule = rule;
            this.weight = weight;
            this.description = "";
        }
        
        public FOLRule(string rule, float weight, string description)
        {
            this.rule = rule;
            this.weight = weight;
            this.description = description;
        }
        
        public override string ToString()
        {
            return $"{rule} (w={weight})";
        }
    }
    
    [System.Serializable]
    public class Command
    {
        public string action;
        public Vector3 target;
        public GameObject targetObject;
        public float duration;
        public Dictionary<string, object> parameters;
        
        public Command(string action)
        {
            this.action = action;
            this.target = Vector3.zero;
            this.targetObject = null;
            this.duration = 0f;
            this.parameters = new Dictionary<string, object>();
        }
        
        public Command(string action, Vector3 target)
        {
            this.action = action;
            this.target = target;
            this.targetObject = null;
            this.duration = 0f;
            this.parameters = new Dictionary<string, object>();
        }
        
        public Command(string action, GameObject targetObject)
        {
            this.action = action;
            this.target = Vector3.zero;
            this.targetObject = targetObject;
            this.duration = 0f;
            this.parameters = new Dictionary<string, object>();
        }
        
        public override string ToString()
        {
            return $"Command[{action}, Target:{target}, Object:{targetObject?.name ?? "none"}]";
        }
    }
} 