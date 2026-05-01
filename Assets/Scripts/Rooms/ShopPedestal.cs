using UnityEngine;
using TMPro;

public class ShopPedestal : MonoBehaviour, IInteractable
{
    [Header("Component References")]
    [Tooltip("The SpriteRenderer that shows the item icon")]
    [SerializeField] private SpriteRenderer itemSprite;
    
    [Tooltip("The TextMeshPro (World) that shows the price")]
    [SerializeField] private TextMeshPro priceText; 
    
    [Header("Hover")]
    [SerializeField] private bool enableHover = true;
    [SerializeField] private float hoverAmplitude = 0.04f;
    [SerializeField] private float hoverFrequency = 1.6f;

    // --- State ---
    private int slotIndex;
    private bool isSoldOut = false;
    private ItemData currentItem;
    private Sprite fallbackSprite;
    private Vector3 baseLocalPosition;
    private float hoverOffset;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (itemSprite == null) itemSprite = GetComponentInChildren<SpriteRenderer>();
        if (priceText == null) priceText = GetComponentInChildren<TextMeshPro>();

        if (itemSprite != null)
        {
            fallbackSprite = itemSprite.sprite;
        }

        baseLocalPosition = transform.localPosition;
        hoverOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (!enableHover)
        {
            return;
        }

        float hoverY = Mathf.Sin((Time.time * hoverFrequency) + hoverOffset) * hoverAmplitude;
        transform.localPosition = baseLocalPosition + Vector3.up * hoverY;
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
                itemSprite.sprite = item.icon != null ? item.icon : fallbackSprite;
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
        {
            if (priceText != null) priceText.fontStyle = FontStyles.Normal;
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

    public void Interact(PlayerController player)
    {
        if (isSoldOut) return;
        // Safety Check
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
