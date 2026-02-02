using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Talk : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    public Animator interactAnim;

    public List<DialogueSO> conversations;
    public DialogueSO currentConversation;




    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }



    private void OnEnable()
    {
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        anim.Play("Idle");
        interactAnim.Play("Open");
    }



    private void OnDisable()
    {
        if (interactAnim != null && interactAnim.gameObject.activeInHierarchy)
            interactAnim.Play("Close");
        rb.isKinematic = false;
    }


    private void Update()
    {
        if (Input.GetButtonDown("Interact"))
        {
            if (GameManager.Instance == null || GameManager.Instance.DialogueManager == null)
            {
                Debug.LogError("GameManager or DialogueManager is not assigned!");
                return;
            }

            if (GameManager.Instance.DialogueManager.isDialogueActive)
                GameManager.Instance.DialogueManager.AdvanceDialogue();
            else
            {
                if (GameManager.Instance.DialogueManager.CanStartDialogue())
                {
                    CheckForNewConversation();
                    GameManager.Instance.DialogueManager.StartDialogue(currentConversation);
                }
            }
        }
    }


    private void CheckForNewConversation()
    {
        for (int i = 0; i < conversations.Count; i++)
        {
            var convo = conversations[i];
            if(convo != null && convo.IsConditionMet())
            {
                currentConversation = convo;

                //Remove this if it's one-time only
                if(convo.removeAfterPlay)
                    conversations.RemoveAt(i);

                //Remove any other dialogues that should be cleared when this one plays (like quest completion)
                if(convo.removeTheseOnPlay != null && convo.removeTheseOnPlay.Count > 0)
                {
                    foreach (var toRemove in convo.removeTheseOnPlay)
                    {
                        conversations.Remove(toRemove);
                    }
                }

                break;
            }
        }
    }
}
