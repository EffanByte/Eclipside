using UnityEngine;
using TMPro;

[RequireComponent(typeof(BoxCollider2D))] // Trigger for interaction
public class LootPedestal : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    private ItemData content;
    private bool isCollected = false;

    public void Setup(ItemData item)
    {
        content = item;
        
        spriteRenderer.sprite = item.icon;

    }

    public void Interact(PlayerController player)
    {
        if (isCollected || content == null) return;

        // --- GIVE ITEM LOGIC ---
        // Reuse your standard acquisition logic
        if (content is WeaponData w) 
        {
            player.EquipWeapon(w);
        }
        // not using currency in chests
        // else if (content is CurrencyItem c) 
        // {
        //     player.AddCurrency(c.currencyType, c.amount);
        // }
        else if (content is ConsumableItem con)
        {
            PlayerController.Instance.GetComponent<InventoryManager>().AddItem(con);
        }

        isCollected = true;
        Destroy(gameObject);
        return;
    }

    public string GetInteractionPrompt()
    {
        return content != null ? $"Pick up {content.itemName}" : "";
    }
}