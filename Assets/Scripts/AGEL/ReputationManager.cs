using UnityEngine;
using TMPro;

public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance;
    public int currentReputation = 0;
    public TMP_Text reputationText; // Assign in Inspector

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void GainReputation(int amount)
    {
        currentReputation += amount;
        Debug.Log($"Reputation increased by {amount}. Total: {currentReputation}");
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (reputationText != null)
            reputationText.text = "Reputation: " + currentReputation;
    }
} 