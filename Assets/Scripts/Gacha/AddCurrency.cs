using TMPro;
using UnityEngine;

public class AddCurrency : MonoBehaviour, IInteractable
{

    [SerializeField] private TextMeshPro debugOutput;
    public void Interact(PlayerController player)
    {
        CurrencyManager.AddCurrency(CurrencyType.Gold, 1000);
        CurrencyManager.AddCurrency(CurrencyType.Orb, 50);
        debugOutput.text = $"Added 1000 Gold and 50 Orbs!";
    }
    public string GetInteractionPrompt()
    {
        return "Add Currency";
    }
}
