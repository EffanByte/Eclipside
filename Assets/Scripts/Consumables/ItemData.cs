using UnityEngine;
using System.Collections.Generic;

public enum ItemRarity { Common, Rare, Epic, Mythical }

[CreateAssetMenu(menuName = "Eclipside/Items/Consumable")]
public class ConsumableItem : ScriptableObject // Or inherit from a lighter ItemData base
{
    [Header("General Info")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemRarity rarity;

    [Header("Configuration")]
    public bool consumeOnUse = true;

    [Header("Logic")]
    // THIS IS THE KEY: A list of effects instead of hardcoded logic
    public List<ItemEffect> effects = new List<ItemEffect>();

    public void Use(PlayerController player)
    {
        // Run all effects (e.g., Heal + Cure Poison)
        foreach (var effect in effects)
        {
            effect.Apply(player);
        }
    }
}