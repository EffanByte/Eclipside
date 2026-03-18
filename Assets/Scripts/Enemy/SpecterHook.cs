using UnityEngine;

public class SpecterHook : MonoBehaviour
{
    private ChainedSpecter owner;
    private float damage;
    private float maxDistance;
    private Vector3 startPos;
    private Rigidbody2D rb;

    public void Setup(ChainedSpecter spawner, Vector2 dir, float speed, float dmg, float range)
    {
        owner = spawner;
        damage = dmg;
        maxDistance = range;
        startPos = transform.position;

        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Update()
    {
        // Check Max Range
        if (Vector2.Distance(startPos, transform.position) >= maxDistance)
        {
            TriggerAnchor();
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            // Direct Hit: Damage, but NO Anchor
            DamageInfo info = new DamageInfo(damage, DamageElement.Physical, AttackStyle.Ranged, transform.position, 0f);
            col.GetComponent<PlayerController>()?.ReceiveDamage(info);
            Destroy(gameObject);
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Environment") || col.CompareTag("Wall"))
        {
            // Missed Player, Hit Wall: Create Anchor
            TriggerAnchor();
        }
    }

    private void TriggerAnchor()
    {
        rb.linearVelocity = Vector2.zero; // Stop moving
        
        // Tell Specter to yank to this position
        if (owner != null)
        {
            owner.StartYankDash(transform.position);
            
            // Optional: Draw a LineRenderer chain between this object and the Specter
        }
        
        // Destroy the hook after the dash completes (e.g., 1s delay)
        Destroy(gameObject, 1.0f);
    }
}