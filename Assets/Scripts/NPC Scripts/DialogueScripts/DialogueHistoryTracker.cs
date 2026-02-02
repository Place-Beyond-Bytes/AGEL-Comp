using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueHistoryTracker : MonoBehaviour
{
    private readonly HashSet<ActorSO> spokenNPCs = new HashSet<ActorSO>();




    public void RecordNPC(ActorSO actorSO)
    {
        spokenNPCs.Add(actorSO);

        Debug.Log("Just spoke to " + actorSO.actorName);
    }



    public bool HasSpokenWith(ActorSO actorSO)
    {
        return spokenNPCs.Contains(actorSO);
    }

    public bool HasSpokenWithName(string name)
    {
        foreach (var npc in spokenNPCs)
        {
            if (npc != null && npc.actorName != null && npc.actorName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
