using UnityEngine;
using System.Collections;

public class CrystalStag : EnemyBase
{
    [Header("Stag Settings")]
    public bool isElite = false;
    public float chargeSpeed = 8f;
    public float stunOnCrash = 2.0f;
    
    private bool isCharging = false;
    private Vector2 chargeDirection;

    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        base.Start();
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 60f;
        stats.damage = 20f; // 2 Hearts charge
        chargeSpeed = 9.5f;
        stats.moveSpeed = 3.5f;
        stats.attackCooldown = 4.5f;
        stunOnCrash = 1.5f;
    }

    protected override void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        // "Will not charge if closer than 5 or farther than 14"
        if (dist >= 5f && dist <= 14f && isAttackReady && !isCharging)
        {
            // Raycast to check Line of Sight (Optional, but in GDD)
            RaycastHit2D hit = Physics2D.Raycast(transform.position, (playerTarget.position - transform.position).normalized, dist, LayerMask.GetMask("Environment"));
            if (hit.collider == null)
            {
                ChangeState(EnemyState.Attacking);
                return;
            }
        }

        base.LogicChasing(); // Normal patrol movement
    }

    protected override IEnumerator AttackSequence()
    {
        isAttackRoutineRunning = true;
        rb.linearVelocity = Vector2.zero; 

        // 1. Lock-On (Windup)
        chargeDirection = (playerTarget.position - transform.position).normalized;
        // Flip to face player
        spriteRenderer.flipX = chargeDirection.x > 0;

        float windup = isElite ? 0.6f : 0.8f;
        yield return new WaitForSeconds(windup);

        // 2. Charge
        isCharging = true;
        rb.linearVelocity = chargeDirection * chargeSpeed;

        // Failsafe: Stop charge after X seconds if it doesn't hit anything
        yield return new WaitForSeconds(14f / chargeSpeed); // Max distance 14 / speed
        
        if (isCharging) StopCharge(false);
    }

    protected override void OnTriggerStay2D(Collider2D collision)
    {
        base.OnTriggerStay2D(collision); // Normal contact damage

        if (!isCharging) return;

        if (collision.CompareTag("Player"))
        {
            PerformAttack(collision.gameObject);
            
            // Push player back
            PlayerController pc = collision.GetComponent<PlayerController>();
            if (pc != null) pc.GetComponent<Rigidbody2D>().AddForce(chargeDirection * 5f, ForceMode2D.Impulse);
            
            StopCharge(false); // Hit player, no self-stun
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCharging && collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            // Hit a wall!
            StopCharge(true);
        }
    }

    private void StopCharge(bool crashedIntoWall)
    {
        isCharging = false;
        rb.linearVelocity = Vector2.zero;
        isAttackRoutineRunning = false;

        if (crashedIntoWall)
        {
            Debug.Log("Stag Crashed! Stunned.");
            ForceStun(stunOnCrash);
            StartCoroutine(AttackCooldownRoutine()); // CD starts after crash
        }
        else
        {
            ChangeState(EnemyState.Chasing);
            StartCoroutine(AttackCooldownRoutine());
        }
    }
}