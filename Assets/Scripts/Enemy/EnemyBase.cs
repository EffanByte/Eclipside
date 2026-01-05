using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public enum EnemyState
{
    Idle, Chasing, Attacking, Stunned, Dead
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

    // Define Status Types strictly for Logic
    public enum StatusType
    { None, Burn, Poison, Freeze, Confusion, Fragile }

[System.Serializable]
public class EnemyStats
{
    [Header("General Stats")]
    public float maxHealth;
    public float moveSpeed;
    public float expReward;
    
    [Header("Attack Configuration")]
    public float damage;        
    public float attackCooldown;
    public float knockbackForce; 
    
    public DamageElement attackElement;
    public AttackStyle attackStyle;


}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour
{
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

    // Active status end-times
    private readonly Dictionary<StatusType, float> statusEnd = new Dictionary<StatusType, float>();

    // Running coroutines (for DoT etc.)
    private readonly Dictionary<StatusType, Coroutine> statusCo = new Dictionary<StatusType, Coroutine>();

    // Modifiers
    private float damageTakenMultiplier = 1f;         
    private float selfAttackMaxHpPercent = 0f;        
    private bool freezeConfusionThawBonus = false;    

    public static event Action<EnemyBase> OnEnemyKilled;

    protected virtual void Start()
    {   
        textbox = FindObjectOfType<TextMeshProUGUI>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        currentHealth = stats.maxHealth;
        
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
        }

        if (transform.position.y < -50f) Die();
        
        CleanupExpiredStatuses();
    }

    // ---------------------------------------------------------
    // DAMAGE RECEIVER
    // ---------------------------------------------------------
    public virtual void ReceiveDamage(DamageInfo dmg)
    {
        if (currentState == EnemyState.Dead) return;

        // 1. Apply Immediate Damage (Physical Calculation)
        float finalDamage = dmg.amount * damageTakenMultiplier;
        currentHealth -= finalDamage;
        
        Debug.Log($"{name} Hit by {dmg.element}. HP: {currentHealth}");

        // 2. Visuals & Physics
        Color flashColor = GetDamageFlashColor(dmg.element);
        StartCoroutine(FlashSpriteRoutine(flashColor)); 

        if (rb != null && dmg.knockbackForce > 0)
        {
            Vector2 knockbackDir = (rb.position - dmg.sourcePosition).normalized;
            rb.AddForce(knockbackDir * 3f, ForceMode2D.Impulse); // Fixed force for test, use dmg.knockbackForce in prod
            StartCoroutine(StunRoutine(0.2f));
        }

        // 3. ATTEMPT TO APPLY STATUS / SYNERGY
        // We convert the element to a status type, then check for combos
        StatusType incomingStatus = GetStatusFromElement(dmg.element);
        
        if (incomingStatus != StatusType.None)
        {
            TryAddStatus(incomingStatus);
        }

        // 4. Death Check
        if (currentHealth <= 0f) Die();
    }

    // ---------------------------------------------------------
    // SYNERGY & STATUS LOGIC
    // ---------------------------------------------------------

    public void TryAddStatus(StatusType incoming)
    {
        // Checking for specific pairings. 
        // If find a match, return early so don't have to apply the base status.

        // --- PAIRINGS WITH BURN ---
        if (incoming == StatusType.Burn)
        {
            if (CheckSynergy(StatusType.Poison)) { TriggerSynergy_BurnPoison(); return; }
            if (CheckSynergy(StatusType.Freeze)) { TriggerSynergy_BurnFreeze(); return; }
            if (CheckSynergy(StatusType.Confusion)) { TriggerSynergy_BurnConfusion(); return; }
        }

        // --- PAIRINGS WITH POISON ---
        if (incoming == StatusType.Poison)
        {
            if (CheckSynergy(StatusType.Burn)) { TriggerSynergy_BurnPoison(); return; } // Symmetric
            if (CheckSynergy(StatusType.Freeze)) { TriggerSynergy_PoisonFreeze(); return; }
            if (CheckSynergy(StatusType.Confusion)) { TriggerSynergy_PoisonConfusion(); return; }
        }

        // --- PAIRINGS WITH FREEZE ---
        if (incoming == StatusType.Freeze)
        {
            if (CheckSynergy(StatusType.Burn)) { TriggerSynergy_BurnFreeze(); return; }
            if (CheckSynergy(StatusType.Poison)) { TriggerSynergy_PoisonFreeze(); return; }
            if (CheckSynergy(StatusType.Confusion)) { TriggerSynergy_FreezeConfusion(); return; }
        }

        // --- PAIRINGS WITH CONFUSION ---
        if (incoming == StatusType.Confusion)
        {
            if (CheckSynergy(StatusType.Burn)) { TriggerSynergy_BurnConfusion(); return; }
            if (CheckSynergy(StatusType.Poison)) { TriggerSynergy_PoisonConfusion(); return; }
            if (CheckSynergy(StatusType.Freeze)) { TriggerSynergy_FreezeConfusion(); return; }
        }

        // --- NO SYNERGY FOUND: APPLY BASE STATUS ---
        ApplyBaseStatus(incoming);
    }

    /// <summary>
    /// Helper: If the enemy has the 'existing' status, clear it and return true.
    /// This effectively "Consumes" the existing status for the synergy.
    /// </summary>
    private bool CheckSynergy(StatusType existing)
    {
        if (HasStatus(existing))
        {
            ClearStatus(existing); // Remove the old one
            return true;           // Confirm synergy
        }
        return false;
    }

    // --- INDIVIDUAL SYNERGY EFFECTS ---

    private void TriggerSynergy_BurnPoison()
    {
        // Burn + Poison: Area explosion + extra DoT (0.2 hearts/sec for 3s).
        LogCombat("SYNERGY: Burn + Poison! (Explosion + DoT)");
        // TODO: Spawn Explosion Prefab here
        StartDot(StatusType.Poison, dps: 2f, duration: 3f); // 2f = 0.2 hearts * 10 health scale
    }

    private void TriggerSynergy_BurnFreeze()
    {
        // Burn + Freeze: Instant damage + Fragile (+20% damage taken for 3s).
        LogCombat("SYNERGY: Burn + Freeze! (Thermal Shock + Fragile)");
        currentHealth -= 20f; // Instant damage (2 hearts)
        ApplyFragile(3f);
    }

    private void TriggerSynergy_BurnConfusion()
    {
        // Burn + Confusion: Extra DoT + self-attacks deal 5% max HP.
        LogCombat("SYNERGY: Burn + Confusion! (Self-Harm Mode)");
        StartDot(StatusType.Burn, dps: 3f, duration: 3f); // 0.3 hearts/sec
        selfAttackMaxHpPercent = 0.05f; 
        // We set a hidden timer to clear the self-attack flag since Confusion was consumed
        StartCoroutine(ResetSelfAttackBonus(3f));
    }

    private void TriggerSynergy_PoisonFreeze()
    {
        // Poison + Freeze: Strong damage (0.2 hearts/sec for 5s).
        LogCombat("SYNERGY: Poison + Freeze! (Strong DoT)");
        StartDot(StatusType.Poison, dps: 2f, duration: 5f);
    }

    private void TriggerSynergy_PoisonConfusion()
    {
        // Poison + Confusion: Confusion duration +50% + poison deals +0.1 heart per tick.
        LogCombat("SYNERGY: Poison + Confusion! (Extended Confusion + Strong Poison)");
        // Since we consumed confusion, we re-apply it longer
        ApplyConfusion(4.5f); // 3s base + 50%
        StartDot(StatusType.Poison, dps: 1f + 1f, duration: 5f); // Base + Bonus
    }

    private void TriggerSynergy_FreezeConfusion()
    {
        // Freeze + Confusion: When thawing, enters extra Confusion (3s).
        LogCombat("SYNERGY: Freeze + Confusion! (Thaw Nightmare)");
        // We apply Freeze, but set the flag so confusion triggers when it ends
        ApplyFreeze(3f); 
        freezeConfusionThawBonus = true; 
    }

    // --- BASE STATUS APPLICATION ---

    private void ApplyBaseStatus(StatusType type)
    {
        switch (type)
        {
            case StatusType.Burn: ApplyBurn(3f); break;
            case StatusType.Poison: ApplyPoison(3f); break;
            case StatusType.Freeze: ApplyFreeze(3f); break;
            case StatusType.Confusion: ApplyConfusion(3f); break;
        }
    }

    // ---------------------------------------------------------
    // HELPERS & IMPLEMENTATIONS
    // ---------------------------------------------------------

    private StatusType GetStatusFromElement(DamageElement element)
    {
        return element switch
        {
            DamageElement.Fire => StatusType.Burn,
            DamageElement.Poison => StatusType.Poison,
            DamageElement.Ice => StatusType.Freeze,
            DamageElement.Psychic => StatusType.Confusion,
            _ => StatusType.None
        };
    }

    public bool HasStatus(StatusType s)
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
        if (statusCo.TryGetValue(s, out var co) && co != null) StopCoroutine(co);
        statusCo.Remove(s);

        // Cleanup modifiers immediately
        if (s == StatusType.Fragile) damageTakenMultiplier = 1f;
        // Note: We don't clear selfAttackMaxHpPercent here immediately because some synergies
        // rely on it persisting after the status is consumed.
    }

    private void StartDot(StatusType key, float dps, float duration)
    {
        if (statusCo.TryGetValue(key, out var co) && co != null) StopCoroutine(co);
        statusCo[key] = StartCoroutine(DotRoutine(key, dps, duration));
    }

    private IEnumerator DotRoutine(StatusType key, float dps, float duration)
    {
        SetStatus(key, duration);
        float t = 0f;
        const float tick = 1f;

        while (t < duration && currentState != EnemyState.Dead)
        {
            currentHealth -= dps * tick;
            if (currentHealth <= 0f) { Die(); yield break; }
            yield return new WaitForSeconds(tick);
            t += tick;
        }
        ClearStatus(key);
    }

    private IEnumerator ResetSelfAttackBonus(float delay)
    {
        yield return new WaitForSeconds(delay);
        selfAttackMaxHpPercent = 0f;
    }

    private void CleanupExpiredStatuses()
    {
        // Handle Freeze Thaw
        if (!HasStatus(StatusType.Freeze) && statusEnd.ContainsKey(StatusType.Freeze))
        {
            statusEnd.Remove(StatusType.Freeze);
            if (freezeConfusionThawBonus)
            {
                freezeConfusionThawBonus = false;
                ApplyConfusion(3f);
                LogCombat("Thawed into Confusion!");
            }
        }
        
        // General cleanup
        List<StatusType> toRemove = new List<StatusType>();
        foreach(var kvp in statusEnd)
        {
            if(Time.time >= kvp.Value) toRemove.Add(kvp.Key);
        }

        foreach(var s in toRemove) ClearStatus(s);
    }

    private void ApplyBurn(float duration) => StartDot(StatusType.Burn, 2f, duration); // 0.2 hearts = 2.0 hp
    private void ApplyPoison(float duration) => StartDot(StatusType.Poison, 1f, duration); // 0.1 hearts = 1.0 hp
    private void ApplyFreeze(float duration) => SetStatus(StatusType.Freeze, duration);
    private void ApplyConfusion(float duration) => SetStatus(StatusType.Confusion, duration);
    private void ApplyFragile(float duration)
    {
        damageTakenMultiplier = 1.2f;
        SetStatus(StatusType.Fragile, duration);
    }

    // ---------------------------------------------------------
    // STANDARD BEHAVIOR (Movement, Attack, etc)
    // ---------------------------------------------------------

    protected virtual void LogicIdle()
    {
        if (playerTarget != null) ChangeState(EnemyState.Chasing);
    }

    protected virtual void LogicChasing()
    {
        if (playerTarget == null || HasStatus(StatusType.Freeze)) return; // Don't move if frozen

        Vector2 direction = (playerTarget.position - transform.position).normalized;
        if(HasStatus(StatusType.Confusion)) direction = -direction; // Inverted movement

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
        if (HasStatus(StatusType.Freeze)) finalSpeed = 0; // Backup check

        rb.linearVelocity = direction * finalSpeed;
    }

    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && isAttackReady && !HasStatus(StatusType.Freeze))
        {
            PerformAttack(collision.gameObject);  
            StartCoroutine(AttackCooldownRoutine());
        }
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
            damageable.ReceiveDamage(info);
        }
    }

    protected IEnumerator AttackCooldownRoutine()
    {
        isAttackReady = false; 
        yield return new WaitForSeconds(stats.attackCooldown); 
        isAttackReady = true; 
    }

    protected virtual void Die()
    {
        currentState = EnemyState.Dead;
        OnEnemyKilled?.Invoke(this);
        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject); 
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    protected virtual void LogicAttacking() { ChangeState(EnemyState.Chasing); }

    // Visuals
    IEnumerator FlashSpriteRoutine(Color color)
    {
        Color original = Color.white;
        spriteRenderer.color = color;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    private Color GetDamageFlashColor(DamageElement element)
    {
        return element switch
        {
            DamageElement.Fire => new Color(1f, 0.5f, 0f), 
            DamageElement.Poison => Color.green,
            DamageElement.Ice => Color.cyan,
            DamageElement.Psychic => new Color(0.6f, 0.2f, 0.8f),
            _ => Color.red
        };
    }

    IEnumerator StunRoutine(float duration)
    {
        if (currentState == EnemyState.Dead) yield break;
        EnemyState previousState = currentState;
        ChangeState(EnemyState.Stunned);
        yield return new WaitForSeconds(duration);
        if (currentState != EnemyState.Dead) ChangeState(previousState);
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