using TMPro;
using UnityEngine;
public class ItemRender : MonoBehaviour
{
     private SpriteRenderer spriteRenderer;
    private ItemData itemData;
    private TextMeshProUGUI Price;

    public void Initialize(ItemData data)
    {
        itemData = data;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (itemData.icon == null)
        {
            Destroy(spriteRenderer);
            TextMeshProUGUI itemText = Instantiate(new TextMeshProUGUI(), transform);
            itemText.text = itemData.itemName;
        }
        spriteRenderer.sprite = itemData.icon;
        Price = GetComponentInChildren<TextMeshProUGUI>();
        MapPricetoRarity(itemData.rarity);
    }

    public void MapPricetoRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                Price.text = "15";
                break;
            case ItemRarity.Rare:
                Price.text = "25";
                break;
            case ItemRarity.Epic:
                Price.text = "40";
                break;
            case ItemRarity.Mythical:
                Price.text = "50";
                break;
            case ItemRarity.Key:
                Price.text = "20";
                break;
        }
    }
}
