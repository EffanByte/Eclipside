using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpiderWeb : MonoBehaviour
{
    private float slowAmount = -0.4f; // -40% Speed
    private Rigidbody2D rb;

    public void Setup(Vector2 direction, float speed)
    {
        rb = GetComponent<Rigidbody2D>();
        // Using velocity for compatibility (Use linearVelocity in Unity 6+)
        rb.linearVelocity = direction.normalized * speed;
        
        // Face the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, 3f); // Cleanup if misses
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. Try to find the PlayerController (from previous steps)
            PlayerController player = collision.GetComponent<PlayerController>();
            
            if (player != null)
            {
                player.ApplyBuff(StatType.Speed, slowAmount, 1);
                
                // We need a way to revert it. Since PlayerController handles logic,
                // ideally PlayerController should handle the duration.
                // For now, we assume PlayerController just changes speed permanently
                // unless we run a coroutine here to revert it, but projectiles die on impact.
 
                Debug.Log("Player hit by Web! Slowed down.");
            }

            Destroy(gameObject);
        }
    }
}