using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FairyBolt : MonoBehaviour
{
    private float damageAmount;
    private float confusionChance;
    private float confusionDuration;
    private Vector2 startPosition;
    private float maxDistance = 2.7f; // The distance to travel before disappearing
    
    private Rigidbody2D rb;

    public void Setup(Vector2 direction, float speed, float damage, float confChance, float confDur, float distance = 2.7f)
    {
        damageAmount = damage;
        confusionChance = confChance;
        confusionDuration = confDur;
        startPosition = transform.position;
        maxDistance = distance;

        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * speed;

        // Face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Failsafe cleanup
        Destroy(gameObject, 3f);
    }

        private void Update()
    {
        // 1. Calculate how far we have traveled
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);

        // 2. If we reach the max distance, spawn puddle and die
        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // 1. Deal Raw Damage
                DamageInfo info = new DamageInfo(damageAmount, DamageElement.Psychic, AttackStyle.Ranged, transform.position, 0f);
                player.ReceiveDamage(info);

                // 2. Roll for Confusion (Twist)
                if (Random.value <= confusionChance)
                {
                    // Since StatusManager handles duration internally via StatusType, 
                    // ensure your Player's StatusManager defaults Confusion to 2.5s,
                    // or add a method to pass custom duration: player.ApplySpecificStatus(type, duration)
                    player.TryAddStatus(StatusType.Confusion);
                    Debug.Log("Fairy Bolt inflicted Confusion!");
                }
            }

            // Spawn Impact VFX here if you have one
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Environment") || collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}