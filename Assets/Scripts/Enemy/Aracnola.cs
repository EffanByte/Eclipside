using UnityEngine;
using System.Collections;

public class Aracnola : EnemyBase
{
    [Header("Aracnola Settings")]
    public bool isElite = false;
    public GameObject webProjectilePrefab;
    
    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        base.Start();
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 45f;
        stats.damage = 7.5f; // 0.75 hearts
        stats.moveSpeed = 3.0f;
        stats.attackCooldown = 2.8f;
    }

    protected override void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;

        // Flee if too close, approach if too far
        if (dist < 3f) 
        {
            MoveTowardsTarget(-dirToPlayer); // Run away
        }
        else if (dist > 8f) 
        {
            MoveTowardsTarget(dirToPlayer); // Approach
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Hold ground
        }

        spriteRenderer.flipX = dirToPlayer.x > 0;

        // Attack if in range
        if (dist <= 9f && isAttackReady && !isAttackRoutineRunning && dist >= 3f)
        {
            ChangeState(EnemyState.Attacking);
        }
    }

    protected override IEnumerator AttackSequence()
    {
        isAttackRoutineRunning = true;
        rb.linearVelocity = Vector2.zero;

        float windup = isElite ? 0.5f : 0.6f;
        yield return new WaitForSeconds(windup);

        if (playerTarget != null && webProjectilePrefab != null)
        {
            Vector2 aimDir = (playerTarget.position - transform.position).normalized;
            GameObject web = Instantiate(webProjectilePrefab, transform.position, Quaternion.identity);
            
            // Add Elite Projectile speed logic here if player is slowed
        }

        StartCoroutine(AttackCooldownRoutine());
        isAttackRoutineRunning = false;
        ChangeState(EnemyState.Chasing);
    }

    // TWIST: Weakness to Fire
    public override void ReceiveDamage(DamageInfo dmg)
    {
        if (dmg.element == DamageElement.Fire)
        {
            dmg.amount *= 2f; // Takes double damage
            StartCoroutine(statusMgr.FlashSpriteRoutine(DamageElement.Fire)); // Visual flair
        }
        base.ReceiveDamage(dmg);
    }
}