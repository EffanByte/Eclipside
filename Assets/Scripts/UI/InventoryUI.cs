using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Image sprite1;
    [SerializeField] private Image sprite2;
    [SerializeField] private Image sprite3;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InventoryManager.Instance.OnInventoryUpdated += UpdateInventoryUI;
        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        Color tempColor = sprite1.color;
        if (InventoryManager.Instance.slots[0])
        {
        sprite1.sprite = InventoryManager.Instance.slots[0].icon;
        tempColor.a = 1.0f;
        }
        else
            tempColor.a = 0.0f;
        sprite1.color = tempColor;
        if (InventoryManager.Instance.slots[1])
        {
        sprite2.sprite = InventoryManager.Instance.slots[1].icon;
        tempColor.a = 1.0f;
        }
        else
            tempColor.a = 0.0f;
        sprite2.color = tempColor;
        if (InventoryManager.Instance.slots[1])
        {
        sprite3.sprite = InventoryManager.Instance.slots[2].icon;
        tempColor.a = 1.0f;
        }
        else
            tempColor.a = 0.0f;
        sprite3.color = tempColor;
    }
}
