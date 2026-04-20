using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using System;

public class CrystalStag : EnemyBase
{
    [Header("Stag Settings")]
    public bool isElite = false;
    
    [Tooltip("Speed in world units. (e.g. 8 tiles/sec = 0.8f)")]
    public float chargeSpeed = 2.4f; 
    public float stunOnCrash = 2.0f;
    
    private bool isCharging = false;
    private Vector2 chargeDirection;
    private Vector2 initialPosition;

    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        
        // Ensure knockback is set in stats so PerformAttack handles it automatically
        stats.knockbackForce = isElite ? 3.0f : 2.5f; 
        
        base.Start();
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 60f;
        stats.damage = 20f; // 2 Hearts charge
        chargeSpeed = 0.95f; // 9.5 tiles/sec
        stats.moveSpeed = 3.5f; 
        stats.attackCooldown = 4.5f;
        stunOnCrash = 1.5f;
    }

    protected override void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        float chargeTriggerDistance = 0.3f * 14f;

        if (dist <= chargeTriggerDistance && isAttackReady && !isCharging) // fix hard coded values later
        {
            // Raycast to check Line of Sight
            Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, LayerMask.GetMask("Environment"));
            
            if (hit.collider == null) // No walls in the way
            {
                ChangeState(EnemyState.Attacking);
                return;
            }
        }

        Vector2 chaseDirection = CalculateMovementDirection(dist);
        if (statusMgr.HasStatus(StatusType.Confusion))
        {
            chaseDirection = -chaseDirection;
        }

        MoveTowardsTarget(chaseDirection);
        HandleSpriteRotation(chaseDirection);
    }

    // ---------------------------------------------------------
    // THE NEW ATTACK PIPELINE
    // ---------------------------------------------------------

    protected override IEnumerator AttackWindup()
    {
        
        // 1. Lock-On
        chargeDirection = (playerTarget.position - transform.position).normalized;
        
        // Face player
        if (movementType == MovementType.Flip)
            spriteRenderer.flipX = chargeDirection.x > 0;
        else if (movementType == MovementType.Rotate)
        {
            float angle = Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        yield return new WaitForSeconds(stats.attackWindup);
    }

    protected override void ExecuteAttack()
    {
        initialPosition = transform.position;
        // 2. Execute Charge
        isCharging = true;
        rb.linearVelocity = chargeDirection * chargeSpeed;

        StartCoroutine(ChargeTimeoutRoutine());
    }
    protected override IEnumerator AttackRecovery()
    {
        ChangeState(EnemyState.Chasing); yield return null; 
    }

    private IEnumerator ChargeTimeoutRoutine()
    {
        float maxDistanceInWorld = 0.3f * 14f;
        float timer = 0f;
        // Loop runs while charging, but stops instantly if StopCharge() is called
        while (isCharging && Math.Abs(Vector2.Distance(transform.position, initialPosition)) < maxDistanceInWorld)
        {
            timer += Time.deltaTime;
            yield return null; 
        }

        // If we finished the timer and are STILL charging (didn't hit player or wall)
        if (isCharging) 
        {
            StopCharge(crashedIntoWall: false);
        }
    }

    // ---------------------------------------------------------
    // COLLISION LOGIC
    // ---------------------------------------------------------

    protected override void OnTriggerStay2D(Collider2D collision)
    {
        base.OnTriggerStay2D(collision); // Normal contact damage

        if (!isCharging) return;

        // Hit the player while charging
        if (collision.CompareTag("Player"))
        {
            PerformAttack(collision.gameObject);
            StopCharge(crashedIntoWall: false); 
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Hit a solid object on the Environment layer while charging
        if (isCharging && collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            StopCharge(crashedIntoWall: true); 
        }
    }

    private void StopCharge(bool crashedIntoWall)
    {
        isCharging = false;
        rb.linearVelocity = Vector2.zero;

        if (crashedIntoWall)
        {
            ForceStun(stunOnCrash); 
        }
        else
        {

        }
    }
}
