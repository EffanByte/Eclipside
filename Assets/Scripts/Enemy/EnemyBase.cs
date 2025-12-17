using System.Collections;
using UnityEngine;
using System;
// 1. Define States
public enum EnemyState
{
    Idle,       // Waiting for player / Spawning
    Chasing,    // Moving towards target
    Attacking,  // Performing action
    Stunned,    // Hit by heavy knockback or Freeze
    Dead
}

public enum DamageElement
{
    Physical, Magic, Fire, Ice, Poison, Psychic, True
}

public enum AttackStyle
{
    MeleeLight, MeleeHeavy, Ranged, Environment
}

[System.Serializable]
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


// 2. Define Stats (Editable in Inspector)
[System.Serializable]
public class EnemyStats
{
    [Header("General Stats")]
    public float maxHealth = 20f;
    public float moveSpeed = 3f;
    public float expReward = 10f;
    public string enemyTag = "Normal"; 

    [Header("Attack Configuration")]
    public float damage = 5f;          // Base Damage amount
    public float attackCooldown = 1f;
    public float knockbackForce = 3f;  // How hard they hit the player
    
    public DamageElement attackElement = DamageElement.Physical; 
    public AttackStyle attackStyle = AttackStyle.MeleeLight;
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Configuration")]
    public EnemyStats stats;

    [Header("Runtime State")]
    public EnemyState currentState = EnemyState.Idle;
    protected float currentHealth;
    
    // Flag for cooldowns
    protected bool isAttackReady = true;

    protected Transform playerTarget;
    
    // Components
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator anim; 

    // Status Modifiers
    protected float speedMultiplier = 1.0f; 

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        currentHealth = stats.maxHealth;
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;

        // Ensure attack is ready at start
        isAttackReady = true;

        ChangeState(EnemyState.Chasing);
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;

        switch (currentState)
        {
            case EnemyState.Idle:
                LogicIdle();
                break;
            case EnemyState.Chasing:
                LogicChasing();
                break;
            case EnemyState.Attacking:
                LogicAttacking();
                break;
        }
    }

    // ---------------------------------------------------------
    // STATE LOGIC 
    // ---------------------------------------------------------

    protected virtual void LogicIdle()
    {
        if (playerTarget != null)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    protected virtual void LogicChasing()
    {
        if (playerTarget == null) return;

        MoveTowardsTarget();

        // Flip Sprite based on player position
        if (playerTarget.position.x > transform.position.x)
            spriteRenderer.flipX = false; 
        else
            spriteRenderer.flipX = true;  
    }

    protected virtual void LogicAttacking()
    {
        ChangeState(EnemyState.Chasing);
    }

    // ---------------------------------------------------------
    // CORE MECHANICS
    // ---------------------------------------------------------

    protected virtual void MoveTowardsTarget()
    {
        if (currentState != EnemyState.Chasing || playerTarget == null) return;

        Vector2 direction = (playerTarget.position - transform.position).normalized;
        
        // Using linearVelocity for Unity 6+
        rb.linearVelocity = direction * (stats.moveSpeed * speedMultiplier);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (isAttackReady)
            {
                PerformAttack(collision.gameObject);
            }
        }
    }

    // ---------------------------------------------------------
    // ATTACK LOGIC
    // ---------------------------------------------------------

    public virtual void PerformAttack(GameObject target)
    {
        PlayerHealth damageable = target.GetComponent<PlayerHealth>();

        if (damageable != null)
        {
            DamageInfo info = new DamageInfo(
                amount: stats.damage,
                element: stats.attackElement,  
                style: stats.attackStyle,      
                sourcePosition: transform.position,
                knockbackForce: stats.knockbackForce
            );

            damageable.ReceiveDamage(2f);
            
        }
    }

    protected IEnumerator AttackCooldownRoutine()
    {
        isAttackReady = false; 
        yield return new WaitForSeconds(stats.attackCooldown); 
        isAttackReady = true; 
    }

    // ---------------------------------------------------------
    // DAMAGE RECEIVER (UPDATED)
    // ---------------------------------------------------------

    public static event Action<EnemyBase> OnEnemyKilled;
    public virtual void ReceiveDamage(DamageInfo dmg)
    {
        // 2. Reduce Health
        currentHealth -= dmg.amount;
        Debug.Log($"{name} Took {dmg.amount} damage. Current HP: {currentHealth}");

        // 3. Visual Feedback
        StartCoroutine(FlashSpriteRoutine());

        // 4. Knockback Logic
        if (rb != null && dmg.knockbackForce > 0)
        {
            // Push enemy away from the source of damage
            //Vector2 knockbackDir = (transform.position - (Vector3)dmg.sourcePosition).normalized;
            
            // Add impulsive force
           // rb.AddForce(knockbackDir * dmg.knockbackForce, ForceMode2D.Impulse);
            
            // Optional: Briefly stun them so they don't immediately walk back while flying
            StartCoroutine(StunRoutine(0.2f)); 
        }

        // 5. Check for Death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} Died!");
        currentState = EnemyState.Dead;
        OnEnemyKilled?.Invoke(this);
        // Stop movement immediately
        rb.linearVelocity = Vector2.zero;
        
        // Disable collider so player walks through corpse
        GetComponent<Collider2D>().enabled = false;

        // TODO: Spawn EXP Orbs or Loot here

        // Destroy the GameObject after a short delay (e.g. for death animation)
        Destroy(gameObject); 
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    // ---------------------------------------------------------
    // VISUALS
    // ---------------------------------------------------------

    IEnumerator FlashSpriteRoutine()
    {
        Color original = Color.white; // Assuming sprite is white by default
        spriteRenderer.color = Color.red; // Flash Red
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    IEnumerator StunRoutine(float duration)
    {
        // Only switch to stunned if not dead
        if (currentState == EnemyState.Dead) yield break;

        EnemyState previousState = currentState;
        ChangeState(EnemyState.Stunned);
        
        yield return new WaitForSeconds(duration);
        
        // Return to previous state if still alive
        if (currentState != EnemyState.Dead)
        {
            ChangeState(previousState);
        }
    }
}