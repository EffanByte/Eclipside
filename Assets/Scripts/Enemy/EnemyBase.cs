using System.Collections;
using UnityEngine;

// 1. Define States
public enum EnemyState
{
    Idle,       // Waiting for player / Spawning
    Chasing,    // Moving towards target
    Attacking,  // Performing action
    Stunned,    // Hit by heavy knockback or Freeze
    Dead
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
    
    // NEW: Allow setting these in the Inspector per enemy
    public DamageElement attackElement = DamageElement.Physical; 
    public AttackStyle attackStyle = AttackStyle.MeleeLight;
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Configuration")]
    public EnemyStats stats;

    [Header("Runtime State")]
    public EnemyState currentState = EnemyState.Idle;
    protected float currentHealth;
    protected float lastAttackTime;
    protected Transform playerTarget;
    
    // Components
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator anim; // Optional

    // Status Modifiers (For Freeze/Slows)
    protected float speedMultiplier = 1.0f; 

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        currentHealth = stats.maxHealth;
        
        // Find player (Assumes player has "Player" tag)
     GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;

        // Default to chasing immediately
        // This is a base case for when enemy definitely has to find player
        // There will be a different case later on for enemy searching for players
        ChangeState(EnemyState.Chasing);
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;

        // State Machine
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
    // STATE LOGIC (Virtual = Children can override this)
    // ---------------------------------------------------------

protected virtual void LogicIdle()
    {
        // Basic behavior: If player exists, start chasing
        if (playerTarget != null)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    protected virtual void LogicChasing()
    {
        if (playerTarget == null) return;

        float distance = Vector2.Distance(transform.position, playerTarget.position);

        // Move towards player
        MoveTowardsTarget();

        // Flip Sprite
        if (playerTarget.position.x > transform.position.x)
            spriteRenderer.flipX = false; // Face Right
        else
            spriteRenderer.flipX = true;  // Face Left
    }

    protected virtual void LogicAttacking()
    {
        // Wait for animation or cooldown, then return to chase
        if (Time.time > lastAttackTime + stats.attackCooldown)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    // ---------------------------------------------------------
    // CORE MECHANICS
    // ---------------------------------------------------------

    protected virtual void MoveTowardsTarget()
    {
        if (currentState != EnemyState.Chasing || playerTarget == null) return;

        Vector2 direction = (playerTarget.position - transform.position).normalized;
        
        // Apply velocity (Use MovePosition for smoother physics collision)
        rb.linearVelocity = direction * (stats.moveSpeed * speedMultiplier);
    }

    // Called when touching the player (Contact Damage)
    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (currentState == EnemyState.Dead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // Check cooldown
            if (Time.time > lastAttackTime + stats.attackCooldown)
            {
                PerformAttack(collision.gameObject);
                lastAttackTime = Time.time;
            }
        }
    }

    // ---------------------------------------------------------
    // REQUIRED FUNCTIONS
    // ---------------------------------------------------------

    // 1. PERFORM ATTACK
// Inside EnemyBase class

public virtual void PerformAttack(GameObject target)
{
    // 1. Get the Interface (Don't look for PlayerHealth directly)
    IDamageable damageable = target.GetComponent<IDamageable>();

    if (damageable != null)
    {
        // 2. Create the Packet using the Stats configured in Inspector
        DamageInfo info = new DamageInfo(
            amount: stats.damage,
            element: stats.attackElement,  // e.g., Fire, Physical, Poison
            style: stats.attackStyle,      // e.g., MeleeLight, Ranged
            sourcePosition: transform.position,
            knockbackForce: stats.knockbackForce
        );

        // 3. Send it
        damageable.ReceiveDamage(info);
        
        // Debug for testing
        Debug.Log($"{name} attacked {target.name} with {stats.attackElement} for {stats.damage} damage!");
    }
}

    // 2. RECEIVE DAMAGE (Interface Implementation)
    public virtual void ReceiveDamage(DamageInfo dmg)
    {
        // if (currentState == EnemyState.Dead) return;

        // // A. Weakness Logic
        // //float multiplier = DamageLogic.CalculateWeaknessMultiplier(dmg, stats.enemyTag);
        // float finalDamage = dmg.amount * multiplier;

        // // B. Apply Damage
        // currentHp -= finalDamage;

        // // C. Visual Feedback (Flash White)
        // StartCoroutine(FlashSpriteRoutine());

        // // D. Knockback
        // if (rb != null && dmg.knockbackForce > 0)
        // {
        //     Vector2 knockbackDir = (transform.position - (Vector3)dmg.sourcePosition).normalized;
        //     rb.AddForce(knockbackDir * dmg.knockbackForce, ForceMode2D.Impulse);
            
        //     // Optional: Briefly stun on heavy hit
        //     StartCoroutine(StunRoutine(0.2f)); 
        // }

        // // E. Death Check
        // if (currentHp <= 0)
        // {
        //     Die();
        // }
    }

    protected virtual void Die()
    {
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 0.5f); // Delay destroy for animation
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    // ---------------------------------------------------------
    // COROUTINES
    // ---------------------------------------------------------

    IEnumerator FlashSpriteRoutine()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white; // Flash white
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    IEnumerator StunRoutine(float duration)
    {
        EnemyState previousState = currentState;
        ChangeState(EnemyState.Stunned);
        yield return new WaitForSeconds(duration);
        ChangeState(previousState);
    }
}