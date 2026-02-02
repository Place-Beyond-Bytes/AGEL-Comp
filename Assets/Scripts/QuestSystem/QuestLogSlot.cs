using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestLogSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questLevelText;

    public QuestSO currentQuest;



    private void OnValidate()
    {
        if(currentQuest != null)
            SetQuest(currentQuest);
    }


    public void SetQuest(QuestSO questSO)
    {
        currentQuest = questSO;

        questNameText.text = questSO.questName;
        questLevelText.text = "Lv. " + questSO.questLevel.ToString();
    }

}
