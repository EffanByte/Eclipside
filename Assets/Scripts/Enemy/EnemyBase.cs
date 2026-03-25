using System.Collections;
using UnityEngine;
using System;
using TMPro;
public enum EnemyState
{
    Idle, Chasing, Follow, Attacking, Stunned, Dead
}

public enum DamageElement
{
    Fire, Poison, Magic, Physical, Ice, Psychic, True
}

public enum AttackStyle
{
    MeleeLight, MeleeHeavy, Ranged, Environment
}

public enum MovementType
{
    Flip, Rotate
}

[Serializable]
public struct DamageInfo
{
    public float amount;
    public DamageElement element;
    public AttackStyle style;
    public Vector2 sourcePosition;
    public float knockbackForce;
    public bool isCritical;

    public DamageInfo(float amount, DamageElement element, AttackStyle style, Vector2 sourcePosition, float knockbackForce = 0f, bool isCritical = false)
    {
        this.amount = amount;
        this.element = element;
        this.style = style;
        this.sourcePosition = sourcePosition;
        this.knockbackForce = knockbackForce;
        this.isCritical = isCritical;
    }
}

// Simple struct to act as a Dictionary Key
public struct SynergyPair : IEquatable<SynergyPair>
{
    public readonly StatusType First;
    public readonly StatusType Second;

    public SynergyPair(StatusType a, StatusType b)
    {
        // Always sort them so (Fire, Ice) is the same key as (Ice, Fire)
        if (a < b) { First = a; Second = b; }
        else       { First = b; Second = a; }
    }

    // make it work as a Dictionary Key
    public bool Equals(SynergyPair other) => First == other.First && Second == other.Second;
    public override int GetHashCode() => HashCode.Combine(First, Second);
}

[System.Serializable]
public class EnemyStats
{
    [Header("General Stats")]
    public float maxHealth;
    public float moveSpeed;
    public static float expReward = 5;
    public int rupeeReward;
    
    [Header("Contact Damage")]
    public bool dealsContactDamage = true;
    public float contactDamage = 5f; 
    public float contactCooldown = 1f;

    [Header("Attack Configuration")]
    public float damage;        
    public float attackCooldown;
    public float knockbackForce; 
    public float projectileSpeed;
    public float attackWindup;
    
    public DamageElement attackElement;
    public AttackStyle attackStyle;

    [Header("AI Vision & Ranges")]   
    public float attackRange = 1.5f; 
    public float aggroRadius = 10f;
    [Tooltip("If distance is less than Min, enemy runs away. If greater than Max, enemy approaches.")]
    public float preferredRangeMin = 0f; // Useful for Ranged enemies (Fairy, Spider)
    public float preferredRangeMax = 1.5f;
    
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour
{
    protected StatusManager statusMgr;
    private TextMeshProUGUI textbox;

    [Header("Configuration")]
    [SerializeField] protected EnemyStats stats;
    
    [Header("Runtime State")]
    public EnemyState currentState = EnemyState.Idle;
    protected float currentHealth;
    [Header("Dynamic Buffs (Handled by Auras)")]
    public float outgoingDamageMultiplier = 1.0f;
    public float bonusStatusChance = 0f; // Used to boost burn/freeze chance via Swamp Woman / Colossus
    
    public bool isAttackReady = true;
    public bool isAttackRoutineRunning = false; 
    protected bool isContactReady = true; 
    protected bool isInvulnerable = false; // NEW: For Fairy Blinks

    protected Transform playerTarget;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator anim; 
    protected MovementType movementType;

    // Status Modifiers
    protected float speedMultiplier = 1.0f; 
    protected float originalSpeedMultiplier;      
    protected float attackSpeedMultiplier = 1.0f;
    private float selfAttackMaxHpPercent = 0f;        
    private Coroutine temporaryMoveSpeedRoutine;
    private Coroutine temporaryAttackSpeedRoutine;

    public static event Action<EnemyBase> OnEnemyKilled;
    protected Collider2D mainCollider;      

    // --- WANDER STATE TRACKING ---
    protected float idleTimer = 0f;
    protected bool isWandering = false;
    protected Vector2 wanderDirection;
    
    [Header("Wander Settings")]
    [Tooltip("How long the enemy stands still before picking a new direction.")]
    private float idleWaitTimeMin = 1.0f;
    private float idleWaitTimeMax = 3.0f;
    
    [Tooltip("How long the enemy walks in a chosen direction.")]
    private float wanderTimeMin = 1.0f;
    private float wanderTimeMax = 2.0f;
    
    [Tooltip("Speed multiplier while wandering (usually slower than chasing).")]
    private float wanderSpeedMultiplier = 0.5f;

    protected virtual void Start()
    {   
        mainCollider = GetComponent<Collider2D>();
        textbox = FindFirstObjectByType<TextMeshProUGUI>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        originalSpeedMultiplier = speedMultiplier;
        currentHealth = stats.maxHealth;
        
        statusMgr = GetComponent<StatusManager>();
        statusMgr.Initialize(rb, this, StatusDamage, spriteRenderer);
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;
        
    }

   protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) Die();

