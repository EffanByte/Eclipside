public interface IInteractable
{
    // Returns true if interaction was successful
    void Interact(PlayerController player);
    
    string GetInteractionPrompt(); 
}