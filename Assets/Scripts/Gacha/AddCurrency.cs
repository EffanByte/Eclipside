using UnityEngine;

public class AddCurrency : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact(PlayerController player)
    {
        CurrencyManager.AddCurrency(CurrencyType.Gold, 1000);
        CurrencyManager.AddCurrency(CurrencyType.Orb, 50);
    }
    public string GetInteractionPrompt()
    {
        return "Add Currency";
    }
}
