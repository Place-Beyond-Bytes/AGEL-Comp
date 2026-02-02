using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopInfo : MonoBehaviour
{
    public CanvasGroup infoPanel;

    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;

    [Header ("Stat Fields")]
    public TMP_Text[] statTexts;

    private RectTransform infoPanelRect;


    private void Awake()
    {
        infoPanelRect = GetComponent<RectTransform>();
    }



    public void ShowItemInfo(ItemSO itemSO)
    {
        infoPanel.alpha = 1;

        itemNameText.text = itemSO.itemName;
        itemDescriptionText.text = itemSO.itemDescription;

        List<string> stats = new List<string>();
        
        // Check if item is a mushroom
        bool isMushroom = itemSO.itemName.ToLower().Contains("mushroom");
        
        if (isMushroom)
        {
            stats.Add("POISONOUS: -3 HP");
        }
        else if (itemSO.currentHealth > 0)
        {
            stats.Add("Health: +" + itemSO.currentHealth.ToString());
        }
        
        if (itemSO.damage > 0) stats.Add("Damage: " + itemSO.damage.ToString());
        if (itemSO.speed > 0) stats.Add("Speed: " + itemSO.speed.ToString());
        if (itemSO.duration > 0) stats.Add("Duration: " + itemSO.duration.ToString());

        if (stats.Count <= 0)
            return;

        for (int i = 0; i < statTexts.Length; i++)
        {
            if (i < stats.Count)
            {
                statTexts[i].text = stats[i];
                statTexts[i].gameObject.SetActive(true);
                
                // Color code poisonous items
                if (stats[i].Contains("POISONOUS:"))
                {
                    statTexts[i].color = Color.red;
                }
                else
                {
                    statTexts[i].color = Color.white;
                }
            }
            else
            {
                statTexts[i].gameObject.SetActive(false);
            }
        }

    }



    public void HideItemInfo()
    {
        infoPanel.alpha = 0;

        itemNameText.text = "";
        itemDescriptionText.text = "";
    }


    public void FollowMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 offset = new Vector3(10, -10, 0);

        infoPanelRect.position = mousePosition + offset;
    }
}
