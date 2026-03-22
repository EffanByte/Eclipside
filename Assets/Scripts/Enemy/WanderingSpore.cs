using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class WanderingSpore : EnemyBase
{
    [Header("Spore Settings")]
    public bool isElite = false;

    [Header("Visuals")]
    [Tooltip("Color when at full HP")]
    [SerializeField] private Color healthyColor = Color.white;
    [Tooltip("Color when near death (e.g. gray, rotten, or flashing red)")]
    [SerializeField] private Color nearDeathColor = new Color(0.2f, 0.2f, 0.2f, 1f); 

    // --- Timers & Stats ---
    private float chargeTime = 0.6f;
    private float aggroTimer = 0f;
    private bool isExploding = false;
    
    // --- Tile Conversion (Assuming 1 Tile = 0.1f) ---
    private const float TILE = 0.3f;
    [SerializeField] private float maxRadius = 4.0f * TILE;
    [SerializeField] private float fullDamageRadius = 3f * TILE;
    [SerializeField] GameObject poisonCloudPrefab;

    private LineRenderer telegraphLine;

    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        base.Start();

        telegraphLine = GetComponent<LineRenderer>();
        telegraphLine.positionCount = 0; // Hide initially
        telegraphLine.useWorldSpace = false;
        telegraphLine.loop = true;
        
        // Ensure LineRenderer is styled
        telegraphLine.startWidth = 0.02f;
        telegraphLine.endWidth = 0.02f;
        telegraphLine.startColor = new Color(0.5f, 1f, 0.2f, 0.5f); // Toxic Green
        telegraphLine.endColor = new Color(0.5f, 1f, 0.2f, 0.5f);
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 30f;
        stats.moveSpeed = 26f; // 2.6 * 10
        chargeTime = 0.5f;
    }

    // ---------------------------------------------------------
    // HEALTH COLOR SHIFT
    // ---------------------------------------------------------
    public override void ReceiveDamage(DamageInfo dmg)
    {
        base.ReceiveDamage(dmg);

        // Update color based on health percentage
        if (currentState != EnemyState.Dead)
        {
            float healthPercent = currentHealth / stats.maxHealth;
            spriteRenderer.color = Color.Lerp(nearDeathColor, healthyColor, healthPercent);
        }
    }

    // ---------------------------------------------------------
    // CHASING & TRIGGER LOGIC
    // ---------------------------------------------------------
    protected override void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        
        // Track how long it has been chasing
        aggroTimer += Time.deltaTime;
        float autoDetonateTime = isElite ? 9f : 10f;

        // 1. Check if we should start the explosive charge
        if ((dist <= stats.attackRange|| aggroTimer >= autoDetonateTime) && !isExploding)
        {
            ChangeState(EnemyState.Attacking);
            return;
        }

        // 2. Normal Movement (Float towards player)
        Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;
        MoveTowardsTarget(dirToPlayer);
        HandleSpriteRotation(dirToPlayer);
    }

    // ---------------------------------------------------------
    // ATTACK SEQUENCE (THE CHARGE)
    // ---------------------------------------------------------

    protected override IEnumerator AttackWindup()
    {
        // 1. Stop moving and start telegraphing
        rb.linearVelocity = Vector2.zero;
        DrawTelegraphCircle(maxRadius);
        StartCoroutine(statusMgr.FlashSpriteRoutine(DamageElement.Poison));
        // 2. Wait for charge time
        float timer = 0f;
        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

    }
    protected override void ExecuteAttack()
    {
        // 1. Stop moving and start telegraphing
        rb.linearVelocity = Vector2.zero;
        DrawTelegraphCircle(maxRadius);

        // 2. After charge time, execute explosion logic
        ExecuteExplosion();
    }

    private void DrawTelegraphCircle(float radius)
    {
        int segments = 36;
        telegraphLine.positionCount = segments;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float currentAngleRad = (angleStep * i) * Mathf.Deg2Rad;
            float x = Mathf.Cos(currentAngleRad) * radius;
            float y = Mathf.Sin(currentAngleRad) * radius;
            telegraphLine.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    // ---------------------------------------------------------
    // EXPLOSION & CHAIN REACTION LOGIC
    // ---------------------------------------------------------

    // Public method so OTHER spores can trigger this one instantly
    public void ForceDetonate()
    {
        if (!isExploding && currentState != EnemyState.Dead)
        {
            isExploding = true;
            ExecuteExplosion(); // Bypasses the 0.6s charge!
        }
    }

    private void ExecuteExplosion()
    {
        // 1. Find everything in blast radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, maxRadius);

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            bool isFullDamage = dist <= fullDamageRadius;

            // --- HIT PLAYER ---
            if (hit.CompareTag("Player"))
            {
                float dmg = isElite 
                    ? (isFullDamage ? 20f : 12.5f)  // Elite: 2.0 / 1.25 hearts
                    : (isFullDamage ? 15f : 10f);   // Base: 1.5 / 1.0 hearts
                
                DamageInfo info = new DamageInfo(dmg, DamageElement.Physical, AttackStyle.Environment, transform.position, stats.knockbackForce);
                hit.GetComponent<PlayerController>()?.ReceiveDamage(info);
            }
            // --- HIT ENEMIES ---
            else if (hit.CompareTag("Enemy") && hit.gameObject != gameObject)
            {
                EnemyBase otherEnemy = hit.GetComponent<EnemyBase>();
                
                // TWIST: CHAIN REACTION
                if (otherEnemy is WanderingSpore otherSpore)
                {
                    otherSpore.ForceDetonate(); // Instant pop!
                }
                else if (otherEnemy != null)
                {
                    // -20% Damage vs normal enemies
                    float dmg = isElite 
                        ? (isFullDamage ? 16f : 10f)  
                        : (isFullDamage ? 12f : 8f);   
                    
                    DamageInfo info = new DamageInfo(dmg, DamageElement.Physical, AttackStyle.Environment, transform.position, 0f);
                    otherEnemy.ReceiveDamage(info);
                }
            }
        }

        // 2. Spawn Toxic Cloud
        if (poisonCloudPrefab != null)
        {
            GameObject poisonCloud = Instantiate(poisonCloudPrefab, transform.position, Quaternion.identity);
            poisonCloud.GetComponent<ToxicCloud>().Setup(isElite);
        }

        // 3. Destroy Self (Bypass normal death logic so it doesn't drop loot twice)
        currentState = EnemyState.Dead;
        Die();
    }
}