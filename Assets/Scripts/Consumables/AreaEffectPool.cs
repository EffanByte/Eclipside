using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; // Required for Guid

[RequireComponent(typeof(Collider2D))] // Needs a Trigger Collider
public class AreaEffectPool : MonoBehaviour
{
    private List<ItemEffect> onTickEffects = new List<ItemEffect>();
    private List<ItemEffect> onEnterEffects = new List<ItemEffect>();
    
    private float tickRate = 1.0f;
    private PlayerController targetPlayer;
    private bool isPlayerInside = false;

    // UNIQUE ID: So if 5 pools overlap, they don't overwrite each other's buffs in PlayerController
    private string poolInstanceID; 

    private void Awake()
    {
        // Generate a random string ID like "Pool_f47ac10b"
        poolInstanceID = "Pool_" + Guid.NewGuid().ToString().Substring(0, 8);
    }

    // --- INITIALIZATION ---
    public void Initialize(float duration, float rate, List<ItemEffect> tickfx, List<ItemEffect> enterfx)
    {
        this.tickRate = rate;
        this.onTickEffects = tickfx ?? new List<ItemEffect>(); // Safety against nulls
        this.onEnterEffects = enterfx ?? new List<ItemEffect>();

        // We use a Coroutine for destruction so we can clean up buffs first
        StartCoroutine(DespawnRoutine(duration));
        
        StartCoroutine(TickRoutine());
    }

    // --- CLEAN DESPAWN ---
    private IEnumerator DespawnRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        // CRITICAL FIX 3: Clean up if the player is standing inside when the pool vanishes
        if (isPlayerInside && targetPlayer != null)
        {
            RemoveEnterEffects(targetPlayer);
        }

        Destroy(gameObject);
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
                
                // CRITICAL FIX 1 & 2: Apply effects using this pool's unique ID
                foreach (var effect in onEnterEffects)
                {
                    // Pass the unique ID so PlayerController tracks THIS specific pool's buff
                    effect.Apply(targetPlayer, poolInstanceID); 
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (targetPlayer != null)
            {
                // CRITICAL FIX 1: Remove the specific buffs this pool applied
                RemoveEnterEffects(targetPlayer);
            }

            isPlayerInside = false;
            targetPlayer = null;
        }
    }

    private void RemoveEnterEffects(PlayerController player)
    {
        // Tell the player to remove any permanent buff associated with this pool's ID
        // Note: You must ensure PlayerController has a 'RemoveBuff' method that accepts a string key.
        player.RemoveBuff(poolInstanceID);
    }

    // --- EFFECT LOOP ---
    private IEnumerator TickRoutine()
    {
        while (true)
        {
            if (isPlayerInside && targetPlayer != null)
            {
                foreach (var effect in onTickEffects)
                {
                    // Tick effects (like healing 0.2/sec) don't need a permanent ID 
                    // because they apply instantly and don't linger.
                    effect.Apply(targetPlayer, ""); 
                }
            }

            yield return new WaitForSeconds(tickRate);
        }
    }
}