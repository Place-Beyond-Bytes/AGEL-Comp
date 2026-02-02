using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    public int expReward = 3;

    public delegate void MonsterDefeated(int exp);
    public static event MonsterDefeated OnMonsterDefeated;

    public int currentHealth;
    public int maxHealth;

    private bool killedByArrow = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void MarkKilledByArrow()
    {
        killedByArrow = true;
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;

        if(currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        else if( currentHealth <= 0)
        {
            OnMonsterDefeated(expReward);
            // Award reputation if this is a goblin
            if ((gameObject.CompareTag("Goblin") || gameObject.name.ToLower().Contains("goblin")) && ReputationManager.Instance != null)
            {
                ReputationManager.Instance.GainReputation(25);
                if (AGELAgent.Instance != null)
                {
                    AGELAgent.Instance.goblinDefeatedForGoal = true;
                    AGELAgent.Instance.goblinsKilledForGoal++;
                    if (killedByArrow)
                    {
                        AGELAgent.Instance.goblinKilledByArrowForGoal = true;
                        killedByArrow = false;
                    }
                }
            }
            // Track sheep kills for AGEL goals
            if ((gameObject.CompareTag("Sheep") || gameObject.name.ToLower().Contains("sheep")) && AGELAgent.Instance != null)
            {
                AGELAgent.Instance.sheepKilledForGoal++;
                Debug.Log($"[Goal 6 Debug] Sheep killed! sheepKilledForGoal = {AGELAgent.Instance.sheepKilledForGoal}");
            }
            Destroy(gameObject);
        }

    }
}
