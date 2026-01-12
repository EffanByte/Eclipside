using UnityEngine;
using System.Collections.Generic;
using System;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private int shopSlots = 5; // How many items appear at once
    
    [Header("Item Pools")]
    [SerializeField] private List<ItemData> allConsumables; // Drag all your potions/buffs here
    [SerializeField] private ItemData keyItem; // Create a "Key" ItemData asset
    [SerializeField] private ItemData xpItem;  // Create an "XP Bundle" ItemData asset

    [Header("Price Settings (GDD Page 1/2)")]
    private const int PRICE_COMMON = 15;
    private const int PRICE_RARE = 25;
    private const int PRICE_EPIC = 40;
    private const int PRICE_KEY = 20;
    private const int PRICE_XP = 30; // Define X amount price here

    [Header("Refresh Settings")]
    private const int BASE_REFRESH_COST = 5;
    private const int MAX_REFRESHES = 3;

    // --- Runtime State ---
    private int currentRefreshCount = 0;
    public ItemData[] currentStock; // The items currently on the shelf
    public bool[] isSoldOut;        // Tracks if slot 0, 1, or 2 is bought

    // Events for UI
    public event Action OnShopUpdated; // Called when bought or refreshed
    public event Action<string> OnTransactionFailed; // "Not enough money"

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // For testing purposes, generate a shop on start.
        // In real game, call GenerateShop() when entering the Shop Room.
        GenerateShop();
    }

    // ---------------------------------------------------------
    // GENERATION LOGIC
    // ---------------------------------------------------------

    public void GenerateShop()
    {
        currentRefreshCount = 0; // Reset refresh cost for new shop encounter
        RollNewItems();
    }

private void RollNewItems()
    {
        currentStock = new ItemData[shopSlots];
        isSoldOut = new bool[shopSlots];

        currentStock[0] = keyItem;
        currentStock[1] = xpItem;
        currentStock[2] = GetRandomConsumable(ItemRarity.Common);
        currentStock[3] = GetRandomConsumable(ItemRarity.Rare);
        currentStock[4] = GetRandomConsumable(ItemRarity.Epic);

        for (int i = 0; i < shopSlots; i++) isSoldOut[i] = false;

        OnShopUpdated?.Invoke();
        Debug.Log("Shop Refreshed: Key, XP, Common, Rare, Epic");
    }

    private ItemData GetRandomConsumable(ItemRarity targetRarity)
    {
        // Filter the list for matching rarity
        // Note: Ideally, cache these lists in Start() so you don't loop every time
        List<ItemData> candidates = new List<ItemData>();

        foreach (var item in allConsumables)
        {
            if (item.rarity == targetRarity)
            {
                candidates.Add(item);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        Debug.LogWarning($"No items found for rarity: {targetRarity}");
        return null;
    }

    // ---------------------------------------------------------
    // REFRESH LOGIC
    // ---------------------------------------------------------

    public int GetRefreshCost()
    {
        // First refresh = 5. Increment by 5 each time.
        // 0 refreshes done = 5 cost
        // 1 refresh done = 10 cost
        return BASE_REFRESH_COST + (currentRefreshCount * 5);
    }

    public void TryRefreshShop()
    {
        if (currentRefreshCount >= MAX_REFRESHES)
        {
            OnTransactionFailed?.Invoke("Max refreshes reached!");
            return;
        }

        int cost = GetRefreshCost();
        PlayerController player = PlayerController.Instance;
    
        if (player.rupees >= cost)
        {
            player.rupees -= cost;
            player.NotifyUIUpdate();

            currentRefreshCount++;
            RollNewItems();
        }
        else
        {
            OnTransactionFailed?.Invoke("Not enough Rupees to refresh!");
        }
    }

    // ---------------------------------------------------------
    // BUYING LOGIC
    // ---------------------------------------------------------

    public int GetItemPrice(ItemData item)
    {
        if (item == null) return 0;

        // Specific overrides
        if (item == keyItem) return PRICE_KEY;
        if (item == xpItem) return PRICE_XP;

        // Standard Rarity Pricing
        // Assuming ConsumableItem has a 'rarity' field (from previous steps)
        if (item is ConsumableItem consumable)
        {
            switch (consumable.rarity)
            {
                case ItemRarity.Common: return PRICE_COMMON;
                case ItemRarity.Rare: return PRICE_RARE;
                case ItemRarity.Epic: return PRICE_EPIC;
                case ItemRarity.Mythical: return 100; // Just in case
            }
        }

        return PRICE_COMMON; // Default fallback
    }

    public void TryBuyItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= shopSlots) return;
        if (isSoldOut[slotIndex]) return;

        ItemData item = currentStock[slotIndex];
        if (item == null) return;

        int price = GetItemPrice(item);
        PlayerController player = PlayerController.Instance;

        if (player.rupees >= price)
        {
            // 1. Deduct Money
            PlayerController.Instance.AddCurrency(CurrencyType.Rupee, -price);
            
            
            // NOTE: Ideally, Key and XP are just ConsumableItems with "EffectAddKey" or "EffectAddXP"
            // But if you handle them specially:
            if (item is CurrencyItem currencyItem) 
            {
                Debug.Log("Bought key");
                player.AddCurrency(currencyItem.currencyType, currencyItem.amount);
            }
            else
            {
                // Try add to inventory
                player.GetComponent<InventoryManager>().AddItem(item);
            }

            // 3. Finalize Transaction
            isSoldOut[slotIndex] = true;
            player.NotifyUIUpdate();
            OnShopUpdated?.Invoke();
            
            Debug.Log($"Bought {item.itemName} for {price}");
        }
        else
        {
            OnTransactionFailed?.Invoke("Not enough Rupees!");
        }
    }
}