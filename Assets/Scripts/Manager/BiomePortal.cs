using UnityEngine;

[RequireComponent(typeof(Collider2D))] // IsTrigger = True
public class BiomePortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController player)
    {
        if (GameDirector.Instance == null)
        {
            return;
        }

        Debug.Log("<color=cyan>Player interacted with the biome portal.</color>");
        GameDirector.Instance.HandlePortalInteraction();
    }

    public string GetInteractionPrompt()
    {
        if (GameDirector.Instance == null)
        {
            return LocalizationManager.GetString(LocalizationManager.DefaultTable, "portal.enter", "Enter Portal");
        }

        return GameDirector.Instance.GetPortalPrompt();
    }
}
