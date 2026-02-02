using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseItem : MonoBehaviour
{
    public void ApplyItemEffects(ItemSO itemSO)
    {
        // Check if item is a mushroom (poisonous)
        bool isMushroom = itemSO.itemName.ToLower().Contains("mushroom");
        
        if (isMushroom)
        {
            // Mushrooms are poisonous - damage health
            int poisonDamage = 3; // Default poison damage for mushrooms
            StatsManager.Instance.UpdateHealth(-poisonDamage);
            Debug.Log("Consumed poisonous mushroom! -" + poisonDamage + " HP");
        }
        else
        {
            // Regular items - apply normal effects
            if(itemSO.currentHealth > 0)
            {
                StatsManager.Instance.UpdateHealth(itemSO.currentHealth);
                Debug.Log("Healed for +" + itemSO.currentHealth + " HP");
            }

            if (itemSO.maxHealth > 0)
            {
                StatsManager.Instance.UpdateMaxHealth(itemSO.maxHealth);
                Debug.Log("Max health increased by +" + itemSO.maxHealth);
            }

            // Pumpkin special effect
            if (itemSO.itemName.ToLower().Contains("pumpkin"))
            {
                StatsManager.Instance.UpdateSpeed(2);
                StatsManager.Instance.UpdateDamage(1);
                Debug.Log("Pumpkin used: Speed +2, Damage +1");
            }
            else
            {
                if (itemSO.speed > 0)
                {
                    StatsManager.Instance.UpdateSpeed(itemSO.speed);
                    Debug.Log("Speed increased by +" + itemSO.speed);
                }
            }
        }

        // Handle temporary effects
        if (itemSO.duration > 0 && !itemSO.itemName.ToLower().Contains("pumpkin"))
            StartCoroutine(EffectTimer(itemSO, itemSO.duration));
    }

    private IEnumerator EffectTimer(ItemSO itemSO, float duration)
    {
        yield return new WaitForSeconds(duration);

        // Revert effects (not for pumpkin)
        if (itemSO.currentHealth > 0)
        {
            StatsManager.Instance.UpdateHealth(-itemSO.currentHealth);
        }

        if (itemSO.maxHealth > 0)
        {
            StatsManager.Instance.UpdateMaxHealth(-itemSO.maxHealth);
        }

        if (!itemSO.itemName.ToLower().Contains("pumpkin"))
        {
            if (itemSO.speed > 0)
            {
                StatsManager.Instance.UpdateSpeed(-itemSO.speed);
            }
        }
    }
}
