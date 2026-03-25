using System;
using System.Collections.Generic;
using UnityEngine;

public static class LuckUtility
{
    private const float StatusChanceBonusPerLuck = 0.01f;
    private const float RupeeBonusPerLuck = 0.005f;
    private const float RareLootBonusPerLuck = 0.005f;
    private const float EpicLootBonusPerLuck = 0.01f;
    private const float MythicLootBonusPerLuck = 0.02f;

    public static float GetStatusProcChanceBonus(float luck)
    {
        return Mathf.Max(0f, luck) * StatusChanceBonusPerLuck;
    }

    public static float GetRupeeMultiplier(float luck)
    {
        return 1f + (Mathf.Max(0f, luck) * RupeeBonusPerLuck);
    }

    public static ItemRarity RollRarity(float[] baseWeights, float luck, ItemRarity fallback = ItemRarity.Common)
    {
        if (baseWeights == null || baseWeights.Length == 0)
        {
            return fallback;
        }

        float totalWeight = 0f;
        for (int i = 0; i < baseWeights.Length; i++)
        {
            totalWeight += Mathf.Max(0f, baseWeights[i]) * GetLootWeightMultiplier((ItemRarity)i, luck);
        }

        if (totalWeight <= 0f)
        {
            return fallback;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float runningWeight = 0f;

        for (int i = 0; i < baseWeights.Length; i++)
        {
            runningWeight += Mathf.Max(0f, baseWeights[i]) * GetLootWeightMultiplier((ItemRarity)i, luck);
            if (roll <= runningWeight)
            {
                return (ItemRarity)i;
            }
        }

        return (ItemRarity)Mathf.Clamp(baseWeights.Length - 1, 0, (int)ItemRarity.Mythical);
    }

    public static T PickWeightedByRarity<T>(IList<T> items, float luck, Func<T, ItemRarity> raritySelector) where T : class
    {
        if (items == null || items.Count == 0)
        {
            return null;
        }

        List<T> validItems = new List<T>();
        float totalWeight = 0f;

        foreach (T item in items)
        {
            if (item == null)
            {
                continue;
            }

            validItems.Add(item);
            totalWeight += GetLootWeightMultiplier(raritySelector(item), luck);
        }

        if (validItems.Count == 0)
        {
            return null;
        }

        if (totalWeight <= 0f)
        {
            return validItems[UnityEngine.Random.Range(0, validItems.Count)];
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float runningWeight = 0f;

        foreach (T item in validItems)
        {
            runningWeight += GetLootWeightMultiplier(raritySelector(item), luck);
            if (roll <= runningWeight)
            {
                return item;
            }
        }

        return validItems[validItems.Count - 1];
    }

    public static float GetLootWeightMultiplier(ItemRarity rarity, float luck)
    {
        float positiveLuck = Mathf.Max(0f, luck);
        if (positiveLuck <= 0f)
        {
            return 1f;
        }

        return rarity switch
        {
            ItemRarity.Rare => 1f + (positiveLuck * RareLootBonusPerLuck),
            ItemRarity.Epic => 1f + (positiveLuck * EpicLootBonusPerLuck),
            ItemRarity.Mythical => 1f + (positiveLuck * MythicLootBonusPerLuck),
            _ => 1f
        };
    }
}
