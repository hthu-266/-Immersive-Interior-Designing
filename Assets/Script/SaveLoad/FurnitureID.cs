using UnityEngine;
using System;

[DisallowMultipleComponent]
public class FurnitureID : MonoBehaviour
{
    [SerializeField]
    string uniqueID;

    public string UniqueID => uniqueID;

    void Awake()
    {
        EnsureUniqueID();
    }

#if UNITY_EDITOR

    void OnValidate()
    {
        EnsureUniqueID();
    }

#endif

    void EnsureUniqueID()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
        }
    }
}
