using UnityEngine;
using System;

[DisallowMultipleComponent]
public class FurnitureID : MonoBehaviour
{
    [SerializeField]
    string uniqueID;

    public string UniqueID => uniqueID;

#if UNITY_EDITOR

    void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
        }
    }

#endif
}