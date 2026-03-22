using UnityEngine;

[RequireComponent(typeof(Collider2D))] // IsTrigger = True
public class BiomePortal : MonoBehaviour, IInteractable
{
    private bool isUsed = false;

    // If you want the player to just touch it to advance:
    /*
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isUsed)
        {
            isUsed = true;
            GameDirector.Instance.AdvanceToNextBiome();
        }
    }
    */

    // If you want the player to PRESS a button to advance (Better UX):
    public void Interact(PlayerController player)
    {
        if (isUsed) return;
        
        isUsed = true;
        Debug.Log("<color=cyan>Player entered the Portal!</color>");
        
        // Tell the Director to load the next stage
        GameDirector.Instance.AdvanceToNextBiome();
        
        return;
    }

    public string GetInteractionPrompt() => "Enter Portal";
}