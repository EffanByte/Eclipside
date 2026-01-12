using UnityEngine;
using TMPro;
using System;

public class ShopRefreshStatue : MonoBehaviour, IInteractable
{
    [SerializeField] private TextMeshPro costText;

    public void Start()
    {
        UpdateCost();
    }
    public void UpdateCost()
    {
        int cost = ShopManager.Instance.GetRefreshCost();
        if (costText != null) costText.text = $"Reroll: {cost}";
    }

    public void Interact(PlayerController player)
    {
        if (ShopManager.Instance.TryRefreshShop())
            UpdateCost();
    }

    string IInteractable.GetInteractionPrompt()
    {
        throw new NotImplementedException();
    }
}