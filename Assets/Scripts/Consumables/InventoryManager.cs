using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Storage")]
    public ConsumableItem[] slots = new ConsumableItem[3];
    public int currentItemIndex = 0; 

    // Called by PlayerController input
    public void TriggerItemUse()
    {
        // Check bounds
        if (currentItemIndex < 0 || currentItemIndex >= slots.Length) return;

        ConsumableItem itemToUse = slots[currentItemIndex];

        if (itemToUse != null)
        {
            // Pass the Player GameObject to the item logic
            itemToUse.Use(PlayerController.instance);
            Debug.Log($"Used item: {itemToUse.itemName} from slot {currentItemIndex + 1}");
            // Remove item (Consume)
            slots[currentItemIndex] = null;
        }
        else
        {
            Debug.Log($"Slot {currentItemIndex + 1} is empty.");
        }
    }
    public void NextItem()
    {
        currentItemIndex = (currentItemIndex + 1) % slots.Length;
        Debug.Log($"Selected item slot: {currentItemIndex + 1}");
    }

    public void AddItem(ConsumableItem newItem)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = newItem;
                Debug.Log($"Added item: {newItem.itemName} to slot {i + 1}");
                return;
            }
        }
        Debug.Log("Inventory full! Could not add item: " + newItem.itemName);
    }
}