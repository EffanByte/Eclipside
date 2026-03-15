using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SpiderWebProjectile : MonoBehaviour
{
    public GameObject webPuddlePrefab; // Drag the Web Puddle prefab here in Inspector
    
    private Aracnola spawner;
    private Rigidbody2D rb;
    private bool isElite;
    private float damageAmount;
    
    // --- DISTANCE TRACKING ---
    private Vector2 startPosition;
    private float maxDistance; // The distance to travel before bursting

    public void Setup(Aracnola spawnerRef, Vector2 direction, float speed, float damage, bool elite, float maxDist = 0.8f)
    {
        spawner = spawnerRef;
        isElite = elite;
        damageAmount = damage;
        
        // Store starting data
        startPosition = transform.position;
        maxDistance = maxDist;

        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * speed;

        // Face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Failsafe: Still destroy after 5 seconds just in case it gets stuck
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        // 1. Calculate how far we have traveled
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);

        // 2. If we reach the max distance, spawn puddle and die
        if (distanceTraveled >= maxDistance)
        {
            SpawnPuddleAndDestroy();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Deal Impact Damage
            DamageInfo info = new DamageInfo(damageAmount, DamageElement.Physical, AttackStyle.Ranged, transform.position, 0f);
            collision.GetComponent<PlayerController>()?.ReceiveDamage(info);
        }
        // If it hits a wall/environment before reaching max distance
        else if (collision.CompareTag("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            SpawnPuddleAndDestroy();
        }
    }

    private void SpawnPuddleAndDestroy()
    {
        if (webPuddlePrefab != null)
        {
            GameObject puddle = Instantiate(webPuddlePrefab, transform.position, Quaternion.identity);
            
            WebPuddle puddleScript = puddle.GetComponent<WebPuddle>();
            if (puddleScript != null)
            {
                puddleScript.Setup(isElite);
            }

            // Report back to the spider so it can manage the 2-web limit
            if (spawner != null && spawner.currentState != EnemyState.Dead)
            {
                spawner.RegisterNewWeb(puddle);
            }
        }
        
        Destroy(gameObject);
    }
}