using UnityEngine;
using System.Collections.Generic;

public enum GachaRarity { Common, Rare, Epic, Mythical }
public enum RewardType { Currency, Consumable, Weapon, Character, Gold, Orb }

[System.Serializable]
public class GachaRewardEntry
{
    public string idName; // e.g., "Rusty Sword" or "300 Gold"
    public RewardType type;
    public GachaRarity rarity;
    
    [Header("Values")]
    public int amount = 1; // For Currency/Consumables
    public ItemData itemReference; // If Weapon/Consumable
    public CharacterData characterReference;

    [Header("Duplicate Logic")]
    public int duplicateConversionAmount; // e.g. 300
    public CurrencyType duplicateConversionType; // e.g. Gold or Orbs
}

[CreateAssetMenu(menuName = "Eclipside/Gacha/Meteorite Banner")]
public class MeteoriteBanner : ScriptableObject
{
    public string bannerName; // e.g., "Dusty Meteorite"
    public string backendBannerId;
    
    [Header("Cost")]
    public CurrencyType currencyType; // Gold or Orbs
    public int singlePullCost;
    public int tenPullCost; // e.g., 950 for Orbs

    [Header("Probabilities (Must sum to 100)")]
    public float probCommon;
    public float probRare;
    public float probEpic;
    public float probMythic;

    [Header("Loot Pools")]
    public List<GachaRewardEntry> commonPool;
    public List<GachaRewardEntry> rarePool;
    public List<GachaRewardEntry> epicPool;
    public List<GachaRewardEntry> mythicPool;

    public string GetBackendBannerId()
    {
        if (!string.IsNullOrWhiteSpace(backendBannerId))
        {
            return backendBannerId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(bannerName))
        {
            return bannerName.Trim().ToLowerInvariant().Replace(" ", "_");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim().ToLowerInvariant().Replace(" ", "_");
        }

        return "arcane_meteorite";
    }
}
