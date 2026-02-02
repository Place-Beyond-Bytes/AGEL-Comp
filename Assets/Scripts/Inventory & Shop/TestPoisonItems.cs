using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPoisonItems : MonoBehaviour
{
    [Header("Test Items")]
    public ItemSO mushroom; // Regular mushroom (will be poisonous)
    public ItemSO steak; // Healing item for comparison
    
    [Header("Test Controls")]
    public KeyCode addMushroomKey = KeyCode.Alpha1;
    public KeyCode addSteakKey = KeyCode.Alpha2;

    private void Update()
    {
        // Add mushroom to inventory (poisonous)
        if (Input.GetKeyDown(addMushroomKey))
        {
            if (mushroom != null)
            {
                InventoryManager.Instance.AddItem(mushroom, 1);
                Debug.Log("Added Mushroom to inventory (poisonous)");
            }
        }
        
        // Add steak to inventory (healing)
        if (Input.GetKeyDown(addSteakKey))
        {
            if (steak != null)
            {
                InventoryManager.Instance.AddItem(steak, 1);
                Debug.Log("Added Steak to inventory (healing)");
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label("Item Test Controls:");
        GUILayout.Label("Press 1: Add Mushroom (poisonous)");
        GUILayout.Label("Press 2: Add Steak (healing)");
        GUILayout.Label("Press 7: Use item from slot 1");
        GUILayout.Label("Press 8: Use item from slot 2");
        GUILayout.Label("Press 9: Use item from slot 3");
        GUILayout.Label("Mushrooms are poisonous (-3 HP)!");
        GUILayout.EndArea();
    }
} 