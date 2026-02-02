using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatsUI : MonoBehaviour
{
    public GameObject[] statsSlots;
    public CanvasGroup statsCanvas;

    private bool statsOpen = false;


    private void Start()
    {
        UpdateAllStats();
    }


    private void Update()
    {
        if (Input.GetButtonDown("ToggleStats"))
            if (statsOpen)
            {
                Time.timeScale = 1;
                UpdateAllStats();
                statsCanvas.alpha = 0;
                statsCanvas.blocksRaycasts = false;
                statsOpen = false;
            }
            else
            {
                Time.timeScale = 0;
                UpdateAllStats();
                statsCanvas.alpha = 1;
                statsCanvas.blocksRaycasts = true;
                statsOpen = true;
            }
    }


    public void UpdateDamage()
    {
        statsSlots[0].GetComponentInChildren<TMP_Text>().text = "Damage: " + StatsManager.Instance.damage;
    }

    public void UpdateSpeed()
    {
        statsSlots[1].GetComponentInChildren<TMP_Text>().text = "Speed: " + StatsManager.Instance.speed;
    }

    public void UpdateLevel()
    {
        if (statsSlots.Length > 2 && statsSlots[2] != null)
            statsSlots[2].GetComponentInChildren<TMP_Text>().text = "Level: " + ExpManager.Instance.level;
    }

    public void UpdateReputation()
    {
        if (statsSlots.Length > 3 && statsSlots[3] != null)
            statsSlots[3].GetComponentInChildren<TMP_Text>().text = "Reputation: " + ReputationManager.Instance.currentReputation;
    }

    public void UpdateAllStats()
    {
        UpdateDamage();
        UpdateSpeed();
        UpdateLevel();
        UpdateReputation();
    }
}
