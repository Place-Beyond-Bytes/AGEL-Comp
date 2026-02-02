using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LocationSO")]
public class LocationSO : ScriptableObject
{
    public string locationID;           //ideally immutable
    public string displayName;


}