        switch (currentState)
        {
            case EnemyState.Idle: LogicIdle(); break;
            case EnemyState.Chasing: LogicChasing(); break;
            
            // --- NEW STATE ADDED HERE ---
            case EnemyState.Follow: LogicFollow(); break; 
            
            case EnemyState.Attacking: 
                if (!isAttackRoutineRunning) LogicAttacking(); 
                break;
            case EnemyState.Stunned: LogicStunned(); break;
        }

        // NEW: Always run passive logic (Auras, Fuse Timers)
        CustomUpdateLogic();

        if (transform.position.y < -30f) Die();
    }


     protected virtual void CustomUpdateLogic() { }

    // ---------------------------------------------------------
    // DAMAGE RECEIVING & MODIFICATION
    // ---------------------------------------------------------
    public virtual void ReceiveDamage(DamageInfo dmg)
    {
        if (isInvulnerable) return;

        // 1. Calculate Status Multipliers
        float finalDamage = dmg.amount * statusMgr.DamageTakenMultiplier;

        // 2. NEW: Allow child classes to modify damage (Frontal Armor, Overheat Weakness)
        finalDamage = ModifyIncomingDamage(finalDamage, dmg);

        currentHealth -= finalDamage;

        // 3. Status Application
        if (dmg.element != DamageElement.True && dmg.element != DamageElement.Physical)
        {
            StatusType effect = statusMgr.GetStatusFromElement(dmg.element);
            statusMgr.TryAddStatus(effect);
        }

        // 4. Knockback
        if (rb != null && dmg.knockbackForce > 0)
        {
            Vector2 knockbackDir = (rb.position - dmg.sourcePosition).normalized;
            rb.AddForce(knockbackDir * dmg.knockbackForce, ForceMode2D.Impulse);
            StartCoroutine(KnockbackRoutine(0.2f));
        }

        StartCoroutine(statusMgr.FlashSpriteRoutine(dmg.element)); 
        
        if (currentHealth <= 0f) Die();
    }

    // NEW: Virtual hook for Bark Guardian's shield or Crystal Eye's overheat
    protected virtual float ModifyIncomingDamage(float currentCalculatedDamage, DamageInfo dmg)
    {
        return currentCalculatedDamage; // Base does nothing
    }

    // ---------------------------------------------------------
    // DIFFICULTY SCALING (Called by WaveManager on Spawn)
    // ---------------------------------------------------------
    public virtual void ApplyDifficultyScaling(float difficultyMultiplier)
    {
        // 1. Scale Health
        float oldMax = stats.maxHealth;
        stats.maxHealth *= difficultyMultiplier;
        currentHealth = stats.maxHealth; // Heal them to full new max

        // 2. Scale Damage (Optional, but usually standard in Roguelikes)
        stats.damage *= difficultyMultiplier;

        // Note: You usually don't scale Move Speed, as it breaks animations and game feel.

        // Debug.Log($"{name} Scaled by {difficultyMultiplier}x. HP: {oldMax} -> {stats.maxHealth}");
    }

    public void ForceStun(float duration)
{
    // Stop any running attacks
    StopAllCoroutines(); 
    isAttackRoutineRunning = false;
    
    StartCoroutine(KnockbackRoutine(duration)); // Reusing your stun routine
}

    public virtual void StatusDamage(DamageInfo dmg)
    {
        if (isInvulnerable) return;

        float finalDamage = dmg.amount * statusMgr.DamageTakenMultiplier;
        currentHealth -= finalDamage;
        
        StartCoroutine(statusMgr.FlashSpriteRoutine(dmg.element)); 
        if (currentHealth <= 0f) Die();
    }

    // ---------------------------------------------------------
    // MOVEMENT & CHASING
    // ---------------------------------------------------------
    protected virtual void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= stats.attackRange && isAttackReady)
        {
            ChangeState(EnemyState.Attacking);
            return; 
        }

        // 2. NEW: Calculate Kiting / Approach Direction
        Vector2 direction = CalculateMovementDirection(distanceToPlayer);
        
        if(statusMgr.HasStatus(StatusType.Confusion)) direction = -direction;

        MoveTowardsTarget(direction);
        HandleSpriteRotation(direction);
    }

    // NEW: Separated to allow Spiders/Fairies to back away if too close
    protected virtual Vector2 CalculateMovementDirection(float distanceToPlayer)
    {
        return (playerTarget.position - transform.position).normalized;
    }

        protected virtual void MoveTowardsTarget(Vector2 direction)
    {
        // Include any aura speed buffs in the calculation
        float finalSpeed = stats.moveSpeed * speedMultiplier;
        rb.linearVelocity = direction * finalSpeed;
    }

    protected virtual void HandleSpriteRotation(Vector2 direction)
    {
        if (direction == Vector2.zero) return;

        if (movementType == MovementType.Flip)
            spriteRenderer.flipX = direction.x > 0; 
        else if (movementType == MovementType.Rotate)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

        protected virtual void LogicFollow()
    {
        LogicChasing();
    }

    // ---------------------------------------------------------
    // ATTACK PIPELINE
    // ---------------------------------------------------------
    protected virtual void LogicAttacking() 
    { 
        StartCoroutine(AttackSequence()); 
    }

    protected virtual IEnumerator AttackSequence()
    {
        isAttackRoutineRunning = true;
        rb.linearVelocity = Vector2.zero; 

        // 1. Windup
        yield return StartCoroutine(AttackWindup());

        // 2. Execution (Melee, Ranged, Dash)
        if (!statusMgr.HasStatus(StatusType.Freeze))
        {
            ExecuteAttack();
        }

        // 3. Recovery & Cooldown
        yield return StartCoroutine(AttackRecovery());
        
        StartCoroutine(AttackCooldownRoutine());
        
        isAttackRoutineRunning = false;
    }

    // Virtual hooks for easy overriding in Ranged vs Melee enemies
    protected virtual IEnumerator AttackWindup() { yield return new WaitForSeconds(stats.attackWindup); }

    protected virtual void ExecuteAttack()
    {
        // Default Melee Behavior
        Collider2D hit = Physics2D.OverlapCircle(transform.position, stats.attackRange, LayerMask.GetMask("Player"));
        if (hit != null) PerformAttack(hit.gameObject);
    }

    protected virtual IEnumerator AttackRecovery()
    {
        ChangeState(EnemyState.Chasing); yield return null; 
    }

    // Change from 'public virtual void' to 'public virtual bool'
    public virtual bool PerformAttack(GameObject target)
    {
        if (selfAttackMaxHpPercent > 0)
        {
            currentHealth -= stats.maxHealth * selfAttackMaxHpPercent;
        }

        PlayerController damageable = target.GetComponent<PlayerController>();
        if (damageable != null)
        {
            DamageInfo info = new DamageInfo(
                amount: stats.damage * outgoingDamageMultiplier, // See Point 2
                element: stats.attackElement,  
                style: stats.attackStyle,      
                sourcePosition: transform.position,
                knockbackForce: stats.knockbackForce
            );
            
            // If the player is in i-frames (dashing), they avoid the damage.
            // You need PlayerController.ReceiveDamage to return a bool, OR 
            // check the player's dash state here.
            if (!damageable.isDashing) 
            {
                damageable.ReceiveDamage(info);
                return true; // HIT CONFIRMED!
            }
        }
        return false; // MISSED OR I-FRAMED!
    }

    protected IEnumerator AttackCooldownRoutine()
    {
        isAttackReady = false; 
        yield return new WaitForSeconds(stats.attackCooldown / Mathf.Max(0.01f, attackSpeedMultiplier)); 
        isAttackReady = true; 
    }

    // ---------------------------------------------------------
    // DEATH & LIFECYCLE
    // ---------------------------------------------------------
    protected void Die()
    {
        currentState = EnemyState.Dead;
        
        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        // NEW: Call the pre-death hook (For slime puddles / spore explosions)
        OnDeath();

        OnEnemyKilled?.Invoke(this);
        
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.AddCurrency(CurrencyType.Rupee, stats.rupeeReward);
            PlayerController.Instance.AddExperience(EnemyStats.expReward);
        }
        Destroy(gameObject);
    }

    // NEW: Virtual hook for dying actions
    protected virtual void OnDeath() 
    { 
        // Override in CorrodedSlime to spawn toxic puddle
    }

    // Call this to dive underground or become a phantom
    public virtual void SetUntargetable(bool isUntargetable)
    {
        isInvulnerable = isUntargetable; // Stops ReceiveDamage
        
        if (mainCollider != null) 
            mainCollider.enabled = !isUntargetable; // Prevents weapon hitboxes from touching it

        // Optional: Fade sprite alpha
        Color c = spriteRenderer.color;
        c.a = isUntargetable ? 0.3f : 1f; // Or 0f if completely invisible
        spriteRenderer.color = c;
    }

    // ---------------------------------------------------------
    // UTILS
    // ---------------------------------------------------------
    protected void ChangeState(EnemyState newState) { currentState = newState;
    ; }
