using UnityEngine;

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
        EnsureSpriteRenderer();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"LootPedestal on {name} is missing a SpriteRenderer.");
            return;
        }

        if (item != null && item.icon != null)
        {
            spriteRenderer.sprite = item.icon;
        }
        else
        {
            Debug.LogWarning($"LootPedestal received reward '{item?.itemName ?? "Unknown"}' without an icon. Keeping fallback sprite.");
        }

    }

    public void Interact(PlayerController player)
    {
        if (isCollected || content == null) return;

        bool collected = false;

        // --- GIVE ITEM LOGIC ---
        // Reuse your standard acquisition logic
        if (content is WeaponData w) 
        {
            if (player.EquipWeapon(w))
            {
                ItemAcquisitionToast.Show(w);
                collected = true;
            }
        }
        else if (content is CurrencyItem c)
        {
            collected = AwardCurrency(player, c);
            if (collected)
            {
                ItemAcquisitionToast.Show(c);
            }
        }
        else if (content is ConsumableItem con)
        {
            InventoryManager inventory = player != null ? player.GetComponent<InventoryManager>() : null;
            collected = inventory != null && inventory.AddItem(con);
        }

        if (!collected)
        {
            return;
        }

        isCollected = true;
        Destroy(gameObject);
        return;
    }

    public string GetInteractionPrompt()
    {
        return content != null ? $"Pick up {content.GetDisplayName()}" : "";
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private static bool AwardCurrency(PlayerController player, CurrencyItem currency)
    {
        if (player == null || currency == null)
        {
            return false;
        }

        switch (currency.currencyType)
        {
            case CurrencyType.Rupee:
            case CurrencyType.Key:
                player.AddCurrency(currency.currencyType, currency.amount);
                return true;

            case CurrencyType.Gold:
            case CurrencyType.Orb:
                CurrencyManager.AddCurrency(currency.currencyType, currency.amount);
                return true;

            default:
                Debug.LogWarning($"LootPedestal does not support awarding currency type {currency.currencyType}.");
                return false;
        }
    }
}
