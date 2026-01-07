using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Storage")]
    public ItemData[] slots = new ItemData[3];

    // Called by PlayerController input
    public void TriggerItemUse(int slotIndex)
    {
        // Check bounds
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        ItemData itemToUse = slots[slotIndex];

        if (itemToUse != null)
        {
            // Pass the Player GameObject to the item logic
            itemToUse.Use(PlayerController.instance);
            Debug.Log($"Used item: {itemToUse.itemName} from slot {slotIndex + 1}");
            // Remove item (Consume)
            slots[slotIndex] = null;
            
            // TODO: Update UI Event here
        }
        else
        {
            Debug.Log($"Slot {slotIndex + 1} is empty.");
        }
    }

    public bool AddItem(ItemData newItem)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = newItem;
                Debug.Log($"Picked up {newItem.itemName}");
                return true;
            }
        }
        Debug.Log("Inventory Full!");
        return false;
    }
}