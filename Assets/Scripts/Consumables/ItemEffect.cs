using UnityEngine;

// Base "Lego Block"
public abstract class ItemEffect : ScriptableObject
{
    public abstract void Apply(PlayerController player, string source = "");
}