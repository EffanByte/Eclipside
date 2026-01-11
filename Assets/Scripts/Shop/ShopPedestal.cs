using UnityEngine;
using TMPro;

public class ShopPedestal : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The SpriteRenderer that shows the item icon")]
    [SerializeField] private SpriteRenderer itemSprite;
    
    [Tooltip("The TextMeshPro (World) that shows the price")]
    [SerializeField] private TextMeshPro priceText; 
    
    [Tooltip("Optional: A visual object (like an X mark) shown when sold out")]
    [SerializeField] private GameObject soldOutVisual; 

    // --- State ---
    private int slotIndex;
    private bool isSoldOut = false;
    private bool playerInRange = false;
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
        if (soldOutVisual != null) soldOutVisual.SetActive(false);
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

        // Show visual marker
        if (soldOutVisual != null) soldOutVisual.SetActive(true);
    }

    private void Update()
    {
        // Interaction Logic
        if (playerInRange && !isSoldOut && Input.GetKeyDown(KeyCode.E))
        {
            // Block buying during combat
            if (GameDirector.Instance != null && GameDirector.Instance.IsWaveActive)
            {
                return;
            }

            ShopManager.Instance.TryBuyItem(slotIndex);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            // Highlight text if valid
            if(!isSoldOut && priceText != null) priceText.fontStyle = FontStyles.Bold;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            if(priceText != null) priceText.fontStyle = FontStyles.Normal;
        }
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
}