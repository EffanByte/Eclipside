using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; 

[RequireComponent(typeof(Collider2D))] 
public class AreaEffectPool : MonoBehaviour
{
    private List<ItemEffect> onTickEffects = new List<ItemEffect>();
    private List<ItemEffect> onEnterEffects = new List<ItemEffect>();
    
    private float tickRate = 1.0f;
    private PlayerController targetPlayer;
    private bool isPlayerInside = false;

    private string poolInstanceID; 

    private void Awake()
    {
        poolInstanceID = "Pool_" + Guid.NewGuid().ToString().Substring(0, 8);
    }

    public void Initialize(float duration, float rate, List<ItemEffect> tickfx, List<ItemEffect> enterfx)
    {
        this.tickRate = rate;
        this.onTickEffects = tickfx ?? new List<ItemEffect>(); 
        this.onEnterEffects = enterfx ?? new List<ItemEffect>();

        StartCoroutine(DespawnRoutine(duration));
        StartCoroutine(TickRoutine());
    }

    private IEnumerator DespawnRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (isPlayerInside && targetPlayer != null)
        {
            RemoveEnterEffects(targetPlayer);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            targetPlayer = collision.GetComponent<PlayerController>();
            if (targetPlayer != null)
            {
                isPlayerInside = true;
                
                foreach (var effect in onEnterEffects)
                {
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
                RemoveEnterEffects(targetPlayer);
            }

            isPlayerInside = false;
            targetPlayer = null;
        }
    }

    private void RemoveEnterEffects(PlayerController player)
    {
        // NOTE: Only applies to Enter Effects (like a persistent speed debuff)
        player.RemoveBuff(poolInstanceID);
    }

    private IEnumerator TickRoutine()
    {
        while (true)
        {
            if (isPlayerInside && targetPlayer != null)
            {
                foreach (var effect in onTickEffects)
                {
                    // Pass the unique ID so the effect can decide to stack or ignore
                    effect.Apply(targetPlayer, poolInstanceID); 
                }
            }

            yield return new WaitForSeconds(tickRate);
        }
    }
}