using System.Collections;
using UnityEngine;
using System;
using TMPro;
public enum EnemyState
{
    Idle, Chasing, Attacking, Stunned, Dead
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
    
    [Header("Attack Configuration")]
    public float damage;        
    public float attackCooldown;
    public float knockbackForce; 
    public float projectileSpeed;
    
    public DamageElement attackElement;
    public AttackStyle attackStyle;

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
    public bool isAttackReady = true;
    protected Transform playerTarget;
    
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator anim; 
    protected MovementType movementType;

    // Status Modifiers
    protected float speedMultiplier = 1.0f; 
    protected float originalSpeedMultiplier;

    // Modifiers      
    private float selfAttackMaxHpPercent = 0f;        

    public static event Action<EnemyBase> OnEnemyKilled;
    // for status effects
    protected virtual void Start()
    {   
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
        isAttackReady = true;
        ChangeState(EnemyState.Chasing);
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;

        switch (currentState)
        {
            case EnemyState.Idle: LogicIdle(); break;
            case EnemyState.Chasing: LogicChasing(); break;
            case EnemyState.Attacking: LogicAttacking(); break;
            case EnemyState.Stunned: LogicStunned(); break;
        }

        if (transform.position.y < -30f) Die();
    }

    // ---------------------------------------------------------
    // DAMAGE RECEIVER
    // ---------------------------------------------------------
    public virtual void ReceiveDamage(DamageInfo dmg)
    {
        if (currentState == EnemyState.Dead) return;

        // 1. Apply Damage (Fragile Multiplier from Manager)
        float finalDamage = dmg.amount * statusMgr.DamageTakenMultiplier;
        currentHealth -= finalDamage;

        // 2. Logic to PREVENT Infinite Loops
        if (dmg.element != DamageElement.True && dmg.element != DamageElement.Physical)
        {
            StatusType effect = statusMgr.GetStatusFromElement(dmg.element);
            statusMgr.TryAddStatus(effect);
        }
                if (rb != null && dmg.knockbackForce > 0)
        {
            Vector2 knockbackDir = (rb.position - dmg.sourcePosition).normalized;
            rb.AddForce(knockbackDir * dmg.knockbackForce, ForceMode2D.Impulse);
            StartCoroutine(KnockbackRoutine(0.2f));
        }

        StartCoroutine(statusMgr.FlashSpriteRoutine(dmg.element)); // NOT GOOD PRACTICE TO put flash logic from statusMgr
        if (currentHealth <= 0f) Die();
    }

// Same as ReceiveDamage but no knockback and no apply status effect
    public virtual void StatusDamage(DamageInfo dmg)
        {
            if (currentState == EnemyState.Dead) return;

            // 1. Apply Immediate Damage (Physical Calculation)
            float finalDamage = dmg.amount * statusMgr.DamageTakenMultiplier;
            currentHealth -= finalDamage;
            
            Debug.Log($"{name} Hit by {dmg.element}. HP: {currentHealth}");

            StartCoroutine(statusMgr.FlashSpriteRoutine(dmg.element)); 

            if (currentHealth <= 0f) Die();
        }

// ---------------------------------------------------------
    // DEBUG GIZMOS
    // ---------------------------------------------------------
    protected virtual void OnDrawGizmosSelected()
    {
        // Draw the Explosion Radius for Burn+Poison Synergy
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }

    // --- BASE STATUS APPLICATION ---


    // private void TriggerAreaExplosion(DamageInfo dmg, float radius)
    // {
    //     // 1. Find everything inside the radius
    //     Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

    //     // 2. Create the explosion damage packet
    //     // Note: We use Fire element for the explosion. 
    //     // WARNING: If neighbors have Poison, this might trigger a chain reaction explosion!


    //     foreach (var hit in hits)
    //     {
    //         // 3. Filter for Enemies
    //         // We use EnemyBase to find other enemies. 
    //         // Change to IDamageable if you want it to hurt the Player too.
    //         EnemyBase neighbor = hit.GetComponent<EnemyBase>();

