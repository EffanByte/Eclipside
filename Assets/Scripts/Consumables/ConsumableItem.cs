using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Eclipside/Items/Consumable")]
public class ConsumableItem : ItemData // Or inherit from a lighter ItemData base
{
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