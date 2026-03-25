using UnityEngine;
using System.Collections;

public class CorruptedFairy : EnemyBase
{
    [Header("Fairy Settings")]
    public bool isElite = false;
    public GameObject magicBoltPrefab;
    
    private float blinkCooldown = 5f;
    private float lastBlinkTime = -999f;
    
    // Tile conversion
    private const float TILE = 0.3f;

    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        base.Start();
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 35f;
        stats.damage = 10f; // 1.0 heart
        stats.moveSpeed = 36f; // 3.6 units/sec * 10 (assuming world speed handling)
        stats.attackCooldown = 2.3f;
        blinkCooldown = 4f;
    }

    // ---------------------------------------------------------
    // CHASING (Closing the distance to 8 tiles)
    // ---------------------------------------------------------
    protected override void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        // 2. If in preferred range (5-8 tiles), switch to Follow (Orbiting)
        if (dist <= stats.preferredRangeMin * TILE)
        {
            ChangeState(EnemyState.Follow);
            return;
        }

        // 3. Otherwise, move closer
        Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;
        MoveTowardsTarget(dirToPlayer);
        HandleSpriteRotation(dirToPlayer);

        CheckAttack(dist);
    }

    // ---------------------------------------------------------
    // FOLLOW (Orbiting at 5-8 tiles)
    // ---------------------------------------------------------
    protected override void LogicFollow()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;

        // 1. Trigger Blink if too close
        if (dist <= 3f * TILE)
        {
            TryBlink();
            return;
        }

        // 2. If player got too far away, go back to Chasing
        if (dist > 8f * TILE)
        {
            ChangeState(EnemyState.Chasing);
            return;
        }

        // 3. Orbiting Movement (Strafe sideways)
        // Vector perpendicular to the player direction
        Vector2 orbitDir = new Vector2(-dirToPlayer.y, dirToPlayer.x);
        
        MoveTowardsTarget(orbitDir);
        
        // Always look at the player, not the orbit direction
        HandleSpriteRotation(dirToPlayer);

        CheckAttack(dist);
    }

    // ---------------------------------------------------------
    // EVASIVE BLINK TWIST
    // ---------------------------------------------------------
    private void TryBlink()
    {
        if (Time.time >= lastBlinkTime + blinkCooldown)
        {
            StartCoroutine(BlinkRoutine());
        }
        else
        {
            // If blink is on cooldown and player is close, just fly away
            Vector2 dirAway = (transform.position - playerTarget.position).normalized;
            MoveTowardsTarget(dirAway);
            HandleSpriteRotation(-dirAway);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        lastBlinkTime = Time.time;
        
        // Use the hook we added to EnemyBase to turn off hitbox
        SetUntargetable(true); 
        rb.linearVelocity = Vector2.zero;

        // Calculate blink destination (3 tiles away)
        Vector2 blinkDir = (transform.position - playerTarget.position).normalized;
        
        // Elite twist: pick a slight diagonal angle to be less predictable
        if (isElite) blinkDir = Quaternion.Euler(0, 0, Random.Range(-30f, 30f)) * blinkDir;

        Vector3 targetPos = transform.position + (Vector3)(blinkDir * (3f * TILE));

        // Visuals: Fade out
        spriteRenderer.color = new Color(1, 0, 1, 0.3f); // Purple ghostly hue

        // 0.15s i-frame window
        yield return new WaitForSeconds(0.15f);

        // Snap position
        transform.position = targetPos;
        
        // Restore
        spriteRenderer.color = Color.white;
        SetUntargetable(false);
    }

    // ---------------------------------------------------------
    // ATTACK PIPELINE
    // ---------------------------------------------------------
    private void CheckAttack(float dist)
    {
        if (dist <= 9f * TILE && isAttackReady && !isAttackRoutineRunning)
        {
            ChangeState(EnemyState.Attacking);
        }
    }

    protected override IEnumerator AttackWindup()
    {
        rb.linearVelocity *= 0.2f; // Slow down to cast, but don't full stop (hovering)

        
        if (anim != null) anim.SetTrigger("Attack"); // Play Cast Anim
        
        yield return new WaitForSeconds(stats.attackWindup);
    }

    protected override void ExecuteAttack()
    {
        if (playerTarget != null && magicBoltPrefab != null)
        {
            Vector2 aimDir = (playerTarget.position - transform.position).normalized;
            GameObject bolt = Instantiate(magicBoltPrefab, transform.position, Quaternion.identity);
            
            FairyBolt projectile = bolt.GetComponent<FairyBolt>();
            if (projectile != null)
            {
                // Stats: 7 units/sec (0.7f scaled)
                float confusionChance = isElite ? 0.35f : 0.25f;
                float confusionDur = isElite ? 3.0f : 2.5f;
                
                // Note: Ensure stats.damage is set to 7.5 (0.75 hearts) in Inspector
                projectile.Setup(aimDir, stats.projectileSpeed, stats.damage, confusionChance, confusionDur);
            }
        }
    }

    protected override IEnumerator AttackRecovery()
    {
        ChangeState(EnemyState.Follow); // Go back to follow/strafe after attacking
        yield return null;
    }
}