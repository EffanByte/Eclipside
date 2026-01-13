using UnityEngine;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    [Header("Storage")]
    public ConsumableItem[] slots = new ConsumableItem[3];
    public int currentItemIndex = 0; 
    public ItemData keyItem; // Reference to Key item

    public event Action OnInventoryUpdated;

    void Awake()
    {
        if (Instance == null) Instance = this;   
    }
    // Called by PlayerController input
    public void TriggerItemUse(int slot)
    {
        // Check bounds
        if (currentItemIndex < 0 || slot >= slots.Length) return;

        ConsumableItem itemToUse = slots[slot];

        if (itemToUse != null)
        {
            // Pass the Player GameObject to the item logic
            itemToUse.Use(PlayerController.Instance);
            Debug.Log($"Used item: {itemToUse.itemName} from slot {slot + 1}");
            slots[currentItemIndex] = null;
            OnInventoryUpdated?.Invoke();
        }
        else
        {
            Debug.Log($"Slot {slot + 1} is empty.");
        }

    }
    public void NextItem()
    {
        currentItemIndex = (currentItemIndex + 1) % slots.Length;
        Debug.Log($"Selected item slot: {currentItemIndex + 1}");
    }

    public void AddItem(ItemData newItem)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = newItem as ConsumableItem;
                Debug.Log($"Added item: {newItem.itemName} to slot {i + 1}");
                OnInventoryUpdated?.Invoke();
                return;
            }
        }   
        Debug.Log("Inventory full! Could not add item: " + newItem.itemName);
        
    }
}