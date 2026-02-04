using UnityEngine;

public abstract class EventObject : MonoBehaviour, IInteractable
{
    [Header("Base Settings")]
    [SerializeField] protected bool oneTimeUse = true;
    protected bool isUsed = false;

    public void Interact(PlayerController player)
    {
        if (isUsed && oneTimeUse) return;
        
        PerformEvent(player);
        
        if (oneTimeUse)
        {
            isUsed = true;
            // Optional: Disable collider or change sprite to "Used" state
        }
    }

    public string GetInteractionPrompt()
    {
        return "yup";
    }

    protected abstract void PerformEvent(PlayerController player);
}