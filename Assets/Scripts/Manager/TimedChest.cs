using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))] // Ensure it has a trigger
public class TimedChest : MonoBehaviour, IInteractable
{
    [Header("Loot Configuration")]
    [SerializeField] private List<WeaponData> weaponPool;
    [SerializeField] private List<ItemData> consumablePool; // nneed to chhange this to retrieve for consumable DB automatically
    [SerializeField] private ItemData keyItem; // Reference to your Key "CurrencyItem"

    [Header("Reward Spawning")]
    [SerializeField] private GameObject lootPedestalPrefab; // The prefab with LootPedestal script
    private int keyCount = 1;
    private static int globalKeyCount;

    [Header("Settings")]
    [SerializeField] private Sprite openVisual; // Sprite/Object to show when opened
    [SerializeField] private Sprite closedVisual;

    private bool isOpened = false;

    public void Setup(int keyCount)
    {
        SetKeyCount(keyCount);
    }

    
    private void Start()
    {
        if (keyCount == 0)
        {
            keyCount = globalKeyCount;
        }
    }

    public void Interact(PlayerController player)
    {
        if (isOpened) return;
        Debug.Log(keyCount);
        if (PlayerController.Instance.DeductCurrency(CurrencyType.Key, keyCount))
        {
            OpenChest();
        }
    }

    public string GetInteractionPrompt()
    {
        return isOpened ? "" : "Open Chest";
    }

    public void OpenChest()
    {
        isOpened = true;
        
        float roll = Random.value;
        float playerLuck = PlayerController.Instance != null ? PlayerController.Instance.GetLuckValue() : 0f;
        ItemData reward = ResolveReward(roll, playerLuck);

        Debug.Log($"[Luck] Chest opened at {playerLuck:0.##} luck. Reward: {reward?.itemName ?? "None"}");

        if (reward != null && lootPedestalPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.2f;
            
            GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
            LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
            if (pedestal != null)
            {
                pedestal.Setup(reward);
            }
        }

        gameObject.GetComponent<SpriteRenderer>().sprite = openVisual;
        StatisticsManager.Instance.IncrementStat("CHESTS_OPENED");
    }

    private ItemData ResolveReward(float roll, float playerLuck)
    {
        List<WeaponData> eligibleWeapons = GetEligibleWeapons();
        List<ItemData> eligibleItems = GetEligibleItems();

        if (roll < 0.6f && eligibleWeapons.Count > 0)
        {
            return LuckUtility.PickWeightedByRarity(eligibleWeapons, playerLuck, weapon => weapon.rarity);
        }

        if (roll < 0.9f && eligibleItems.Count > 0)
        {
            return LuckUtility.PickWeightedByRarity(eligibleItems, playerLuck, item => item.rarity);
        }

        if (IsRewardSupported(keyItem))
        {
            return keyItem;
        }

        if (eligibleItems.Count > 0)
        {
            return LuckUtility.PickWeightedByRarity(eligibleItems, playerLuck, item => item.rarity);
        }

        if (eligibleWeapons.Count > 0)
        {
            return LuckUtility.PickWeightedByRarity(eligibleWeapons, playerLuck, weapon => weapon.rarity);
        }

        return null;
    }

    private List<WeaponData> GetEligibleWeapons()
    {
        List<WeaponData> eligibleWeapons = new List<WeaponData>();
        if (weaponPool == null)
        {
            return eligibleWeapons;
        }

        for (int i = 0; i < weaponPool.Count; i++)
        {
            WeaponData weapon = weaponPool[i];
            if (IsRewardSupported(weapon))
            {
                eligibleWeapons.Add(weapon);
            }
        }

        return eligibleWeapons;
    }

    private List<ItemData> GetEligibleItems()
    {
        List<ItemData> eligibleItems = new List<ItemData>();
        if (consumablePool == null)
        {
            return eligibleItems;
        }

        for (int i = 0; i < consumablePool.Count; i++)
        {
            ItemData item = consumablePool[i];
            if (IsRewardSupported(item))
            {
                eligibleItems.Add(item);
            }
        }

        return eligibleItems;
    }

    private bool IsRewardSupported(ItemData item)
    {
        return item is WeaponData || item is ConsumableItem || item is CurrencyItem;
    }

    private void SetKeyCount(int count)
    {
        keyCount = count;
    }
    public static void SetGloalKeyCount(int count)
    {
        globalKeyCount = count;
    }
}
