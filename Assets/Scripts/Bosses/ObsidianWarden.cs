using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObsidianWarden : BossBase
{
    [Header("Warden Settings")]
    public GameObject crystalSpikePrefab;

    // --- Timings & Ranges (Converted to TILE internally) ---
    private float slamRadius = 2.2f;
    private float slamDamage = 12.5f; // 1.25 hearts
    private float slamTelegraph = 0.85f;
    private float slamCooldown = 8.5f;
    
    private float pulseRadius = 3.0f;
    private float pulseDamage = 7.5f; // 0.75 hearts
    private float pulseTelegraph = 0.6f;
    private float pulseCooldown = 6.0f;
    
    // --- Turn Rate & Facing ---
    [Tooltip("Degrees per second. Low value prevents instant snapping to player.")]
    [SerializeField] private float maxTurnSpeed = 90f; 
    
    private Vector2 currentFacingDir = Vector2.down; // Starting direction
    private bool isFacingLocked = false;
    
    // --- Back Arc Tracker ---
    private float timePlayerInBackArc = 0f;
    private float lastSlamTime = -999f;
    private float lastPulseTime = -999f;
    
    private const float TILE = 0.3f;

    private void Awake()
    {
        bossName = "Obsidian Warden";
    }

    // ---------------------------------------------------------
    // CUSTOM ROTATION & FACING (THE TWIST)
    // ---------------------------------------------------------
    protected override void HandleSpriteRotation(Vector2 direction)
    {
        if (isFacingLocked || direction == Vector2.zero) return;

        // Slow turn rate logic
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float currentAngle = Mathf.Atan2(currentFacingDir.y, currentFacingDir.x) * Mathf.Rad2Deg;

        // Smoothly rotate current angle towards target angle
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxTurnSpeed * Time.deltaTime);
        
        currentFacingDir = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad));

        // Apply visual rotation
        if (movementType == MovementType.Rotate)
        {
            transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);
        }
        else if (movementType == MovementType.Flip)
        {
            spriteRenderer.flipX = currentFacingDir.x > 0;
        }
    }

    // ---------------------------------------------------------
    // SHIELD & WEAKSPOT LOGIC (DAMAGE MODIFIER)
    // ---------------------------------------------------------
    protected override float ModifyIncomingDamage(float currentCalculatedDamage, DamageInfo dmg)
    {
        if (currentState == EnemyState.Dead) return currentCalculatedDamage;

        Vector2 damageDir = (dmg.sourcePosition - (Vector2)transform.position).normalized;
        float angle = Vector2.Angle(currentFacingDir, damageDir);

        // FRONT ARC (120 degrees -> 60 on each side)
        if (angle <= 60f)
        {
            // Magic/Ranged Reflection Logic
            if (dmg.style == AttackStyle.Ranged)
            {
                Debug.Log("<color=magenta>[Warden] Reflected Magic Attack with Crystal Shield!</color>");
                // GDD: Reflect or nullify. We choose Nullify (0 damage).
                // If you want reflect, you must spawn a new player-hostile projectile here.
                return 0f; 
            }
            
            // Melee Frontal Armor (-70% damage -> takes 30%)
            Debug.Log("[Warden] Frontal Armor block.");
            return currentCalculatedDamage * 0.3f;
        }
        
        // BACK ARC (120 degrees -> > 120 from front)
        if (angle >= 120f)
        {
            // Meteor Core Exposed (+40% damage taken)
            Debug.Log("<color=red>[Warden] Weakspot Hit! (Back Core)</color>");
            
            // Optional visual flair for hitting core
            StartCoroutine(statusMgr.FlashSpriteRoutine(DamageElement.Poison)); 
            
            return currentCalculatedDamage * 1.4f; 
        }

        // SIDES (Normal damage)
        return currentCalculatedDamage;
    }

    // ---------------------------------------------------------
    // AI PRIORITY & ATTACK SELECTION
    // ---------------------------------------------------------
    protected override void CustomUpdateLogic()
    {
        if (playerTarget == null || currentState == EnemyState.Dead) return;

        // Track how long player is behind us
        Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;
        float angle = Vector2.Angle(currentFacingDir, dirToPlayer);

        if (angle >= 120f) timePlayerInBackArc += Time.deltaTime;
        else timePlayerInBackArc = 0f; // Reset if they leave the back arc
    }

    protected override string ChooseNextAttack(float distanceToPlayer)
    {
        // Priority 1: Back-Core Pulse
        // Trigger: player in back arc > 1.2s AND cooldown ready
        if (timePlayerInBackArc >= 1.2f && Time.time >= lastPulseTime + pulseCooldown)
        {
            return "BackCorePulse";
        }

        // Priority 2: Obsidian Slam
        // Trigger: Player in preferred range (or closer) AND cooldown ready
        if (distanceToPlayer <= stats.preferredRangeMax && Time.time >= lastSlamTime + slamCooldown)
        {
            return "ObsidianSlam";
        }

        return "None";
    }

    // ---------------------------------------------------------
    // ATTACK EXECUTION
    // ---------------------------------------------------------
    protected override IEnumerator BossAttackWindup(string attackName)
    {
        rb.linearVelocity = Vector2.zero; // Stop moving
        
        if (attackName == "ObsidianSlam")
        {
            isFacingLocked = true; // Lock facing during Slam
            if (anim != null) anim.SetTrigger("Slam");
            
            // Telegraph: cracked-circle decal (Draw telegraph line/sprite here)
            yield return new WaitForSeconds(slamTelegraph); // 0.85s
        }
        else if (attackName == "BackCorePulse")
        {
            isFacingLocked = true;
            if (anim != null) anim.SetTrigger("Pulse");
            
            // Telegraph: core glow building up
            spriteRenderer.color = Color.magenta;
            yield return new WaitForSeconds(pulseTelegraph); // 0.6s
            spriteRenderer.color = Color.white;
        }
    }

    protected override void ExecuteBossAttack(string attackName)
    {
        if (attackName == "ObsidianSlam")
        {
            // 1. Slam Impact
            float radius = slamRadius * TILE;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    DamageInfo info = new DamageInfo(slamDamage, DamageElement.Physical, AttackStyle.MeleeHeavy, transform.position, 0f);
                    hit.GetComponent<PlayerController>()?.ReceiveDamage(info);
                    Debug.Log("<color=orange>[Warden] Slam hit the player!</color>");
                }
            }

            // 2. Crystal Spike Bloom
            if (crystalSpikePrefab != null)
            {
                SpawnSpikeRing(radius);
            }
            
            lastSlamTime = Time.time;
        }
        else if (attackName == "BackCorePulse")
        {
            float radius = pulseRadius * TILE;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    DamageInfo info = new DamageInfo(pulseDamage, DamageElement.Magic, AttackStyle.Environment, transform.position, 20f); // 2.0 tiles knockback approx
                    hit.GetComponent<PlayerController>()?.ReceiveDamage(info);
                }
            }
            
            lastPulseTime = Time.time;
            timePlayerInBackArc = 0f; // Reset tracker
        }
    }

    protected override IEnumerator BossAttackRecovery(string attackName)
    {
        if (attackName == "ObsidianSlam")
        {
            // Facing locked during Slam + 0.4s after
            yield return new WaitForSeconds(0.4f);
        }
        else
        {
            yield return new WaitForSeconds(0.2f); // Short recovery for pulse
        }

        isFacingLocked = false;
    }

    // ---------------------------------------------------------
    // SPIKE BLOOM HAZARD
    // ---------------------------------------------------------
    private void SpawnSpikeRing(float radius)
    {
        int spikeCount = 8; // "8 spikes ring"
        float angleStep = 360f / spikeCount;

        for (int i = 0; i < spikeCount; i++)
        {
            float angleRad = (angleStep * i) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;
            Vector3 spawnPos = transform.position + (Vector3)offset;

            GameObject spike = Instantiate(crystalSpikePrefab, spawnPos, Quaternion.identity);
            
            // Assuming you have a generic hazard script on the prefab
            HazardSpike spikeScript = spike.GetComponent<HazardSpike>();
            if (spikeScript != null)
            {
                // duration 3.5s, spike contact 0.75 hearts
                spikeScript.Setup(7.5f, 3.5f); 
            }
        }
    }
}