    //         // 4. Apply Damage
    //         // neighbor != this: Prevents the enemy from exploding itself twice
    //         if (neighbor != null && neighbor != this)
    //         {
    //             neighbor.ReceiveDamage(dmg);
    //         }
    //     }
    // }

    // ---------------------------------------------------------
    // HELPERS & IMPLEMENTATIONS
    // ---------------------------------------------------------




    private IEnumerator ResetSelfAttackBonus(float delay)
    {
        yield return new WaitForSeconds(delay);
        selfAttackMaxHpPercent = 0f;
    }

    public void ApplyDifficultyScaling(float multiplier)
    {
        stats.maxHealth *= multiplier;
        currentHealth = GetMaxHealth();
    }

    // ---------------------------------------------------------
    // STANDARD BEHAVIOR (Movement, Attack, etc)
    // ---------------------------------------------------------

    protected virtual void LogicIdle()
    {
        if (playerTarget != null) ChangeState(EnemyState.Chasing);
    }

    protected virtual void LogicStunned()
    {
        Debug.Log($"{name} is stunned and cannot move.");
    }   
    protected virtual void LogicChasing()
    {
        if (playerTarget == null) return;

        Vector2 direction = (playerTarget.position - transform.position).normalized;
        if(statusMgr.HasStatus(StatusType.Confusion)) direction = -direction; // Inverted movement

        MoveTowardsTarget(direction);

        if (movementType == MovementType.Flip)
            spriteRenderer.flipX = direction.x > 0; 
        else if (movementType == MovementType.Rotate)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    protected virtual void MoveTowardsTarget(Vector2 direction)
    {
        if (currentState != EnemyState.Chasing) return;
        
        float finalSpeed = stats.moveSpeed * speedMultiplier;

        rb.linearVelocity = direction * finalSpeed;
    }

    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && isAttackReady && !statusMgr.HasStatus(StatusType.Freeze))
        {
            PerformAttack(collision.gameObject);  
            StartCoroutine(AttackCooldownRoutine());
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

    public virtual void PerformAttack(GameObject target)
    {
        // Handle Self-Damage from Confusion Synergy
        if (selfAttackMaxHpPercent > 0)
        {
            float selfDmg = stats.maxHealth * selfAttackMaxHpPercent;
            currentHealth -= selfDmg;
            LogCombat("Confused! Hurt itself in confusion.");
        }

        PlayerController damageable = target.GetComponent<PlayerController>();
        if (damageable != null)
        {
            DamageInfo info = new DamageInfo(
                amount: stats.damage,
                element: stats.attackElement,  
                style: stats.attackStyle,      
                sourcePosition: transform.position,
                knockbackForce: stats.knockbackForce
            );
            damageable.ReceiveDamage(info);
        }
    }

    protected IEnumerator AttackCooldownRoutine()
    {
        isAttackReady = false; 
        yield return new WaitForSeconds(stats.attackCooldown); 
        isAttackReady = true; 
    }

    protected void Die()
    {
        currentState = EnemyState.Dead;
        OnEnemyKilled?.Invoke(this);
        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        PlayerController.Instance.AddCurrency(CurrencyType.Rupee, stats.rupeeReward);
        PlayerController.Instance.AddExperience(EnemyStats.expReward);
        StatisticsManager.Instance.IncrementStat("TOTAL_KILLS");
        Destroy(gameObject);
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    protected virtual void LogicAttacking() { ChangeState(EnemyState.Chasing); }

    // Visuals


    IEnumerator KnockbackRoutine(float duration)
    {
        if (currentState == EnemyState.Dead) yield break;
        EnemyState previousState = currentState;
        ChangeState(EnemyState.Stunned);
        yield return new WaitForSeconds(duration);
        if (currentState != EnemyState.Dead) ChangeState(EnemyState.Idle); // removed previousState since it might loop stunned forever
    }

    private void LogCombat(string message)
    {
        if (textbox != null) textbox.text = $"\n{message}";
        Debug.Log(message);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return stats.maxHealth;
    }

}