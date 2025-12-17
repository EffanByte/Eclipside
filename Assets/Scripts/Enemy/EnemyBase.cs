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
    
    // REPLACEMENT: Boolean flag instead of timestamp
    protected bool isAttackReady = true;

    protected Transform playerTarget;
    
    // Components
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator anim; 

    // Status Modifiers (For Freeze/Slows)
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

        // Flip Sprite
        if (playerTarget.position.x > transform.position.x)
            spriteRenderer.flipX = false; 
        else
            spriteRenderer.flipX = true;  
    }

    protected virtual void LogicAttacking()
    {
        // Since we are using a boolean flag for cooldowns, 
        // we can simply switch back to chasing immediately after the attack frame,
        // or wait here if you have an animation lock.
        
        // For simple contact enemies, we just go back to chasing.
        ChangeState(EnemyState.Chasing);
    }

    // ---------------------------------------------------------
    // CORE MECHANICS
    // ---------------------------------------------------------

    protected virtual void MoveTowardsTarget()
    {
        if (currentState != EnemyState.Chasing || playerTarget == null) return;

        Vector2 direction = (playerTarget.position - transform.position).normalized;
        
        // Note: linearVelocity is for Unity 6+. If using older Unity, use .velocity
        rb.linearVelocity = direction * (stats.moveSpeed * speedMultiplier);
    }

protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // This will now check every frame while the bodies are touching
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
        Debug.Log("Attack performed");
        IDamageable damageable = target.GetComponent<IDamageable>();

        if (damageable != null)
        {
            Debug.Log("Damageable found");
            DamageInfo info = new DamageInfo(
                amount: stats.damage,
                element: stats.attackElement,  
                style: stats.attackStyle,      
                sourcePosition: transform.position,
                knockbackForce: stats.knockbackForce
            );
            Debug.Log("DamageInfo created");
            damageable.ReceiveDamage(info);
            Debug.Log($"{name} attacked {target.name}!");

            // START THE COOLDOWN ROUTINE
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    // NEW: The Cooutine that handles the timer
    protected IEnumerator AttackCooldownRoutine()
    {
        isAttackReady = false; // Lock attack
        yield return new WaitForSeconds(stats.attackCooldown); // Wait
        isAttackReady = true;  // Unlock attack
    }

    // ---------------------------------------------------------
    // DAMAGE RECEIVER
    // ---------------------------------------------------------

    public virtual void ReceiveDamage(DamageInfo dmg)
    {
        // Uncomment this logic when you have the rest of your system ready
        /*
        if (currentState == EnemyState.Dead) return;

        // Weakness calculation would go here...
        float finalDamage = dmg.amount; 

        currentHealth -= finalDamage;

        StartCoroutine(FlashSpriteRoutine());

        if (rb != null && dmg.knockbackForce > 0)
        {
            Vector2 knockbackDir = (transform.position - (Vector3)dmg.sourcePosition).normalized;
            rb.AddForce(knockbackDir * dmg.knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        */
    }

    protected virtual void Die()
    {
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 0.5f); 
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
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white; 
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