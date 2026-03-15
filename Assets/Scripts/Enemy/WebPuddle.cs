using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))] // Must be a Trigger!
public class WebPuddle : MonoBehaviour
{
    private float slowAmount;
    private float lingeringSlow;
    private float lingeringDuration;
    private float lifeTime;
    private float elapsedTime = 0f;


    public void Setup(bool isElite)
    {
        if (isElite)
        {
            slowAmount = -0.55f;        // -55%
            lingeringSlow = 0.30f;      // -30% (used in ApplySpeedDebuff)
            lingeringDuration = 1.0f;
            lifeTime = 8.0f;
        }
        else
        {
            slowAmount = -0.45f;        // -45%
            lingeringSlow = 0.25f;      // -25%
            lingeringDuration = 0.8f;
            lifeTime = 6.0f;
        }

        // Auto-destroy after its lifetime
        StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        while (elapsedTime < lifeTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // If player is still standing on it when it vanishes, release them
        if (PlayerController.Instance != null)
        {
            ReleasePlayer(PlayerController.Instance);
        }
        
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (PlayerController.Instance != null)
            {
                // Directly modify speed (e.g. subtract 45%)
                PlayerController.Instance.ApplyBuff("Web", StatType.Speed, -slowAmount, lifeTime); 
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && PlayerController.Instance != null)
        {
            
        }
    }

    private void ReleasePlayer(PlayerController player)
    {
        player.RemoveBuff("Web");
    }
}