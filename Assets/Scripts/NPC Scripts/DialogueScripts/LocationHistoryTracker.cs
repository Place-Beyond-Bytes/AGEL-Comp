using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationHistoryTracker : MonoBehaviour
{
    private readonly HashSet<LocationSO> locationsVisited = new HashSet<LocationSO>();



    public void RecordLocation(LocationSO locationSO)
    {
        if (locationsVisited.Add(locationSO))
        {
            Debug.Log("Just visited to " + locationSO.displayName);
        }
    }



    public bool HasVisited(LocationSO locationSO)
    {
        return locationsVisited.Contains(locationSO);
    }
}
