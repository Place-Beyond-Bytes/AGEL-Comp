using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sheep_Health : MonoBehaviour
{
    public int currentHealth = 1;
    public int maxHealth = 1;
    public GameObject meatPrefab;
    public string layerName;

    private void Start()
    {
        currentHealth = maxHealth;
        if (!string.IsNullOrEmpty(layerName))
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1)
                gameObject.layer = layer;
            else
                Debug.LogWarning($"Layer '{layerName}' not found. Sheep will remain on its current layer.");
        }
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (currentHealth <= 0)
        {
            if (ReputationManager.Instance != null)
                ReputationManager.Instance.GainReputation(-25);
            if (meatPrefab != null)
                Instantiate(meatPrefab, transform.position, Quaternion.identity);
            // AGEL goal tracking for sheep kills
            if (AGELAgent.Instance != null)
            {
                AGELAgent.Instance.sheepKilledForGoal++;
                Debug.Log($"[Goal 6 Debug] Sheep killed! sheepKilledForGoal = {AGELAgent.Instance.sheepKilledForGoal}");
            }
            Destroy(gameObject);
        }
    }
} 