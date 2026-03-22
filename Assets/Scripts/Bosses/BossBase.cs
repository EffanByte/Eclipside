using UnityEngine;
using System;
using System.Collections;

public abstract class BossBase : EnemyBase
{
    [Header("Boss Configuration")]
    public string bossName;
    public bool isElite = false; // Hard mode bosses
    
    // --- Boss UI Event ---
    // You can subscribe to this in your UIManager to draw the big boss health bar
    public static event Action<BossBase, float, float> OnBossHealthChanged;
    public static event Action<BossBase> OnBossDefeated;

    // --- Attack System ---
    protected int currentPhase = 1;
    protected string currentAttackName = "None"; // Tracks what we are currently casting

    protected override void Start()
    {
        base.Start();
        // Initialize the UI health bar
        OnBossHealthChanged?.Invoke(this, currentHealth, stats.maxHealth);
    }

    // ---------------------------------------------------------
    // OVERRIDE HEALTH SYSTEM FOR UI
    // ---------------------------------------------------------
    public override void ReceiveDamage(DamageInfo dmg)
    {
        base.ReceiveDamage(dmg);
        
        // Notify the Boss UI
        if (currentState != EnemyState.Dead)
        {
            OnBossHealthChanged?.Invoke(this, currentHealth, stats.maxHealth);
            CheckPhaseTransitions();
        }
    }

    public override void StatusDamage(DamageInfo dmg)
    {
        base.StatusDamage(dmg);
        
        if (currentState != EnemyState.Dead)
        {
            OnBossHealthChanged?.Invoke(this, currentHealth, stats.maxHealth);
            CheckPhaseTransitions();
        }
    }

    public void Heal(float amount)
    {
        if (currentState == EnemyState.Dead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, stats.maxHealth);
        
        OnBossHealthChanged?.Invoke(this, currentHealth, stats.maxHealth);
        StartCoroutine(statusMgr.FlashSpriteRoutine(DamageElement.True)); // Green flash or default
    }

    // ---------------------------------------------------------
    // BOSS LOGIC PIPELINE
    // ---------------------------------------------------------

    // Hook for child classes to change phases when HP drops (e.g. 70%, 40%)
    protected virtual void CheckPhaseTransitions() { }

    protected override void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // 1. Bosses decide WHICH attack to do before attacking
        if (isAttackReady && !isAttackRoutineRunning)
        {
            currentAttackName = ChooseNextAttack(distToPlayer);
            
            if (currentAttackName != "None")
            {
                ChangeState(EnemyState.Attacking);
                return;
            }
        }

        // 2. Normal Boss Movement (Kiting, Approaching)
        Vector2 dir = CalculateMovementDirection(distToPlayer);
        if(statusMgr.HasStatus(StatusType.Confusion)) dir = -dir;

        MoveTowardsTarget(dir);
        HandleSpriteRotation(dir);
    }

    // Bosses MUST implement this to decide their next move
    // Return "None" to keep moving, or a string like "VineLance" to attack
    protected abstract string ChooseNextAttack(float distanceToPlayer);

    // ---------------------------------------------------------
    // BOSS ATTACK OVERRIDES
    // ---------------------------------------------------------
    
    // We override the base Sequence to pass the 'currentAttackName' 
    // into the Windup, Execute, and Recovery functions.
    protected override IEnumerator AttackSequence()
    {
        isAttackRoutineRunning = true;
        rb.linearVelocity = Vector2.zero; 

        // 1. Windup specific to the chosen attack
        yield return StartCoroutine(BossAttackWindup(currentAttackName));

        // 2. Execute specific attack
        if (currentState != EnemyState.Dead && !statusMgr.HasStatus(StatusType.Freeze))
        {
            ExecuteBossAttack(currentAttackName);
        }

        // 3. Recovery specific to the chosen attack
        yield return StartCoroutine(BossAttackRecovery(currentAttackName));
        
        StartCoroutine(AttackCooldownRoutine());
        
        isAttackRoutineRunning = false;
        currentAttackName = "None"; // Reset
        ChangeState(EnemyState.Chasing);
    }

    // Child classes must implement these to handle their specific attack timings
    protected abstract IEnumerator BossAttackWindup(string attackName);
    protected abstract void ExecuteBossAttack(string attackName);
    protected abstract IEnumerator BossAttackRecovery(string attackName);

    // Block the old base methods so we don't accidentally use them in Boss scripts
    protected override IEnumerator AttackWindup() { yield return null; }
    protected override void ExecuteAttack() { }
    protected override IEnumerator AttackRecovery() { yield return null; }

    // ---------------------------------------------------------
    // DEATH
    // ---------------------------------------------------------
    protected override void OnDeath()
    {
        base.OnDeath();
        OnBossDefeated?.Invoke(this);
    }
}