using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Image sprite1;
    [SerializeField] private Image sprite2;
    [SerializeField] private Image sprite3;

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated += UpdateInventoryUI;
        }

        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        if (InventoryManager.Instance == null || InventoryManager.Instance.slots == null)
        {
            SetSlotVisual(sprite1, null);
            SetSlotVisual(sprite2, null);
            SetSlotVisual(sprite3, null);
            return;
        }

        SetSlotVisual(sprite1, GetSlotItem(0));
        SetSlotVisual(sprite2, GetSlotItem(1));
        SetSlotVisual(sprite3, GetSlotItem(2));
    }

    private ConsumableItem GetSlotItem(int slotIndex)
    {
        var slots = InventoryManager.Instance.slots;
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            return null;
        }

        return slots[slotIndex];
    }

    private void SetSlotVisual(Image slotImage, ConsumableItem item)
    {
        if (slotImage == null)
        {
            return;
        }

        Color color = slotImage.color;
        if (item != null && item.icon != null)
        {
            slotImage.sprite = item.icon;
            color.a = 1.0f;
        }
        else
        {
            slotImage.sprite = null;
            color.a = 0.0f;
        }

        slotImage.color = color;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated -= UpdateInventoryUI;
        }
    }
}
