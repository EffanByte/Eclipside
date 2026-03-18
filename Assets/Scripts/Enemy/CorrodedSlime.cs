using UnityEngine;
using System.Collections;

public class CorrodedSlime : EnemyBase
{
    [Header("Slime Config")]
    public bool isElite = false;
    public GameObject poisonPuddlePrefab; // Use the generic AreaEffectPool we made earlier!
    public ItemEffect poisonEffect; // Drag the Effect_PoisonTick asset here

    private const float TILE = 0.3f;

    private void Awake()
    {

        if (isElite)
        {
            stats.maxHealth = 30f;      // 3.0 Hearts
            stats.moveSpeed = 6.5f;     
            stats.damage = 10f;         // 1.0 Heart (Melee Slam)
            stats.contactDamage = 5f;   // 0.5 Hearts (Bump)
            stats.attackCooldown = 1.8f;
            stats.attackWindup = 0.4f;  
            stats.aggroRadius = 8f;
            stats.attackRange = 1.5f;
        }

    }

    protected override IEnumerator AttackWindup()
    {
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetTrigger("Attack");
        yield return new WaitForSeconds(stats.attackWindup);
    }

    protected override void ExecuteAttack()
    {
        // GDD: "Range: 1.5 tiles in front."
        // We do a circle check, but filter to only hit things IN FRONT.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stats.attackRange);
        Vector2 facingDir = transform.localScale.x < 0 ? Vector2.right : Vector2.left; // Adjust based on your sprite setup

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Verify the player is actually in front of the slime
                float playerRelativeX = hit.transform.position.x - transform.position.x;
                float playerDirX = playerRelativeX > 0 ? 1f : -1f;
                float facingDirX = facingDir.x > 0 ? 1f : -1f;

                if (facingDirX == playerDirX)
                {
                    PerformAttack(hit.gameObject);
                }
            }
        }
    }

    protected override IEnumerator AttackRecovery()
    {
        // GDD: Base 0.4s, Elite 0.35s
        float recovery = isElite ? 0.35f : 0.4f;
        yield return new WaitForSeconds(recovery);
    }

    // --- THE TWIST: DEATH PUDDLE ---
    protected override void OnDeath()
    {
        if (poisonPuddlePrefab != null && poisonEffect != null)
        {
            // Spawn the generic AreaEffectPool
            GameObject puddleObj = Instantiate(poisonPuddlePrefab, transform.position, Quaternion.identity);
            AreaEffectPool poolScript = puddleObj.GetComponent<AreaEffectPool>();

            if (poolScript != null)
            {
                float duration = isElite ? 7.0f : 6.0f;
                float radius = (isElite ? 2.0f : 1.5f) * TILE;
                
                // Adjust physical size of the trigger area
                puddleObj.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

                // Setup the pool configuration
                System.Collections.Generic.List<ItemEffect> tickEffects = new System.Collections.Generic.List<ItemEffect> { poisonEffect };
                
                // Tick extremely fast (0.1s) so it constantly refreshes the poison timer while player stands in it
                poolScript.Initialize(duration, 0.1f, tickEffects, null);
                
                Debug.Log("<color=green>[Slime] Spawned Toxic Death Puddle!</color>");
            }
        }
    }
}