protected virtual void LogicIdle() 
    {
        // 1. AGGRO CHECK (High Priority)
        // If player is in range, immediately drop everything and chase.
        if (playerTarget != null && Vector2.Distance(transform.position, playerTarget.position) <= stats.aggroRadius)
        {
            ChangeState(EnemyState.Chasing);
            return;
        }

        // 2. WANDER TIMER LOGIC
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            // Time to switch states!
            if (isWandering)
            {
                // We were walking. Time to stop and stand still.
                isWandering = false;
                rb.linearVelocity = Vector2.zero; // Stop moving
                
                // Pick a random time to stand still
                idleTimer = UnityEngine.Random.Range(idleWaitTimeMin, idleWaitTimeMax);
            }
            else
            {
                // We were standing still. Time to pick a direction and walk.
                isWandering = true;
                
                // Pick a random direction (Normalized vector)
                wanderDirection = UnityEngine.Random.insideUnitCircle.normalized;
                
                // Pick a random time to walk in this direction
                idleTimer = UnityEngine.Random.Range(wanderTimeMin, wanderTimeMax);
            }
        }

        // 3. EXECUTE WANDER MOVEMENT
        if (isWandering && !statusMgr.HasStatus(StatusType.Freeze))
        {
            // Calculate a slower speed for wandering
            float currentSpeed = stats.moveSpeed * wanderSpeedMultiplier * speedMultiplier;
            
            // Apply confusion if active
            Vector2 finalDir = statusMgr.HasStatus(StatusType.Confusion) ? -wanderDirection : wanderDirection;

            rb.linearVelocity = finalDir * currentSpeed;

            // Handle visuals (flip or rotate to face wander direction)
            HandleSpriteRotation(finalDir);
        }
        else if (!isWandering)
        {
            // Ensure we are fully stopped if not wandering (and not pushed by physics)
            rb.linearVelocity = Vector2.zero;
        }
    }


    protected virtual void LogicStunned() { /* Do nothing */ }   
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => stats.maxHealth;
    
    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (stats.dealsContactDamage && isContactReady && collision.CompareTag("Player") && !statusMgr.HasStatus(StatusType.Freeze))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                DamageInfo info = new DamageInfo(stats.contactDamage, DamageElement.Physical, AttackStyle.MeleeLight, transform.position, 1f);
                player.ReceiveDamage(info);
                StartCoroutine(ContactCooldownRoutine());
            }
        }
    }

    protected IEnumerator ContactCooldownRoutine()
    {
        isContactReady = false;
        yield return new WaitForSeconds(stats.contactCooldown);
        isContactReady = true;
    }

    IEnumerator KnockbackRoutine(float duration)
    {
        if (currentState == EnemyState.Dead) yield break;
        ChangeState(EnemyState.Stunned);
        yield return new WaitForSeconds(duration);
        if (currentState != EnemyState.Dead) ChangeState(EnemyState.Idle); 
    }

    protected void PushPlayer(GameObject player, float force)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ApplyPureKnockback(transform.position, force);
        }
    }

    // exposing methods for Weapon Effects (convenient)
