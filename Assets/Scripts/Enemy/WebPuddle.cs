using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))] // Must be a Trigger!
public class WebPuddle : MonoBehaviour
{
    private float slowAmount;
    private float lingeringSlow;
    private float lingeringDuration;
    private float lifeTime;



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
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (PlayerController.Instance != null)
            {
                // Directly modify speed (e.g. subtract 45%)
                PlayerController.Instance.ApplyBuff(StatType.Speed, slowAmount, lifeTime); 
                Destroy(gameObject); // Destroy immediately after applying the initial slow
            }
        }
    }

}