using UnityEngine;
using System.Collections;

public class ChainedSpecter : EnemyBase
{
    [Header("Specter Config")]
    public bool isElite = false;
    public GameObject hookProjectilePrefab;

    private const float TILE = 0.3f;
    private bool isYanking = false;

    [SerializeField] private float hookRange = 5f; // Base range in tiles

    private void Awake()
    {
        if (isElite)
        {
            stats.maxHealth = 35f;      // 3.5 Hearts
            stats.moveSpeed = 7.5f;     
            stats.damage = 10f;         // 1.0 Heart (Hook hit)
            stats.contactDamage = 7.5f; // 0.75 Hearts (Dash body touch)
            stats.attackCooldown = 1.7f;
            stats.attackWindup = 0.4f;
            stats.aggroRadius = 11f;
            stats.preferredRangeMin = 4f;
            stats.preferredRangeMax = 7f;
        }
    }

    protected override Vector2 CalculateMovementDirection(float distanceToPlayer)
    {
        if (isYanking) return Vector2.zero; // Don't allow normal movement while dashing

        Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;

        if (distanceToPlayer < stats.preferredRangeMin) return -dirToPlayer;
        if (distanceToPlayer > stats.preferredRangeMax) return dirToPlayer;
        
        return Vector2.zero; // Strafe/Orbit (Handled by base class rotation/flip)
    }

    protected override IEnumerator AttackWindup()
    {
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetTrigger("Attack");
        yield return new WaitForSeconds(stats.attackWindup);
    }

    protected override void ExecuteAttack()
    {
        if (playerTarget == null || hookProjectilePrefab == null) return;

        Vector2 aimDir = (playerTarget.position - transform.position).normalized;
        GameObject hook = Instantiate(hookProjectilePrefab, transform.position, Quaternion.identity);
        
        SpecterHook script = hook.GetComponent<SpecterHook>();
        if (script != null)
        {
            // Range: 7 tiles (Base), 8 tiles (Elite)
            float range = hookRange;
            // Travel time to max range ≈ 0.25–0.3 s. So speed = range / time
            float speed = range * TILE / 0.75f;
            
            script.Setup(this, aimDir, speed, stats.damage, range);
        }
    }

    // --- THE TWIST: YANK DASH ---
    // This is called publicly by the Hook Projectile when it hits a wall
    public void StartYankDash(Vector3 anchorPosition)
    {
        if (currentState == EnemyState.Dead) return;
        StartCoroutine(YankDashRoutine(anchorPosition));
    }

    private IEnumerator YankDashRoutine(Vector3 targetPos)
    {
        isYanking = true;
        isAttackRoutineRunning = true; // Keep state locked

        // Tether Delay: 0.25s Base, 0.2s Elite
        float delay = isElite ? 0.2f : 0.25f;
        yield return new WaitForSeconds(delay);

        // Calculate dash physics
        Vector3 startPos = transform.position;
        float dashTime = isElite ? 0.2f : 0.25f; // Fast snap
        float elapsedTime = 0f;

        // Visuals: Ghost trail or stretch effect could go here

        // LERP position for the dash
        while (elapsedTime < dashTime && currentState != EnemyState.Dead)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / dashTime);
            elapsedTime += Time.deltaTime;
            
            // While dashing, check for player collision manually since we are overriding rb.velocity
            CheckDashCollision();
            
            yield return null;
        }

        // Snap to final position
        transform.position = targetPos;

        // Post-Yank Recovery
        float recovery = isElite ? 0.3f : 0.4f;
        yield return new WaitForSeconds(recovery);

        isYanking = false;
        
        // Finish attack sequence properly
        StartCoroutine(AttackCooldownRoutine());
        isAttackRoutineRunning = false;
        ChangeState(EnemyState.Chasing);
    }

    private void CheckDashCollision()
    {
        // GDD: "If its body passes through the player, player takes contact damage"
        // Since we Lerp position rapidly, OnTriggerStay2D might miss the player. 
        // We do a manual overlap circle during the dash.
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.5f, LayerMask.GetMask("Player"));
        if (hit != null)
        {
            // Use contact damage value (0.5 hearts)
            DamageInfo info = new DamageInfo(stats.contactDamage, DamageElement.Physical, AttackStyle.MeleeLight, transform.position, 0f);
            hit.GetComponent<PlayerController>()?.ReceiveDamage(info);
            // Player i-frames prevent this hitting 60 times a second
        }
    }
}