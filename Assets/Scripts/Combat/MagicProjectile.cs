using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MagicProjectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private float knockback;
    private Rigidbody2D rb;
    private Transform target;

    [Header("Homing Stats")]
    public float homingStrength = 1.5f; // "Slightly" homing (Low number)
    public float detectionRadius = 5f;

    public void Setup(Vector2 direction, float speed, float damage, float knockback)
    {
        this.speed = speed;
        this.damage = damage;
        this.knockback = knockback;

        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed; // Use 'velocity' if on older Unity

        // Destroy after 3 seconds so it doesn't fly forever
        Destroy(gameObject, 3f); 
        
        FindNearestTarget();
    }

    private void FindNearestTarget()
    {
        // Find closest enemy to lock onto
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = hit.transform;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (target != null)
        {
            // Calculate direction to target
            Vector2 direction = (Vector2)target.position - rb.position;
            direction.Normalize();

            // "Slightly" rotate the velocity towards the target
            // We use Vector3.RotateTowards or simple interpolation
            Vector2 newVelocity = Vector2.Lerp(rb.linearVelocity.normalized, direction, homingStrength * Time.fixedDeltaTime);
            
            rb.linearVelocity = newVelocity * speed;

            // Optional: Rotate the sprite to face velocity
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log($"Magic Hit {collision.name} for {damage}");
            // collision.GetComponent<EnemyHealth>().TakeDamage(damage, knockback);
            Destroy(gameObject);
        }
    }
}