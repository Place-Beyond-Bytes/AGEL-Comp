using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Image portrait;
    public TMP_Text actorName;
    public TMP_Text dialogueText;
    public Button[] choiceButtons;

    public bool isDialogueActive;

    private DialogueSO currentDialogue;
    private int dialogueIndex;

    private float lastDialogueEndTime;
    private float dialogueCooldown = .1f;



    private void Awake()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        foreach (var button in choiceButtons)
            button.gameObject.SetActive(false);
    }


    public bool CanStartDialogue()
    {
        return Time.unscaledTime - lastDialogueEndTime >= dialogueCooldown;
    }

    public void StartDialogue(DialogueSO dialogueSO)
    {
        currentDialogue = dialogueSO;
        dialogueIndex = 0;
        isDialogueActive = true;
        ShowDialogue();
        if (AGELAgent.Instance != null && dialogueSO != null && dialogueSO.name.Equals("Yellow_WontTalkToYou", System.StringComparison.OrdinalIgnoreCase))
        {
            AGELAgent.Instance.receivedSecret = true;
            Debug.Log("[Goal 7 Debug] Received secret from NPC (Yellow_WontTalkToYou)");
            if (AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex].ToLower().Contains("purple bob") && AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex].ToLower().Contains("secret"))
            {
                Debug.Log($"Goal {AGELAgent.Instance.currentGoalIndex + 1} completed: {AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex]} goal_status=Complete");
                if (ExpManager.Instance != null)
                    ExpManager.Instance.LevelUpDirectly();
                AGELAgent.Instance.currentGoalIndex++;
                AGELAgent.Instance.goblinDefeatedForGoal = false;
                if (AGELAgent.Instance.currentGoalIndex < AGELAgent.Instance.goals.Count)
                {
                    Debug.Log($"Goal {AGELAgent.Instance.currentGoalIndex + 1}: {AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex]}");
                }
                else
                {
                    Debug.Log("All goals completed! Objective achieved: " + AGELAgent.Instance.mainObjective);
                }
            }
        }
        if (AGELAgent.Instance != null && dialogueSO != null && dialogueSO.name.Replace(" ","").ToLower().Contains("findbrothers_complete"))
        {
            AGELAgent.Instance.foundBlueQuestComplete = true;
            Debug.Log("[Goal 8 Debug] Found Blue quest complete (PurpleBob_Quest_ FindBrothers_Complete)");
        }
    }



    public void AdvanceDialogue()
    {
        if (dialogueIndex < currentDialogue.lines.Length)
            ShowDialogue();
        else
            ShowChoices();
    }



    private void ShowDialogue()
    {
        DialogueLine line = currentDialogue.lines[dialogueIndex];

        GameManager.Instance.DialogueHistoryTracker.RecordNPC(line.speaker);

        portrait.sprite = line.speaker.portrait;
        actorName.text = line.speaker.actorName;

        dialogueText.text = line.text;

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Check for Goal 8 completion (Find Brother Blue and report back to Purple Bob)
        if (AGELAgent.Instance != null && AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex].ToLower().Contains("brother blue") && AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex].ToLower().Contains("report back"))
        {
            var tracker = GameManager.Instance.DialogueHistoryTracker;
            bool spokenToBlue = tracker.HasSpokenWithName("Blue Bob");
            // Only complete if talking to Purple Bob and Blue Bob has already been spoken to
            if (line.speaker.actorName == "Purple Bob" && spokenToBlue)
            {
                Debug.Log($"Goal {AGELAgent.Instance.currentGoalIndex + 1} completed: {AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex]} goal_status=Complete");
                if (ExpManager.Instance != null)
                    ExpManager.Instance.LevelUpDirectly();
                AGELAgent.Instance.currentGoalIndex++;
                AGELAgent.Instance.goblinDefeatedForGoal = false;
                if (AGELAgent.Instance.currentGoalIndex < AGELAgent.Instance.goals.Count)
                {
                    Debug.Log($"Goal {AGELAgent.Instance.currentGoalIndex + 1}: {AGELAgent.Instance.goals[AGELAgent.Instance.currentGoalIndex]}");
                }
                else
                {
                    Debug.Log("All goals completed! Objective achieved: " + AGELAgent.Instance.mainObjective);
                }
            }
        }

        dialogueIndex++;
    }




    private void ShowChoices()
    {
        ClearChoices();

        if(currentDialogue.options.Length > 0)
        {
            if (currentDialogue.options.Length > choiceButtons.Length)
            {
                Debug.LogError("Not enough choice buttons for the number of dialogue options!");
                return;
            }
            for (int i = 0; i < currentDialogue.options.Length; i++)
            {
                var option = currentDialogue.options[i];

                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = option.optionText;
                choiceButtons[i].gameObject.SetActive(true);

                choiceButtons[i].onClick.AddListener(() => ChooseOption(option.nextDialogue));
            }
        }
        else
        {
            choiceButtons[0].GetComponentInChildren<TMP_Text>().text = "End";
            choiceButtons[0].onClick.AddListener(EndDialogue);
            choiceButtons[0].gameObject.SetActive(true);
        }

        EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject);
    }




    private void ChooseOption(DialogueSO dialogueSO)
    {
        if (dialogueSO == null)
            EndDialogue();
        else
        {
            ClearChoices();
            StartDialogue(dialogueSO);
        }
    }



    private void EndDialogue()
    {
        dialogueIndex = 0;
        isDialogueActive = false;
        ClearChoices();

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        lastDialogueEndTime = Time.unscaledTime;
    }



    private void ClearChoices()
    {
        foreach (var button in choiceButtons)
        {
            button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
        }
    }

    private void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
    }
}
