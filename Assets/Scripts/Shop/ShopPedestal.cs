using UnityEngine;
using TMPro;

public class ShopPedestal : MonoBehaviour, IInteractable
{
    [Header("Component References")]
    [Tooltip("The SpriteRenderer that shows the item icon")]
    [SerializeField] private SpriteRenderer itemSprite;
    
    [Tooltip("The TextMeshPro (World) that shows the price")]
    [SerializeField] private TextMeshPro priceText; 
    
    [Tooltip("Optional: A visual object (like an X mark) shown when sold out")]

    // --- State ---
    private int slotIndex;
    private bool isSoldOut = false;
    private ItemData currentItem;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (itemSprite == null) itemSprite = GetComponentInChildren<SpriteRenderer>();
        if (priceText == null) priceText = GetComponentInChildren<TextMeshPro>();
    }

    // Called by ShopZone.cs
    public void Setup(ItemData item, int index)
    {
        currentItem = item;
        slotIndex = index;
        isSoldOut = false;

        // Reset visual state
        if (itemSprite != null) itemSprite.gameObject.SetActive(true);
        if (priceText != null) priceText.gameObject.SetActive(true);

        if (item != null)
        {
            // 1. Set Sprite
            if (itemSprite != null)
            {
                itemSprite.sprite = item.icon;
                
                // Fallback if no icon 
                if (item.icon == null) 
                {
                    //  enable a text label here 
                }
            }

            // 2. Set Price (Using Logic from ShopManager)
            if (priceText != null)
            {
                int price = ShopManager.Instance.GetItemPrice(item);
                priceText.text = $"{price} R";
                
                // Set color based on rarity (Visual polish)
                priceText.color = GetRarityColor(item.rarity);
            }
        }
        else
        {
            // Null item logic
            SetSoldOut();
        }
    }

    public void SetSoldOut()
    {
        isSoldOut = true;
        
        // Hide Item
        if (itemSprite != null) itemSprite.gameObject.SetActive(false);
        
        // Show "SOLD" text
        if (priceText != null)
        {
            priceText.text = "SOLD";
            priceText.color = Color.gray;
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(!isSoldOut && priceText != null) priceText.fontStyle = FontStyles.Bold;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            priceText.fontStyle = FontStyles.Normal;
    }

    // Helper for visual flair
    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Rare: return new Color(0.3f, 0.6f, 1f); // Blue
            case ItemRarity.Epic: return new Color(0.6f, 0.2f, 0.8f); // Purple
            case ItemRarity.Mythical: return new Color(1f, 0.8f, 0f); // Gold
            default: return Color.white;
        }
    }

    public void Interact(PlayerController player)
    {
        if (isSoldOut) return;
        // Safety Check
        if (GameDirector.Instance != null && GameDirector.Instance.IsWaveActive)
        {
            Debug.Log("Cannot shop during combat!");
            return;
        }
        if (ShopManager.Instance.TryBuyItem(slotIndex))
        {
            SetSoldOut();
        }
        return;
    }

    public string GetInteractionPrompt()
    {
        if (isSoldOut) return "";
        int price = ShopManager.Instance.GetItemPrice(currentItem);
        return $"Buy {price} R";
    }
}