public bool HasStatus(StatusType status)
{
    return statusMgr.HasStatus(status);
}

public void TryAddStatus(StatusType status)
{
    statusMgr.TryAddStatus(status);
}

public void TryAddStatus(StatusType status, float durationOverride)
{
    statusMgr.TryAddStatus(status, durationOverride);
}

public void ApplyTemporaryMoveSpeedMultiplier(float multiplier, float duration)
{
    if (temporaryMoveSpeedRoutine != null)
    {
        StopCoroutine(temporaryMoveSpeedRoutine);
    }

    speedMultiplier = Mathf.Max(0.01f, multiplier);
    temporaryMoveSpeedRoutine = StartCoroutine(ResetMoveSpeedRoutine(duration));
}

public void ApplyTemporaryAttackSpeedMultiplier(float multiplier, float duration)
{
    if (temporaryAttackSpeedRoutine != null)
    {
        StopCoroutine(temporaryAttackSpeedRoutine);
    }

    attackSpeedMultiplier = Mathf.Max(0.01f, multiplier);
    temporaryAttackSpeedRoutine = StartCoroutine(ResetAttackSpeedRoutine(duration));
}

public void SetTargetOverride(Transform target)
{
    playerTarget = target;
}

private IEnumerator ResetMoveSpeedRoutine(float duration)
{
    yield return new WaitForSeconds(duration);
    speedMultiplier = originalSpeedMultiplier;
    temporaryMoveSpeedRoutine = null;
}

private IEnumerator ResetAttackSpeedRoutine(float duration)
{
    yield return new WaitForSeconds(duration);
    attackSpeedMultiplier = 1f;
    temporaryAttackSpeedRoutine = null;
}

}
