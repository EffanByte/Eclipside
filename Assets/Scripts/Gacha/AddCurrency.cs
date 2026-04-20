using TMPro;
using UnityEngine;

public class AddCurrency : MonoBehaviour, IInteractable
{

    [SerializeField] private TextMeshPro debugOutput;
    public void Interact(PlayerController player)
    {
        CurrencyManager.AddCurrency(CurrencyType.Gold, 1000);
        CurrencyManager.AddCurrency(CurrencyType.Orb, 50);
        if (debugOutput != null)
        {
            debugOutput.text = "Added test currency.";
        }
    }
    public string GetInteractionPrompt()
    {
        return "Add Currency";
    }
}
