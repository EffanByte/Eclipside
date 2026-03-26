using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityHFSM; // Requires UnityHFSM package

public enum StatType 
{ 
    AttackSpeed, BaseDamage, MagicDamage, HeavyDamage, MaxHealth,
    CritChance, CritDamage, Luck, Defense, Speed, projectileSpeed
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InventoryManager))] 
public class PlayerController : MonoBehaviour
{
    // --- STATE MACHINE ---
    private StateMachine fsm;

    [Header("--- Stats (Page 1) ---")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float dashForce = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float luck = 0;
    public float playerAttackSpeedMultiplier = 1f; 

    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactionLayer;
    
    [Header("--- Progression ---")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 10f;
    public int rupees = 100;
    public int keys = 0;

    [Header("--- Combat Setup ---")]
    public WeaponData currentWeapon; 
    public Transform weaponObject;
    private WeaponHitbox currentWeaponHitBox; 
    private StatusManager statusMgr;
    private PlayerHealth healthComp;

    [Header("--- Special Ability ---")]
    public float specialChargeMax = 100f;
    private float currentSpecialCharge = 0f;
    private bool isSpecialReady = false;

    // --- Internal State ---
    private SpecialMeterFill specialMeterFill;
    private Rigidbody2D rb;
    private Vector2 rawInputMovement;
    
    // State Flags for FSM
    public bool isDashing = false;         // Locked in dash
    private bool isAttacking = false;       // Locked in attack animation
    private bool dashRequested = false;     // Input buffer
    private bool isAttackPressed = false;   // Input hold
    public bool isWalking = false;         // Locomotion Toggle
    private bool isDead = false;

    // --- Buff State ---
    private float defaultMovementSpeed;
    private float defaultDashForce;
    private float defaultDashDuration;
    private float baseMovementSpeed; 
    private float globalDamageMultiplier = 1f;
    private float lightDamageMultiplier = 1f;
    private float magicDamageMultiplier = 1f;
    private float heavyDamageMultiplier = 1f;
    private float lightAttackSpeedMultiplier = 1f;
    private float magicAttackSpeedMultiplier = 1f;
    private float heavyAttackSpeedMultiplier = 1f;
    private float critChanceMultiplier = 1f;
    private float critChanceFlatBonus = 0f;
    private float critDamageMultiplier = 1f;
    private float projectileSpeedMultiplier = 1f;
    private float characterDashDistanceMultiplier = 1f;
    private float pendingLuckRupeeFraction = 0f;
    private bool hasLuck = false; 
    private bool isLuckLocked = false;
    private bool isReviveInvulnerable = false;
    private bool hasPendingRevive = false;
    private float pendingReviveHealthFraction = 0.25f;
    private float pendingReviveInvulnerabilityDuration = 5f;

    // A class to store exactly what a buff did, so we can cleanly reverse it
    private class ActiveBuff
    {
        public StatType Type;
        public float FlatAmountAdded; // Stores the exact math result to prevent percentage drift
        public Coroutine TimerCoroutine;
    }

    private class ActiveWaveEffect
    {
        public int WavesRemaining;
        public Action ExpireAction;
    }

    private Dictionary<string, ActiveBuff> activeBuffs = new Dictionary<string, ActiveBuff>();  
    private Dictionary<string, ActiveWaveEffect> activeWaveEffects = new Dictionary<string, ActiveWaveEffect>();


    // --- References ---
    private PlayerControls controls; 
    private InventoryManager inventory; 
    private PlayerCharacterRuntime characterRuntime;

    // --- Events ---
    public event Action onCurrencyUpdate;
    public event Action OnLevelUp; 
    public event Action<float, float> OnExpChanged; 
    public event Action OnPlayerDeath;

    private float lastAttackTime = -999f; 
    
    public Animator anim; 

    public static PlayerController Instance {get; private set;}

    private void Awake()
    {
        gameObject.SetActive(true); 
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        rb = GetComponent<Rigidbody2D>();
        inventory = GetComponent<InventoryManager>();
        controls = new PlayerControls();
        healthComp = GetComponent<PlayerHealth>();
        PlayerHealth.OnPlayerDeath += PlayerKilled;
        characterRuntime = GetComponent<PlayerCharacterRuntime>();
        if (characterRuntime == null)
        {
            characterRuntime = gameObject.AddComponent<PlayerCharacterRuntime>();
        }
        
        statusMgr = GetComponent<StatusManager>();
        // Ensure StatusManager calls StatusDamage when it triggers DOTs
        statusMgr.Initialize(rb, this, StatusDamage, GetComponentInChildren<SpriteRenderer>());
        
        defaultMovementSpeed = movementSpeed;
        defaultDashForce = dashForce;
        defaultDashDuration = dashDuration;
        baseMovementSpeed = movementSpeed;
        hasLuck = luck > 0f && !isLuckLocked;

        // --- INPUT BINDINGS ---
        controls.Player.Move.performed += ctx => rawInputMovement = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => rawInputMovement = Vector2.zero;
        
        // Attack (Hold to Auto-Attack logic)
        controls.Player.Fire.started += ctx => isAttackPressed = true;
        controls.Player.Fire.canceled += ctx => isAttackPressed = false;
        
        // Dash (Buffer the input)
        controls.Player.Dash.performed += ctx => dashRequested = true;
        
        controls.Player.Special.performed += ctx => AttemptSpecial();
        controls.Player.Interact.performed += ctx => AttemptInteract(); 

        // Items
        controls.Player.Item1.performed += ctx => inventory.TriggerItemUse(0); 
        controls.Player.Item2.performed += ctx => inventory.TriggerItemUse(1); 
        controls.Player.Item3.performed += ctx => inventory.TriggerItemUse(2); 
    }

    private void Start()
    {
        specialMeterFill = FindFirstObjectByType<SpecialMeterFill>();
        characterRuntime.Initialize(this, healthComp, statusMgr);
        characterRuntime.ApplyEquippedCharacter();

        if (currentWeapon != null && (weaponObject == null || weaponObject.childCount == 0))
        {
            EquipWeapon(currentWeapon);
        }

        onCurrencyUpdate?.Invoke();

        if (GameDirector.Instance != null)
        {
            GameDirector.Instance.OnWaveAdvanced += HandleWaveTransition;
            GameDirector.Instance.OnLevelCompleted += HandleWaveTransition;
        }

        // Initialize HFSM
        InitializeStateMachine();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void OnDestroy()
    {
        PlayerHealth.OnPlayerDeath -= PlayerKilled;

        if (GameDirector.Instance != null)
        {
            GameDirector.Instance.OnWaveAdvanced -= HandleWaveTransition;
            GameDirector.Instance.OnLevelCompleted -= HandleWaveTransition;
        }
    }

    // ---------------------------------------------------------
    // STATE MACHINE SETUP
    // ---------------------------------------------------------
    private void InitializeStateMachine()
    {
        fsm = new StateMachine();

        // 1. IDLE STATE
        fsm.AddState("Idle", new State(
            onEnter: (state) =>
            {
                anim.CrossFade("Idle", 0.2f);  
            },
            onLogic: (state) => 
            {
                rb.linearVelocity = Vector2.zero;
            }
        ));


        // This handles both Walking and Running
        StateMachine locomotionFsm = new StateMachine();
        // Sub-State: RUN (Default)
        locomotionFsm.AddState("Run", new State(
            onEnter: (state) =>
            {
                anim.CrossFade("Running", 0.3f);
            },
            onLogic: (state) => MoveLogic(1.0f) // Full Speed
        ));

        // Sub-State: WALK
        locomotionFsm.AddState("Walk", new State(
            onEnter: (state) =>
            {
                anim.CrossFade("Walking", 0.3f);
            }
        ));

        // Transitions between Walk/Run inside Locomotion
        locomotionFsm.AddTransition("Run", "Walk", t => isWalking);
        locomotionFsm.AddTransition("Walk", "Run", t => !isWalking);

        // Add the Sub-Machine to Main Machine
        locomotionFsm.SetStartState("Run");
        fsm.AddState("Locomotion", locomotionFsm);


        // 3. DASH STATE
        fsm.AddState("Dash", new State(
            onEnter: (state) => 
            {
                dashRequested = false; // Consume Input
               // anim.SetTrigger("Dash");
                StartCoroutine(DashRoutine());
            }
        ));

// 3. ATTACK STATE
    fsm.AddState("Attack", new State(
        onEnter: (state) => 
        {
            anim.CrossFade("Attack", 0.1f, -1, 0f);
            StartCoroutine(AttackRoutine());
        },
        onLogic: (state) => 
        {
            float penalty = 0.8f;
            MoveLogic(penalty); 
        }
    ));

        // --- MAIN TRANSITIONS ---

        // Idle <-> Locomotion
        fsm.AddTransition("Idle", "Locomotion", t => rawInputMovement != Vector2.zero);
        fsm.AddTransition("Locomotion", "Idle", t => rawInputMovement == Vector2.zero);

        // Any -> Dash (Priority)
        fsm.AddTransitionFromAny("Dash", t => 
            dashRequested && !isDashing && !statusMgr.IsFrozen
        );

        // Any -> Attack
        fsm.AddTransitionFromAny("Attack", t => 
            isAttackPressed && CanAttackCheck() && !isDashing && !statusMgr.IsFrozen
        );

        // Exit Dash (When routine finishes)
        fsm.AddTransition("Dash", "Idle", t => !isDashing);

        // Exit Attack (When animation/coroutine finishes)
        fsm.AddTransition("Attack", "Idle", t => !isAttacking);

        // Start the FSM
        fsm.Init();
    }

    private void Update()
    {
        characterRuntime?.ManualUpdate(Time.deltaTime);
        if (isDead) return;

        // Tick the State Machine
            fsm.OnLogic();
    }

    // ---------------------------------------------------------
    // ACTION ROUTINES (Called by FSM)
    // ---------------------------------------------------------

    // 1. MOVEMENT LOGIC
    private void MoveLogic(float speedMultiplier)
    {
        if (statusMgr.IsFrozen) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 finalInput = rawInputMovement;
        if (statusMgr.IsConfused) finalInput *= -1; 
        
        // Calculate speed based on Stats AND State (Walk vs Run)
        float currentSpeed = movementSpeed * speedMultiplier;

        // FLIP LOGIC
        if (rawInputMovement.x > 0) transform.localScale = new Vector3(0.42f, 0.42f, 1);
        else if (rawInputMovement.x < 0) transform.localScale = new Vector3(-0.42f, 0.42f, 1);
    
        rb.linearVelocity = finalInput * currentSpeed;
    }

    // 2. DASH LOGIC
    private IEnumerator DashRoutine()
    {
        isDashing = true; // Locks State
        characterRuntime?.NotifyDashStarted();
        
        Vector2 dashDir = rawInputMovement == Vector2.zero ? Vector2.right : rawInputMovement.normalized;
        float dashDistanceMultiplier = characterDashDistanceMultiplier;
        if (characterRuntime != null)
        {
            dashDistanceMultiplier *= characterRuntime.GetDashDistanceMultiplier();
        }
        rb.linearVelocity = dashDir * (dashForce * dashDistanceMultiplier);
        
        yield return new WaitForSeconds(dashDuration);
        
        rb.linearVelocity = Vector2.zero; 
        isDashing = false; // Unlocks State -> FSM transitions to Idle
        characterRuntime?.NotifyDashEnded();
    }

    // 3. ATTACK LOGIC
    private bool CanAttackCheck()
    {
        if (currentWeapon == null) return false;
        float actualCooldown = currentWeapon.Cooldown / GetAttackSpeedMultiplierForWeapon(currentWeapon);
        return Time.time >= lastAttackTime + actualCooldown;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true; // Locks State
        lastAttackTime = Time.time;

        // Execute Weapon Logic (Visuals + Hitbox)
        // WeaponData.OnAttack returns an IEnumerator that waits for hitDuration
        yield return StartCoroutine(currentWeapon.OnAttack(this, currentWeaponHitBox));

        // Charge Special
        OnDealtDamage(5f); 

        isAttacking = false; // Unlocks State -> FSM transitions to Idle
    }



    // ---------------------------------------------------------
    // HELPERS & PUBLIC API
    // ---------------------------------------------------------

    // Call this to toggle Walk mode
    public void SetWalking(bool walking)
    {
        isWalking = walking;
    }

    public void ResetPlayer()
    {
        gameObject.SetActive(true);
        transform.position = Vector2.zero;
        ClearActiveWaveEffects();
        healthComp.Heal(healthComp.GetMaxHealth());
        isDead = false;
        pendingLuckRupeeFraction = 0f;
        isReviveInvulnerable = false;
        hasPendingRevive = false;
        
        // Reset Logic State
        isDashing = false;
        isAttacking = false;
        fsm?.RequestStateChange("Idle");
    }

    public Vector2 GetLastMovementDirection()
    {
        if (rawInputMovement != Vector2.zero) return rawInputMovement;
        return transform.localScale.x < 0 ? Vector2.left : Vector2.right; 
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (ChallengeManager.theGladiator && newWeapon is MagicWeapon)
        {
            Debug.Log("Can't equip weapon due to gladiator");
            return;
        }

        if (characterRuntime != null && !characterRuntime.CanEquipWeapon(newWeapon))
        {
            Debug.Log($"[Character] {characterRuntime.GetActiveCharacterName()} cannot equip {newWeapon?.name ?? "null"}.");
            return;
        }

        if (currentWeapon != null)
        {
            currentWeapon.Cleanup(this);
        }

        currentWeapon = newWeapon;

        if (weaponObject.childCount > 0)
        {
            foreach (Transform child in weaponObject) Destroy(child.gameObject);
        }

        if (currentWeapon != null && currentWeapon.weaponPrefab != null)
        {
            GameObject spawnedWeapon = Instantiate(currentWeapon.weaponPrefab, weaponObject);
            currentWeaponHitBox = spawnedWeapon.GetComponent<WeaponHitbox>();
            
            if (currentWeapon.animatorOverride != null)
            {
                anim.runtimeAnimatorController = currentWeapon.animatorOverride;
            }
        }
        else
        {
            currentWeaponHitBox = CreateFallbackWeaponHitbox(currentWeapon);
        }

        if (currentWeapon != null)
        {
            currentWeapon.Initialize(this);
        }

        characterRuntime?.NotifyWeaponEquipped(currentWeapon);
    }

    private WeaponHitbox CreateFallbackWeaponHitbox(WeaponData weapon)
    {
        if (weaponObject == null || weapon == null || weapon is MagicWeapon)
        {
            return null;
        }

        GameObject hitboxObject = new GameObject($"{weapon.name}_RuntimeHitbox");
        hitboxObject.transform.SetParent(weaponObject, false);
        hitboxObject.transform.localPosition = weapon is HeavyMelee ? new Vector3(1.05f, 0f, 0f) : new Vector3(0.8f, 0f, 0f);

        BoxCollider2D collider = hitboxObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = weapon is HeavyMelee ? new Vector2(1.7f, 1.25f) : new Vector2(1.1f, 0.95f);

        return hitboxObject.AddComponent<WeaponHitbox>();
    }

    // --- Special Ability ---
    public void OnDealtDamage(float damageAmount)
    {
        if (characterRuntime != null && characterRuntime.HandlesSpecialCooldowns())
        {
            characterRuntime.NotifyDamageDealt(damageAmount);
            return;
        }

        if (!isSpecialReady)
        {
            if (specialMeterFill) specialMeterFill.SetValue(currentSpecialCharge);
            currentSpecialCharge += damageAmount;
            if (currentSpecialCharge >= specialChargeMax)
            {
                currentSpecialCharge = specialChargeMax;
                isSpecialReady = true;
                Debug.Log("Special Ability Ready!"); 
            }
        }
    }

    private void AttemptSpecial()
    {
        if (isDead)
        {
            return;
        }

        if (characterRuntime != null && characterRuntime.HandlesSpecialCooldowns())
        {
            characterRuntime.TryActivateSpecial();
            return;
        }

        if (isSpecialReady) ActivateSpecialAbility();
    }

    private void ActivateSpecialAbility()
    {
        if (characterRuntime != null && characterRuntime.HandlesSpecialCooldowns())
        {
            characterRuntime.TryActivateSpecial();
            return;
        }

        Debug.Log("SPECIAL ABILITY UNLEASHED!");
        currentSpecialCharge = 0;
        isSpecialReady = false;
    }

    // --- Interaction ---
    private void AttemptInteract()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactionLayer);
        IInteractable closest = null;
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = interactable;
                }
            }
        }

        if (closest != null) closest.Interact(this);
    }

    // --- Damage & Status ---
    public void ReceiveDamage(DamageInfo dmg)
    {
        if (isDashing || isReviveInvulnerable) return; // i-frames

        float finalAmount = dmg.amount * statusMgr.DamageTakenMultiplier;
        if (characterRuntime != null)
        {
            finalAmount *= characterRuntime.GetIncomingDamageMultiplier(dmg, false);
        }

        if (dmg.element != DamageElement.True)
        {
            StatusType effect = statusMgr.GetStatusFromElement(dmg.element);
            TryAddStatus(effect);
        }
        
        if (statusMgr != null)
        {
            StartCoroutine(statusMgr.FlashSpriteRoutine(dmg.element));
        }
        healthComp.ReceiveDamage(finalAmount, dmg.element);
        characterRuntime?.NotifyDamageTaken(dmg, finalAmount, false);
        
        WaveManager waveMgr = FindFirstObjectByType<WaveManager>();
        if(waveMgr) waveMgr.TookDamageThisWave();
    }

    public void StatusDamage(DamageInfo dmg)
    {
        if (isReviveInvulnerable) return;

        float finalAmount = dmg.amount * statusMgr.DamageTakenMultiplier;
        if (characterRuntime != null)
        {
            finalAmount *= characterRuntime.GetIncomingDamageMultiplier(dmg, true);
        }
        if (statusMgr != null)
        {
            StartCoroutine(statusMgr.FlashSpriteRoutine(dmg.element));
        }
        healthComp.ReceiveDamage(finalAmount, dmg.element);
        characterRuntime?.NotifyDamageTaken(dmg, finalAmount, true);
    }

    /// <summary>
    /// Applies a temporary buff. Automatically removes it after duration.
    /// </summary>
    public void ApplyBuff(string buffKey, StatType type, float amount, float duration)
    {
        ApplyPermanentBuff(buffKey, type, amount);

        // Start the timer to remove it later, and store the Coroutine
        ActiveBuff buff = activeBuffs[buffKey];
        buff.TimerCoroutine = StartCoroutine(BuffTimerRoutine(buffKey, duration));
    }

    /// <summary>
    /// Applies a permanent buff. Remains until manually removed.
    /// </summary>
    public void ApplyPermanentBuff(string buffKey, StatType type, float amount)
    {
        // 1. If this key already exists, remove the old one first to prevent infinite stacking
        if (activeBuffs.ContainsKey(buffKey))
        {
            RemoveBuff(buffKey);
        }

        float flatChange = 0f;

        // 2. Calculate exact flat change and apply it
        switch (type)
        {
            case StatType.Defense:
                flatChange = amount; // Assuming amount is flat (e.g. 0.15)
                if (statusMgr != null) statusMgr.ChangeDamageMultiplier(-flatChange);
                break;

            case StatType.BaseDamage:
                flatChange = amount;
                globalDamageMultiplier += flatChange;
                break;

            case StatType.MagicDamage:
                flatChange = amount;
                magicDamageMultiplier += flatChange;
                break;

            case StatType.HeavyDamage:
                flatChange = amount;
                heavyDamageMultiplier += flatChange;
                break;

            case StatType.CritChance:
                flatChange = amount;
                critChanceFlatBonus += flatChange;
                break;

            case StatType.CritDamage:
                flatChange = amount;
                critDamageMultiplier += flatChange;
                break;

            case StatType.Luck:
                if (isLuckLocked)
                {
                    Debug.LogWarning($"[Luck] Ignored buff '{buffKey}' because luck is locked.");
                    return;
                }

                flatChange = amount;
                luck += flatChange;
                hasLuck = luck > 0f;
                break;

            case StatType.Speed:
                flatChange = baseMovementSpeed * amount;
                movementSpeed += flatChange;
                break;

            case StatType.AttackSpeed:
                flatChange = amount; // Usually flat addition for multipliers
                playerAttackSpeedMultiplier += flatChange;
                break;

            case StatType.projectileSpeed:
                flatChange = amount;
                projectileSpeedMultiplier += flatChange;
                break;

            case StatType.MaxHealth:
                flatChange = amount;
                if (flatChange > 0f && characterRuntime != null && characterRuntime.TryHandleMaxHealthGain(flatChange))
                {
                    flatChange = 0f;
                }
                else
                {
                    healthComp.SetMaxHealth(healthComp.GetMaxHealth() + flatChange);
                }
                break;
        }

        // 3. Store the buff in the dictionary
        activeBuffs.Add(buffKey, new ActiveBuff 
        { 
            Type = type, 
            FlatAmountAdded = flatChange, 
            TimerCoroutine = null 
        });

        Debug.Log($"[Buffs] Applied '{buffKey}' ({type}): +{flatChange}");
    }

    /// <summary>
    /// Manually removes a buff by its Key.
    /// </summary>
    public void RemoveBuff(string buffKey)
    {
        if (!activeBuffs.TryGetValue(buffKey, out ActiveBuff buff))
        {
            return; // Buff doesn't exist
        }

        // 1. Stop the timer if it's a temporary buff
        if (buff.TimerCoroutine != null)
        {
            StopCoroutine(buff.TimerCoroutine);
        }

        // 2. Revert the exact flat amount that was added
        switch (buff.Type)
        {
            case StatType.Defense:
                if (statusMgr != null) statusMgr.ChangeDamageMultiplier(buff.FlatAmountAdded); 
                break;
            case StatType.BaseDamage:
                globalDamageMultiplier -= buff.FlatAmountAdded;
                break;
            case StatType.MagicDamage:
                magicDamageMultiplier -= buff.FlatAmountAdded;
                break;
            case StatType.HeavyDamage:
                heavyDamageMultiplier -= buff.FlatAmountAdded;
                break;
            case StatType.CritChance:
                critChanceFlatBonus -= buff.FlatAmountAdded;
                break;
            case StatType.CritDamage:
                critDamageMultiplier -= buff.FlatAmountAdded;
                break;
            case StatType.Luck:
                luck -= buff.FlatAmountAdded;
                hasLuck = luck > 0f && !isLuckLocked;
                break;
            case StatType.Speed:
                movementSpeed -= buff.FlatAmountAdded;
                break;
            case StatType.AttackSpeed:
                playerAttackSpeedMultiplier -= buff.FlatAmountAdded;
                break;
            case StatType.projectileSpeed:
                projectileSpeedMultiplier -= buff.FlatAmountAdded;
                break;
            case StatType.MaxHealth:
                if (buff.FlatAmountAdded != 0f)
                {
                    healthComp.SetMaxHealth(healthComp.GetMaxHealth() - buff.FlatAmountAdded);
                }
                break;
        }

        // 3. Remove from tracking
        activeBuffs.Remove(buffKey);
        Debug.Log($"[Buffs] Removed '{buffKey}'. Reversed: {buff.FlatAmountAdded}");
    }

    public void ApplyBuffForWaves(string buffKey, StatType type, float amount, int waveCount)
    {
        ApplyPermanentBuff(buffKey, type, amount);
        RegisterWaveEffect(buffKey, waveCount, () => RemoveBuff(buffKey));
    }

    public void AddTemporaryHeartsForWaves(string effectKey, float amount, int waveCount)
    {
        healthComp.AddTemporaryHearts(amount);
        RegisterWaveEffect(effectKey, waveCount, () => healthComp.RemoveTemporaryHearts(amount));
    }

    public void ArmRevive(string sourceKey, float healthFraction, float invulnerabilityDuration)
    {
        hasPendingRevive = true;
        pendingReviveHealthFraction = Mathf.Max(0.01f, healthFraction);
        pendingReviveInvulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
        Debug.Log($"[Player] Armed revive from {sourceKey} with {pendingReviveHealthFraction:P0} health and {pendingReviveInvulnerabilityDuration}s invulnerability.");
    }

    public bool TryTriggerRevive()
    {
        if (!hasPendingRevive)
        {
            return false;
        }

        hasPendingRevive = false;
        isDead = false;

        float reviveHealth = Mathf.Max(1f, healthComp.GetMaxHealth() * pendingReviveHealthFraction);
        healthComp.ReviveToHealth(reviveHealth);

        if (pendingReviveInvulnerabilityDuration > 0f)
        {
            StartCoroutine(ReviveInvulnerabilityRoutine(pendingReviveInvulnerabilityDuration));
        }

        Debug.Log($"[Player] Revived with {reviveHealth} health.");
        return true;
    }

    public float GetDamageMultiplierForWeapon(WeaponData weapon)
    {
        float multiplier = globalDamageMultiplier;

        if (weapon is MagicWeapon)
        {
            multiplier *= magicDamageMultiplier;
        }
        else if (weapon is HeavyMelee)
        {
            multiplier *= heavyDamageMultiplier;
        }
        else
        {
            multiplier *= lightDamageMultiplier;
        }

        if (characterRuntime != null)
        {
            multiplier *= characterRuntime.GetCharacterDamageMultiplierForWeapon(weapon);
        }

        return multiplier;
    }

    public float GetAttackSpeedMultiplierForWeapon(WeaponData weapon)
    {
        float multiplier = playerAttackSpeedMultiplier;

        if (weapon is MagicWeapon)
        {
            multiplier *= magicAttackSpeedMultiplier;
        }
        else if (weapon is HeavyMelee)
        {
            multiplier *= heavyAttackSpeedMultiplier;
        }
        else
        {
            multiplier *= lightAttackSpeedMultiplier;
        }

        if (characterRuntime != null)
        {
            multiplier *= characterRuntime.GetCharacterAttackSpeedMultiplierForWeapon(weapon);
        }

        return Mathf.Max(0.01f, multiplier);
    }

    public float GetCriticalChanceForWeapon(WeaponData weapon, float weaponCritChance)
    {
        float finalChance = (weaponCritChance * critChanceMultiplier) + critChanceFlatBonus;

        if (characterRuntime != null)
        {
            finalChance = characterRuntime.GetCharacterCriticalChanceForWeapon(weapon, finalChance);
        }

        return Mathf.Clamp(finalChance, 0f, 100f);
    }

    public float GetCriticalDamageMultiplier()
    {
        return Mathf.Max(1f, critDamageMultiplier);
    }

    public float GetProjectileSpeedMultiplier()
    {
        float multiplier = projectileSpeedMultiplier;

        if (characterRuntime != null)
        {
            multiplier *= characterRuntime.GetProjectileSpeedMultiplier();
        }

        return Mathf.Max(0.01f, multiplier);
    }

    private void RegisterWaveEffect(string effectKey, int waveCount, Action expireAction)
    {
        if (string.IsNullOrWhiteSpace(effectKey))
        {
            effectKey = Guid.NewGuid().ToString("N");
        }

        if (activeWaveEffects.TryGetValue(effectKey, out ActiveWaveEffect existing))
        {
            existing.ExpireAction?.Invoke();
            activeWaveEffects.Remove(effectKey);
        }

        activeWaveEffects[effectKey] = new ActiveWaveEffect
        {
            WavesRemaining = Mathf.Max(1, waveCount),
            ExpireAction = expireAction
        };
    }

    private void HandleWaveTransition()
    {
        if (activeWaveEffects.Count == 0)
        {
            characterRuntime?.NotifyWaveTransition();
            return;
        }

        List<string> expiredKeys = new List<string>();
        foreach (var entry in activeWaveEffects)
        {
            entry.Value.WavesRemaining -= 1;
            if (entry.Value.WavesRemaining <= 0)
            {
                expiredKeys.Add(entry.Key);
            }
        }

        foreach (string key in expiredKeys)
        {
            if (!activeWaveEffects.TryGetValue(key, out ActiveWaveEffect effect))
            {
                continue;
            }

            effect.ExpireAction?.Invoke();
            activeWaveEffects.Remove(key);
        }

        characterRuntime?.NotifyWaveTransition();
    }

    private void ClearActiveWaveEffects()
    {
        if (activeWaveEffects.Count == 0)
        {
            return;
        }

        foreach (var effect in activeWaveEffects.Values)
        {
            effect.ExpireAction?.Invoke();
        }

        activeWaveEffects.Clear();
    }

    // ---------------------------------------------------------
    // INTERNAL COROUTINES
    // ---------------------------------------------------------

    private IEnumerator BuffTimerRoutine(string buffKey, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // Once time is up, use the central Remove method
        RemoveBuff(buffKey);
    }

    private IEnumerator ReviveInvulnerabilityRoutine(float duration)
    {
        isReviveInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isReviveInvulnerable = false;
    }

    // ---------------------------------------------------------
    // LEGACY HELPERS (Wrappers to keep old code working)
    // ---------------------------------------------------------
    
    public void ModifySpeed(float percentageAmount)
    {
        // Generates a random key so it acts like an untracked permanent buff
        ApplyPermanentBuff("SpeedMod_" + System.Guid.NewGuid().ToString(), StatType.Speed, percentageAmount);
    }

    public void ModifyPlayerStat(StatType statType, float value)
    {
        ApplyPermanentBuff("StatMod_" + System.Guid.NewGuid().ToString(), statType, value);
    }

    public void AddExperience(float amount)
    {
        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        if (currentExp >= expToNextLevel) LevelUp();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel;
        expToNextLevel *= 1.2f;
        OnLevelUp?.Invoke();
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
    }

    public void ApplyPermanentUpgrade(StatType stat)
    {
        switch (stat)
        {
            case StatType.BaseDamage: globalDamageMultiplier += 0.1f; break;
            case StatType.MagicDamage: magicDamageMultiplier += 0.1f; break;
            case StatType.HeavyDamage: heavyDamageMultiplier += 0.1f; break;
            case StatType.CritChance: critChanceFlatBonus += 5f; break;
            case StatType.CritDamage: critDamageMultiplier += 0.1f; break;
            case StatType.Luck:
                if (!isLuckLocked)
                {
                    luck += 1f;
                    hasLuck = luck > 0f;
                }
                break;
            case StatType.MaxHealth: healthComp.ModifyMaxHealth(1); healthComp.Heal(1f); break; // +1 Heart
            case StatType.Speed: baseMovementSpeed *= 1.05f; movementSpeed = baseMovementSpeed; break;
            case StatType.AttackSpeed: playerAttackSpeedMultiplier += 0.05f; break;
            case StatType.projectileSpeed: projectileSpeedMultiplier += 0.1f; break;
        }
    }

    public void TryAddStatus(StatusType effect) => statusMgr.TryAddStatus(effect);
    public void TryAddStatus(StatusType effect, float durationOverride) => statusMgr.TryAddStatus(effect, durationOverride);
    public float GetMaxHealth() => healthComp.GetMaxHealth();
    public bool IsDead => isDead;
    public float GetLuckValue() => hasLuck && !isLuckLocked ? Mathf.Max(0f, luck) : 0f;
    public float GetStatusProcChanceBonus() => LuckUtility.GetStatusProcChanceBonus(GetLuckValue());
    public float GetOutgoingStatusDuration(StatusType statusType, float baseDuration)
    {
        if (characterRuntime == null)
        {
            return baseDuration;
        }

        return characterRuntime.GetOutgoingStatusDuration(statusType, baseDuration);
    }

    public void NotifyWeaponHit(EnemyBase target, DamageInfo damageInfo)
    {
        characterRuntime?.NotifyWeaponHit(target, damageInfo);
    }

    public void Heal(float amount)
    {
        if (characterRuntime != null && characterRuntime.TryHandleHealing(amount))
        {
            return;
        }

        healthComp.Heal(amount);
    }
    public void AddTemporaryHearts(float amount) => healthComp.AddTemporaryHearts(amount);
    public void RemoveTemporaryHearts(float amount) => healthComp.RemoveTemporaryHearts(amount);
    
    public void ToggleLuck(bool state)
    {
        hasLuck = state && !isLuckLocked && luck > 0f;
        Debug.Log($"Luck enabled: {hasLuck} (value: {GetLuckValue():0.##})");
    }

    public void LockLuck()
    {
        isLuckLocked = true;
        hasLuck = false;
        Debug.Log("[Luck] Luck is now locked.");
    }

    public float GetBaseDamage()
    {
        if (currentWeapon != null) return currentWeapon.damage;
        Debug.Log("No weapon equipped, returning unarmed base damage.");
        return 1f; // Unarmed base damage
    }
    public void SetLuck(int value)
    {
        if (!isLuckLocked)
        {
            luck = value;
            hasLuck = luck > 0f;
        }
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        int finalAmount = amount;

        if (type == CurrencyType.Rupee && amount > 0)
        {
            finalAmount = ApplyLuckToRupeeGain(amount);
        }

        switch (type) {
            case CurrencyType.Rupee: rupees += finalAmount; break;
            case CurrencyType.Key: keys += finalAmount; break;
        }
        onCurrencyUpdate?.Invoke();
    }

    public void ApplyCharacterBaseStats(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        baseMovementSpeed = defaultMovementSpeed * Mathf.Max(0.01f, character.moveSpeed);
        movementSpeed = baseMovementSpeed;
        dashForce = defaultDashForce;
        dashDuration = defaultDashDuration;
        playerAttackSpeedMultiplier = Mathf.Max(0.01f, character.attackSpeedMultiplier);
        globalDamageMultiplier = Mathf.Max(0.01f, character.damageMultiplier);
        lightDamageMultiplier = Mathf.Max(0.01f, character.lightDamageMultiplier);
        magicDamageMultiplier = Mathf.Max(0.01f, character.magicDamageMultiplier);
        heavyDamageMultiplier = Mathf.Max(0.01f, character.heavyDamageMultiplier);
        lightAttackSpeedMultiplier = Mathf.Max(0.01f, character.lightAttackSpeedMultiplier);
        magicAttackSpeedMultiplier = Mathf.Max(0.01f, character.magicAttackSpeedMultiplier);
        heavyAttackSpeedMultiplier = Mathf.Max(0.01f, character.heavyAttackSpeedMultiplier);
        critChanceMultiplier = Mathf.Max(0.01f, character.critChanceMultiplier);
        critChanceFlatBonus = character.critChanceFlatBonus;
        projectileSpeedMultiplier = Mathf.Max(0.01f, character.projectileSpeedMultiplier);
        characterDashDistanceMultiplier = Mathf.Max(0.01f, character.dashDistanceMultiplier);

        if (healthComp != null)
        {
            healthComp.SetMaxHealth(character.maxHealth);
            healthComp.Heal(character.maxHealth);
        }
    }

    public void ConfigureBaseMovementSpeed(float speed)
    {
        baseMovementSpeed = speed;
        movementSpeed = speed;
    }

    public float GetCurrentMovementSpeed() => movementSpeed;
    public float GetDashForce() => dashForce;
    public float GetDashDuration() => dashDuration;
    public float GetCurrentHealth() => healthComp.GetCurrentHealth();
    public StatusManager GetStatusManager() => statusMgr;
    public PlayerHealth GetHealthComponent() => healthComp;
    public void SetSpecialMeterNormalized(float normalized)
    {
        if (specialMeterFill == null)
        {
            return;
        }

        specialMeterFill.SetValue(Mathf.Clamp01(normalized) * 100f);
    }

    private int ApplyLuckToRupeeGain(int baseAmount)
    {
        float currentLuck = GetLuckValue();
        if (currentLuck <= 0f || baseAmount <= 0)
        {
            return baseAmount;
        }

        float bonusFraction = (baseAmount * (LuckUtility.GetRupeeMultiplier(currentLuck) - 1f)) + pendingLuckRupeeFraction;
        int bonusRupees = Mathf.FloorToInt(bonusFraction);
        pendingLuckRupeeFraction = bonusFraction - bonusRupees;

        if (bonusRupees > 0)
        {
            Debug.Log($"[Luck] Added +{bonusRupees} bonus rupees from {currentLuck:0.##} luck on a base reward of {baseAmount}.");
        }

        return baseAmount + bonusRupees;
    }

    public bool DeductCurrency(CurrencyType type, int amount)
    {
        switch (type) {
            case CurrencyType.Rupee:
                if (rupees - amount >= 0)
                {
                    rupees -= amount;
                    Debug.Log("Money deducted by" + amount);
                    onCurrencyUpdate?.Invoke();
                    return true;
                }
                else
                {
                    Debug.Log("Not enough Rupees!");
                    return false;
                }
            case CurrencyType.Key:
                if (keys - amount >= 0)
                {
                    keys -= amount;
                    Debug.Log("Keys deducted by" + amount);
                    onCurrencyUpdate?.Invoke();
                    return true;
                }
                else
                {
                    Debug.Log("Not enough Keys!");
                    return false;
                }
            default:
                return false;
        }
    }

    public void ApplyPureKnockback(Vector2 sourcePosition, float force)
    {
        if (isDashing) return; // i-frames ignore wind/push too
        
        Vector2 dir = ((Vector2)transform.position - sourcePosition).normalized;
        rb.linearVelocity = Vector2.zero; // Reset momentum
        rb.AddForce(dir * force, ForceMode2D.Impulse);
    }
    
    public void PlayerKilled()
    {
        isDead = true;
        OnPlayerDeath?.Invoke();
    }
}
