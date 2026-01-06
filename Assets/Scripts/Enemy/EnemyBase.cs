using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using Unity.VisualScripting;

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
    protected float originalSpeedMultiplier;

    // Active status end-times
    private readonly Dictionary<StatusType, float> statusEnd = new Dictionary<StatusType, float>();

    // Running coroutines (for DoT etc.)
    private readonly Dictionary<StatusType, Coroutine> statusCo = new Dictionary<StatusType, Coroutine>();

    // Modifiers
    private float damageTakenMultiplier = 1f;         
    private float selfAttackMaxHpPercent = 0f;        
    private bool freezeConfusionThawBonus = false;    

    public static event Action<EnemyBase> OnEnemyKilled;
    private Dictionary<SynergyPair, Action> synergyLibrary;
    // for status effects
    private static readonly WaitForSeconds DotTickWait = new WaitForSeconds(1f);

    protected virtual void Start()
    {   
        textbox = FindObjectOfType<TextMeshProUGUI>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        originalSpeedMultiplier = speedMultiplier;
        currentHealth = stats.maxHealth;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;

        isAttackReady = true;
        ChangeState(EnemyState.Chasing);
        InitializeSynergies(); 
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
            rb.AddForce(knockbackDir * dmg.knockbackForce, ForceMode2D.Impulse);
            StartCoroutine(KnockbackRoutine(0.2f));
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
// Same as ReceiveDamage but no knockback and no apply status effect
    public virtual void StatusDamage(DamageInfo dmg)
        {
            if (currentState == EnemyState.Dead) return;

            // 1. Apply Immediate Damage (Physical Calculation)
            float finalDamage = dmg.amount * damageTakenMultiplier;
            currentHealth -= finalDamage;
            
            Debug.Log($"{name} Hit by {dmg.element}. HP: {currentHealth}");

            Color flashColor = GetDamageFlashColor(dmg.element);
            StartCoroutine(FlashSpriteRoutine(flashColor)); 

            if (currentHealth <= 0f) Die();
        }
private void InitializeSynergies()
{
    synergyLibrary = new Dictionary<SynergyPair, Action>();

    // Helper to add recipes easily
    void AddSynergy(StatusType s1, StatusType s2, Action effect)
    {
        synergyLibrary.Add(new SynergyPair(s1, s2), effect);
    }

    // --- DEFINE YOUR COMBOS ---
    
    AddSynergy(StatusType.Burn, StatusType.Poison, () => {
        LogCombat("SYNERGY: Explosion + DoT");
        StartDot(StatusType.Poison, dps: 2f, duration: 3f);
        // Add explosion prefab logic here
    });

    AddSynergy(StatusType.Burn, StatusType.Freeze, () => {
        LogCombat("SYNERGY: Thermal Shock (Instant Dmg + Fragile)");
        currentHealth -= 20f;
        ApplyFragile(3f);
    });

    AddSynergy(StatusType.Burn, StatusType.Confusion, () => {
        LogCombat("SYNERGY: Self-Harm Mode");
        StartDot(StatusType.Burn, dps: 3f, duration: 3f);
        selfAttackMaxHpPercent = 0.05f;
        StartCoroutine(ResetSelfAttackBonus(3f));
    });

    AddSynergy(StatusType.Poison, StatusType.Freeze, () => {
        LogCombat("SYNERGY: Strong DoT");
        StartDot(StatusType.Poison, dps: 2f, duration: 5f);
        ApplyFreeze(3f, originalSpeedMultiplier);
    });

    AddSynergy(StatusType.Poison, StatusType.Confusion, () => {
        LogCombat("SYNERGY: Extended Confusion + Poison");
        ApplyConfusion(4.5f);
        StartDot(StatusType.Poison, dps: 2f, duration: 5f);
    });

    AddSynergy(StatusType.Freeze, StatusType.Confusion, () => {
        LogCombat("SYNERGY: Thaw Nightmare");
        ApplyFreeze(3f, originalSpeedMultiplier);
        freezeConfusionThawBonus = true;
    });
}

public void TryAddStatus(StatusType incoming)
{
    // 1. Loop through all currently ACTIVE statuses on this enemy
    // (We copy keys to a list to avoid "Collection Modified" errors if we remove one)
    List<StatusType> currentStatuses = new List<StatusType>(statusEnd.Keys);

    foreach (StatusType existing in currentStatuses)
    {
        // 2. Check if active?
        if (!HasStatus(existing)) continue;

        // 3. Create a pair and check the library
        SynergyPair pair = new SynergyPair(incoming, existing);

        if (synergyLibrary.ContainsKey(pair))
        {
            // FOUND A SYNERGY!
            
            // A. Execute the specific code defined in InitializeSynergies
            synergyLibrary[pair].Invoke();

            // B. Consume the OLD status (Standard mechanic)
            ClearStatus(existing);

            // C. Return early (So we don't apply the NEW status base effect)
            return; 
        }
    }
    // 4. No synergy found? Just apply the status normally.
    Debug.Log("Applying Base status, no synergy found.");
    ApplyBaseStatus(incoming);
}

    // --- BASE STATUS APPLICATION ---

    private void ApplyBaseStatus(StatusType type)
    {
        switch (type)
        {
            case StatusType.Burn: ApplyBurn(3f); break;
            case StatusType.Poison: ApplyPoison(3f); break;
            case StatusType.Freeze: ApplyFreeze(3f, originalSpeedMultiplier); break;
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
            StatusDamage(new DamageInfo(
                amount: dps * tick,
                element: key == StatusType.Burn ? DamageElement.Fire :
                         key == StatusType.Poison ? DamageElement.Poison : DamageElement.True,
                style: AttackStyle.Environment,
                sourcePosition: transform.position
            ));
            if (currentHealth <= 0f) { Die(); yield break; }
            yield return DotTickWait;
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
    private void ApplyFreeze(float duration, float currentSpeed)
    {
        SetStatus(StatusType.Freeze, duration); 
        StartCoroutine(FreezeRoutine(duration, currentSpeed));;
    }
    private void ApplyConfusion(float duration) => SetStatus(StatusType.Confusion, duration);
    private void ApplyFragile(float duration)
    {
        damageTakenMultiplier = 1.2f;
        SetStatus(StatusType.Fragile, duration);
    }

    private IEnumerator FreezeRoutine(float duration, float currentSpeed)
    {
        speedMultiplier = 0.6f; // hard-coded for now
        Debug.Log("Decreased speed");

        yield return new WaitForSeconds(duration);

        // Only restore if Freeze really ended
        if (!HasStatus(StatusType.Freeze))
        {
            speedMultiplier = currentSpeed;
            Debug.Log("Speed back to normal");
        }
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