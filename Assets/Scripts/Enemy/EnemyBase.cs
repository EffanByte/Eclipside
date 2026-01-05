    using System.Collections;
    using UnityEngine;
    using System;
using TMPro;
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
    public enum MovementType
    {
        Flip, Rotate
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
        private TextMeshProUGUI textbox;
        [Header("Configuration")]
        public EnemyStats stats;

        [Header("Runtime State")]
        public EnemyState currentState = EnemyState.Idle;
        protected float currentHealth;
        
        // Flag for cooldowns
        public bool isAttackReady = true;

        protected Transform playerTarget;
        
        // Components
        protected Rigidbody2D rb;
        protected SpriteRenderer spriteRenderer;
        protected Animator anim; 
        protected MovementType movementType;

        // Status Modifiers
        protected float speedMultiplier = 1.0f; 
        private enum StatusType { Burn, Poison, Freeze, Confusion, Fragile }

[SerializeField] private float explosionRadius = 2.5f;
[SerializeField] private float explosionDamage = 3f;

// active status end-times
private readonly System.Collections.Generic.Dictionary<StatusType, float> statusEnd =
    new System.Collections.Generic.Dictionary<StatusType, float>();

// running coroutines (for DoT etc.)
private readonly System.Collections.Generic.Dictionary<StatusType, Coroutine> statusCo =
    new System.Collections.Generic.Dictionary<StatusType, Coroutine>();

// modifiers
private float damageTakenMultiplier = 1f;         // used by Fragile
private float selfAttackMaxHpPercent = 0f;        // used by Burn+Confusion
private bool freezeConfusionThawBonus = false;    // used by Freeze+Confusion

        protected virtual void Start()
        {
            textbox = FindObjectOfType<TextMeshProUGUI>();
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
            if (transform.position.y < -50f)
            {
                Die();
            }
            CleanupExpiredStatuses();

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

        // Calculate direction ONCE
        Vector2 direction = (playerTarget.position - transform.position).normalized;

        // Pass direction to movement
        MoveTowardsTarget(direction);

        // Handle Rotation / Flipping
        if (movementType == MovementType.Flip)
        {
            // Flip based on X direction
            spriteRenderer.flipX = direction.x < 0; 
        }
        else if (movementType == MovementType.Rotate)
        {
            // Rotate to face player
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Assumption: Sprite faces RIGHT. If sprite faces UP, use (angle - 90)
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    protected virtual void MoveTowardsTarget(Vector2 direction)
    {
        if (currentState != EnemyState.Chasing) return;
        rb.linearVelocity = direction * (stats.moveSpeed * speedMultiplier);
    }

        protected virtual void LogicAttacking()
        {
            ChangeState(EnemyState.Chasing);
        }

        // ---------------------------------------------------------
        // CORE MECHANICS
        // ---------------------------------------------------------

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

    float selfDmg = stats.maxHealth * selfAttackMaxHpPercent;
    currentHealth -= selfDmg;

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
    if (currentState == EnemyState.Dead) return;

    // Apply fragile multiplier to incoming damage
    float finalDamage = dmg.amount * damageTakenMultiplier;
    currentHealth -= finalDamage;


    StartCoroutine(FlashSpriteRoutine());

    if (rb != null && dmg.knockbackForce > 0)
    {
        Vector2 knockbackDir = (rb.position - dmg.sourcePosition).normalized;
        rb.AddForce(knockbackDir * 3f, ForceMode2D.Impulse);
        StartCoroutine(StunRoutine(0.2f));
    }

    // ----- COMBO CHECKS -----
    // Map incoming element to status
    bool incomingBurn = dmg.element == DamageElement.Fire;
    bool incomingPoison = dmg.element == DamageElement.Poison;
    bool incomingFreeze = dmg.element == DamageElement.Ice;
    bool incomingConfuse = dmg.element == DamageElement.Psychic;

    // Burn + Poison: area explosion + extra DoT (0.2 hearts/sec for 3s).
    if (incomingBurn && HasStatus(StatusType.Poison))
    {
        TriggerExplosion(transform.position);
        StartDot(StatusType.Burn, dps: 0.2f, duration: 3f);
        ClearStatus(StatusType.Poison);

        //LogCombat("Burn + Poison = Explosion + DoT (0.2/sec for 3s)");
    }
    else if (incomingPoison && HasStatus(StatusType.Burn))
    {
        TriggerExplosion(transform.position);
        StartDot(StatusType.Poison, dps: 0.2f, duration: 3f);
        ClearStatus(StatusType.Burn);
        // LogCombat(" Poison + Burn = Explosion + DoT (0.2/sec for 3s)");
    }

    // Burn + Freeze: instant damage + Fragile (+20% damage taken for 3s).
    if ((incomingBurn && HasStatus(StatusType.Freeze)) || (incomingFreeze && HasStatus(StatusType.Burn)))
    {
        currentHealth -= 2f;          // "instant damage" (tune this number)
        ApplyFragile(3f);
        ClearStatus(StatusType.Burn);
        ClearStatus(StatusType.Freeze);
        LogCombat(" Burn + Freeze = Instant damage + Fragile (+20% for 3s)");
    }

    // Burn + Confusion: extra DoT (0.3 hearts/sec for 3s) + self-attacks deal 5% max HP.
    if ((incomingBurn && HasStatus(StatusType.Confusion)) || (incomingConfuse && HasStatus(StatusType.Burn)))
    {
        StartDot(StatusType.Burn, dps: 0.3f, duration: 3f);
        ApplyConfusion(3f);
        selfAttackMaxHpPercent = 0.05f; // 5% max HP self-damage on attacks (see PerformAttack hook below)
        ClearStatus(StatusType.Burn);
        LogCombat(" Burn + Confusion = DoT (0.3/sec) + Self-damage (5% max HP)");
    }

    // Poison + Freeze: strong damage (0.2 hearts/sec for 5s).
    if ((incomingPoison && HasStatus(StatusType.Freeze)) || (incomingFreeze && HasStatus(StatusType.Poison)))
    {
        StartDot(StatusType.Poison, dps: 0.2f, duration: 5f);
        ClearStatus(StatusType.Poison);
        ClearStatus(StatusType.Freeze);
        LogCombat(" Poison + Freeze = Strong DoT (0.2/sec for 5s)");

    }

    // Poison + Confusion: Confusion duration +50% + poison deals +0.1 heart per tick.
    if ((incomingPoison && HasStatus(StatusType.Confusion)) || (incomingConfuse && HasStatus(StatusType.Poison)))
    {
        // extend confusion by +50% (assume base 3s -> +1.5s)
        float extra = 1.5f;
        SetStatus(StatusType.Confusion, (statusEnd.TryGetValue(StatusType.Confusion, out var end) ? (end - Time.time) : 0f) + extra);

        // poison +0.1 per tick (with 1s tick this is +0.1 dps)
        StartDot(StatusType.Poison, dps: 0.1f, duration: 5f);
        LogCombat(" Poison + Confusion = Confusion +50% duration + Poison +0.1/tick");

    }

    // Freeze + Confusion: when thawing, enters extra Confusion (3s).
    if ((incomingFreeze && HasStatus(StatusType.Confusion)) || (incomingConfuse && HasStatus(StatusType.Freeze)))
    {
        freezeConfusionThawBonus = true;
        ApplyFreeze(3f);
        ApplyConfusion(3f);
        LogCombat(" Freeze + Confusion = Extra Confusion on thaw (3s)");
    }

    // ----- Apply BASE STATUS if no combo consumed it -----
    if (incomingBurn && !HasStatus(StatusType.Burn)) ApplyBurn(3f);
    if (incomingPoison && !HasStatus(StatusType.Poison)) ApplyPoison(3f);
    if (incomingFreeze && !HasStatus(StatusType.Freeze)) ApplyFreeze(3f);
    if (incomingConfuse && !HasStatus(StatusType.Confusion)) ApplyConfusion(3f);

    if (currentHealth <= 0f)
        Die();
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
            
            // Return to    previous state if still alive
            if (currentState != EnemyState.Dead)
            {
                ChangeState(previousState);
            }
        }
        private bool HasStatus(StatusType s)
{
    return statusEnd.TryGetValue(s, out float end) && Time.time < end;
}

private void SetStatus(StatusType s, float duration)
{
    statusEnd[s] = Time.time + duration;
}

private void ClearStatus(StatusType s)
{
    statusEnd.Remove(s);
    if (statusCo.TryGetValue(s, out var co) && co != null)
        StopCoroutine(co);
    statusCo.Remove(s);

    // clean side-effects
    if (s == StatusType.Fragile) damageTakenMultiplier = 1f;
    if (s == StatusType.Confusion) selfAttackMaxHpPercent = 0f;
}

private void StartDot(StatusType key, float dps, float duration)
{
    // restart dot if already running
    if (statusCo.TryGetValue(key, out var co) && co != null)
        StopCoroutine(co);

    statusCo[key] = StartCoroutine(DotRoutine(key, dps, duration));
}

private IEnumerator DotRoutine(StatusType key, float dps, float duration)
{
    SetStatus(key, duration);

    float t = 0f;
    const float tick = 1f;

    while (t < duration && currentState != EnemyState.Dead)
    {
        // deal dps per second
        currentHealth -= dps * tick;
        if (currentHealth <= 0f)
        {
            Die();
            yield break;
        }

        yield return new WaitForSeconds(tick);
        t += tick;
    }

    // dot ended
    ClearStatus(key);
}

private void TriggerExplosion(Vector2 center)
{
    var hits = Physics2D.OverlapCircleAll(center, explosionRadius);

    foreach (var h in hits)
    {
        if (!h) continue;

        // Example: damage other enemies (skip self)
        var otherEnemy = h.GetComponentInParent<EnemyBase>();
        if (otherEnemy != null && otherEnemy != this && otherEnemy.currentState != EnemyState.Dead)
        {
            otherEnemy.ReceiveDamage(new DamageInfo(
                amount: explosionDamage,
                element: DamageElement.Fire,
                style: AttackStyle.Environment,
                sourcePosition: center,
                knockbackForce: 0f
            ));
        }

        // If you also want to damage player, do it here (optional):
        // var ph = h.GetComponentInParent<PlayerHealth>();
        // if (ph != null) ph.ReceiveDamage(explosionDamage);
    }
}

// Call this from Update() to auto-expire non-coroutine statuses
private void CleanupExpiredStatuses()
{
    // handle Freeze thaw bonus
    if (HasStatus(StatusType.Freeze) == false && statusEnd.ContainsKey(StatusType.Freeze))
    {
        // freeze just ended
        statusEnd.Remove(StatusType.Freeze);

        if (freezeConfusionThawBonus)
        {
            freezeConfusionThawBonus = false;
            // "when thawing, enters extra Confusion (3s)."
            ApplyConfusion(3f);
        }
    }

    // expire Fragile
    if (statusEnd.ContainsKey(StatusType.Fragile) && !HasStatus(StatusType.Fragile))
        ClearStatus(StatusType.Fragile);

    // expire Confusion (if not coroutine-based)
    if (statusEnd.ContainsKey(StatusType.Confusion) && !HasStatus(StatusType.Confusion))
        ClearStatus(StatusType.Confusion);
}

private void ApplyBurn(float duration = 3f) => SetStatus(StatusType.Burn, duration);
private void ApplyPoison(float duration = 3f) => SetStatus(StatusType.Poison, duration);
private void ApplyFreeze(float duration = 3f) => SetStatus(StatusType.Freeze, duration);

private void ApplyConfusion(float duration)
{
    SetStatus(StatusType.Confusion, duration);
}

private void ApplyFragile(float duration)
{
    damageTakenMultiplier = 1.2f; // +20% damage taken
    SetStatus(StatusType.Fragile, duration);
}

private void LogCombat(string message)
{
    if (textbox == null) return;
    textbox.text = $"\n{message}";
}


    }