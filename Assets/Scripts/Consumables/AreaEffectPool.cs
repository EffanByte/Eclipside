using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))] // Needs a Trigger Collider
public class AreaEffectPool : MonoBehaviour
{
    private List<ItemEffect> onTickEffects = new List<ItemEffect>();
    private List<ItemEffect> onEnterEffects = new List<ItemEffect>();
    
    private float tickRate = 1.0f;
    private PlayerController targetPlayer;
    private bool isPlayerInside = false;

    // --- INITIALIZATION ---
    // Called by the ScriptableObject immediately after spawning
    public void Initialize(float duration, float rate, List<ItemEffect> tickfx, List<ItemEffect> enterfx)
    {
        this.tickRate = rate;
        this.onTickEffects = tickfx;
        this.onEnterEffects = enterfx;

        // Auto-destroy pool after duration
        Destroy(gameObject, duration);

        // Start the heartbeat
        StartCoroutine(TickRoutine());
    }

    // --- COLLISION LOGIC ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            targetPlayer = collision.GetComponent<PlayerController>();
            if (targetPlayer != null)
            {
                isPlayerInside = true;
                
                // Apply "One Time" effects (Buffs, Debuffs) immediately upon entering
                foreach (var effect in onEnterEffects)
                {
                    effect.Apply(targetPlayer);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            targetPlayer = null;
        }
    }

    // --- EFFECT LOOP ---
    private IEnumerator TickRoutine()
    {
        while (true)
        {
            // Only apply effects if player is standing in it
            if (isPlayerInside && targetPlayer != null)
            {
                foreach (var effect in onTickEffects)
                {
                    effect.Apply(targetPlayer);
                }
            }

            yield return new WaitForSeconds(tickRate);
        }
    }
}