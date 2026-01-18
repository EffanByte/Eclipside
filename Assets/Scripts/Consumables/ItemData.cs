using UnityEngine;

public enum ItemRarity { Common, Rare, Epic, Mythical, Key }
public enum CurrencyType { Rupee, Key, XP, Gold, Orb }
public abstract class ItemData : ScriptableObject
{
    [Header("Core Info")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemRarity rarity;
}

// Example of the wrapper class
