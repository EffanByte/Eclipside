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
            Collider2D interactionCollider = GetComponent<Collider2D>();
            if (interactionCollider != null)
            {
                interactionCollider.enabled = false;
            }
        }
    }

    public string GetInteractionPrompt()
    {
        return "yup";
    }

    protected abstract void PerformEvent(PlayerController player);
}
