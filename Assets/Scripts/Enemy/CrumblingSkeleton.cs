using UnityEngine;
using System.Collections;

public class CrumblingSkeleton : EnemyBase
{
    [Header("Skeleton Settings")]
    public bool isElite = false;
    
    // --- Barrel / Fuse State ---
    private enum FuseStage { NONE, SMALL, BIG }
    private FuseStage currentFuseStage = FuseStage.NONE;
    private float fuseTimer = 0f;
    private bool isFuseLit = false;

    // --- Explosions (Assigned in Inspector) ---
    [Tooltip("Prefab for Phase 2 explosion (1.5 tiles)")]
    public GameObject smallExplosionPrefab;
    [Tooltip("Prefab for Phase 3/Auto explosion (2.5 tiles inner/outer)")]
    public GameObject bigExplosionPrefab;

    // --- Tile Conversion ---
    private const float TILE = 0.3f; // 1 Tile = 0.3 Units

    private void Awake()
    {

        if (isElite){
            stats.maxHealth = 25f;      // 2.5 Hearts
            stats.moveSpeed = 9.0f;
            stats.contactDamage = 7.5f; // 0.75 Hearts
            stats.aggroRadius = 9f;       
        }
    }

    // ---------------------------------------------------------
    // THE FUSE TIMER (Runs every frame regardless of state)
    // ---------------------------------------------------------

    protected override void ExecuteAttack()
    {
        // GDD: "When the player gets within 4 tiles of the Spore, it starts a fuse."
        if (!isFuseLit)
        {
            isFuseLit = true;
            fuseTimer = 0f;
            currentFuseStage = FuseStage.NONE;
            Debug.Log("<color=green>Skeleton Fuse Lit!</color>");
        }
    }

    protected override IEnumerator AttackRecovery()
    {
        ChangeState(EnemyState.Chasing); yield return null; 
    }
    
    // ---------------------------------------------------------
    // DEATH & DETONATION
    // ---------------------------------------------------------
    protected override void OnDeath()
    {
        // GDD: "If the player kills the Spore while in Phase 1 (0.0-1.2s)... No explosion."
        // If killed in Phase 2 or 3, detonate immediately.
        
        if (currentFuseStage != FuseStage.NONE)
        {
            Detonate(currentFuseStage);
        }
        else
        {
            Debug.Log("Skeleton safely disarmed!");
        }
    }


    private void Detonate(FuseStage stage)
    {
        Debug.Log($"Skeleton Exploded! Stage: {stage}");

        if (stage == FuseStage.SMALL)
        {
            float radius = (isElite ? 1.7f : 1.5f) * TILE;
            
            ExplodeHitbox(radius, stats.damage, radius, stats.damage);
            
            if (smallExplosionPrefab != null) 
                Instantiate(smallExplosionPrefab, transform.position, Quaternion.identity);
            else 
                DrawExplosionLine(radius, Color.yellow); // Fallback visual
        }
        else if (stage == FuseStage.BIG)
        {
            float innerDmg = stats.damage;
            float outerDmg = stats.damage * 0.5f;
            
            // FIX: Added * TILE to your previous code here!
            float innerRadius = (isElite ? 1.7f : 1.5f) * TILE;
            float outerRadius = (isElite ? 2.7f : 2.5f) * TILE;

            ExplodeHitbox(innerRadius, innerDmg, outerRadius, outerDmg);
            
            if (bigExplosionPrefab != null) 
                Instantiate(bigExplosionPrefab, transform.position, Quaternion.identity);
            else 
                DrawExplosionLine(outerRadius, Color.red); // Fallback visual
        }

        // Force Death so we despawn
        if (currentState != EnemyState.Dead)
        {
            currentHealth = 0; 
            Die(); 
        }
    }

    private void ExplodeHitbox(float innerRadius, float innerDamage, float outerRadius, float outerDamage)
    {
        // 1. Check Outer Radius first
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, outerRadius);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                
                // 2. Determine if player is in inner or outer ring
                float dmgToApply = (dist <= innerRadius) ? innerDamage : outerDamage;

                DamageInfo info = new DamageInfo(dmgToApply, DamageElement.Fire, AttackStyle.Environment, transform.position, 0f);
                hit.GetComponent<PlayerController>()?.ReceiveDamage(info);
                
                // GDD: Single hit, respects i-frames. 
                // Because we only check the player once per explosion, we don't need complex debounce logic here.
                break; // Stop checking after we hit the player once
            }
        }
    }

    protected override void CustomUpdateLogic()
    {
        if (currentState == EnemyState.Dead || playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) 
            return;

        // Progress the Fuse
        if (isFuseLit)
        {
            fuseTimer += Time.deltaTime;

            // Phase 2: Small Fuse (1.2s - 2.4s)
            if (fuseTimer >= 1.2f && currentFuseStage == FuseStage.NONE)
            {
                currentFuseStage = FuseStage.SMALL;
                if (anim != null) anim.SetTrigger("Attack"); // GDD: Switches to charge pose
                Debug.Log("<color=yellow>Phase 2: Small Bomb Ready</color>");
                // Optional: Enable a spark particle effect on the barrel
            }
            // Phase 3: Big Fuse (2.4s - 3.6s)
            else if (fuseTimer >= 2.4f && currentFuseStage == FuseStage.SMALL)
            {
                currentFuseStage = FuseStage.BIG;
                Debug.Log("<color=red>Phase 3: Big Bomb Ready</color>");
                // Optional: Increase spark intensity
            }
            // Auto Detonation (3.6s)
            else if (fuseTimer >= 3.6f)
            {
                // BOOM
                Detonate(FuseStage.BIG);
            }
        }
    }

    private void DrawExplosionLine(float radius, Color color)
    {
        // 1. Create a temporary GameObject
        GameObject lineObj = new GameObject("ExplosionVisual");
        lineObj.transform.position = transform.position;
        
        // 2. Setup LineRenderer
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = new Color(color.r, color.g, color.b, 0f); // Fade to transparent

        // 3. Calculate Circle Points
        int segments = 36;
        lr.positionCount = segments;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float currentAngleRad = (angleStep * i) * Mathf.Deg2Rad;
            float x = Mathf.Cos(currentAngleRad) * radius;
            float y = Mathf.Sin(currentAngleRad) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }

        // 4. Destroy after 0.2s (Matching the Hitbox duration in GDD)
        Destroy(lineObj, 0.2f);
    }
}