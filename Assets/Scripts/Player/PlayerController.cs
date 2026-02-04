using UnityEngine;
using System;
using System.Collections;
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
    private bool isDashing = false;         // Locked in dash
    private bool isAttacking = false;       // Locked in attack animation
    private bool dashRequested = false;     // Input buffer
    private bool isAttackPressed = false;   // Input hold
    private bool isWalking = false;         // Locomotion Toggle
    private bool isDead = false;

    // --- Buff State ---
    private float baseMovementSpeed; 
    private bool hasLuck = false; 
    private bool isLuckLocked = false;

    // --- References ---
    private PlayerControls controls; 
    private InventoryManager inventory; 

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
        
        statusMgr = GetComponent<StatusManager>();
        // Ensure StatusManager calls StatusDamage when it triggers DOTs
        statusMgr.Initialize(rb, this, StatusDamage, GetComponentInChildren<SpriteRenderer>());
        
        baseMovementSpeed = movementSpeed;

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
        EquipWeapon(currentWeapon);
        onCurrencyUpdate?.Invoke();

        // Initialize HFSM
        InitializeStateMachine();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

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
            // Keep moving! 
            // The Base Layer will handle switching "Run" <-> "Idle" visuals based on speed
            // The Attack Layer will override the arms.
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
        if (isDead) return;

        // Tick the State Machine
            fsm.OnLogic();
        Debug.Log(fsm.ActiveStateName);
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
        
        Vector2 dashDir = rawInputMovement == Vector2.zero ? Vector2.right : rawInputMovement.normalized;
        rb.linearVelocity = dashDir * dashForce;
        
        yield return new WaitForSeconds(dashDuration);
        
        rb.linearVelocity = Vector2.zero; 
        isDashing = false; // Unlocks State -> FSM transitions to Idle
    }

    // 3. ATTACK LOGIC
    private bool CanAttackCheck()
    {
        if (currentWeapon == null) return false;
        float actualCooldown = currentWeapon.Cooldown / playerAttackSpeedMultiplier;
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
        healthComp.Heal(healthComp.GetMaxHealth());
        isDead = false;
        
        // Reset Logic State
        isDashing = false;
        isAttacking = false;
        fsm?.RequestStateChange("Idle");
    }

    public Vector2 GetLastMovementDirection()
    {
        if (rawInputMovement != Vector2.zero) return rawInputMovement;
        return transform.localScale.x < 0 ? Vector2.right : Vector2.left; 
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (ChallengeManager.theGladiator && newWeapon is MagicWeapon)
        {
            Debug.Log("Can't equip weapon due to gladiator");
            return;
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
    }

    // --- Special Ability ---
    public void OnDealtDamage(float damageAmount)
    {
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
        if (isSpecialReady && !isDead) ActivateSpecialAbility();
    }

    private void ActivateSpecialAbility()
    {
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
        if (isDashing) return; // i-frames

        float finalAmount = dmg.amount * statusMgr.DamageTakenMultiplier;

        if (dmg.element != DamageElement.True)
        {
            StatusType effect = statusMgr.GetStatusFromElement(dmg.element);
            TryAddStatus(effect);
        }
        
        statusMgr.FlashSpriteRoutine(dmg.element);
        healthComp.ReceiveDamage(finalAmount, dmg.element);
        
        WaveManager waveMgr = FindFirstObjectByType<WaveManager>();
        if(waveMgr) waveMgr.TookDamageThisWave();
    }

    public void StatusDamage(DamageInfo dmg)
    {
        float finalAmount = dmg.amount * statusMgr.DamageTakenMultiplier;
        statusMgr.FlashSpriteRoutine(dmg.element);
        healthComp.ReceiveDamage(finalAmount, dmg.element);
    }

    // --- Buffs / Stats ---
    public void ApplyBuff(StatType type, float amount, float duration)
    {
        StartCoroutine(BuffRoutine(type, amount, duration));
    }

    public void ApplyPermanentBuff(StatType type, float amount)
    {
        switch (type)
        {
            case StatType.Defense: statusMgr.ChangeDamageMultiplier(-amount); break;
            case StatType.BaseDamage: if(currentWeapon) currentWeapon.damage += currentWeapon.damage * amount; break;
            case StatType.Speed: movementSpeed += baseMovementSpeed * amount; break;
            case StatType.AttackSpeed: playerAttackSpeedMultiplier += amount; break;
        }   
    }
    

    private IEnumerator BuffRoutine(StatType type, float amount, float duration)
    {
        switch (type)
        {
            case StatType.Defense: statusMgr.ChangeDamageMultiplier(-amount); break;
            case StatType.BaseDamage: if(currentWeapon) currentWeapon.damage += currentWeapon.damage * amount; break;
            case StatType.Speed: movementSpeed += baseMovementSpeed * amount; break;
            case StatType.AttackSpeed: playerAttackSpeedMultiplier += amount; break;
        }

        yield return new WaitForSeconds(duration);

        switch (type)
        {
            case StatType.Defense: statusMgr.ChangeDamageMultiplier(amount); break;
            case StatType.BaseDamage: if(currentWeapon) currentWeapon.damage -= currentWeapon.damage * amount; break;
            case StatType.Speed: movementSpeed -= baseMovementSpeed * amount; break;
            case StatType.AttackSpeed: playerAttackSpeedMultiplier -= amount; break;
        }
    }

    public void ModifySpeed(float percentageAmount) => movementSpeed += baseMovementSpeed * percentageAmount;

    public void ModifyPlayerStat(StatType statType, float value)
    {
        if (statType == StatType.MaxHealth) healthComp.SetMaxHealth(value);
        if (statType == StatType.AttackSpeed) playerAttackSpeedMultiplier += value;
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
            case StatType.BaseDamage: if (currentWeapon != null) currentWeapon.damage *= 1.1f; break;
            case StatType.MaxHealth: healthComp.ModifyMaxHealth(1); healthComp.Heal(1f); break; // +1 Heart
            case StatType.Speed: baseMovementSpeed *= 1.05f; movementSpeed = baseMovementSpeed; break;
            case StatType.AttackSpeed: playerAttackSpeedMultiplier += 0.05f; break;
        }
    }

    public void TryAddStatus(StatusType effect) => statusMgr.TryAddStatus(effect);
    public float GetMaxHealth() => healthComp.GetMaxHealth();
    public void Heal(float amount) => healthComp.Heal(amount);
    public void AddTemporaryHearts(float amount) => healthComp.AddTemporaryHearts(amount);
    
    public void ToggleLuck(bool state) { hasLuck = state; Debug.Log($"Luck: {state}"); }
    public void LockLuck() => isLuckLocked = true;

    public float GetBaseDamage()
    {
        if (currentWeapon != null) return currentWeapon.damage;
        Debug.Log("No weapon equipped, returning unarmed base damage.");
        return 1f; // Unarmed base damage
    }
    public void SetLuck(int value)
    {
        if (!isLuckLocked) { hasLuck = true; luck = value; }
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        switch (type) {
            case CurrencyType.Rupee: rupees += amount; break;
            case CurrencyType.Key: keys += amount; break;
        }
        onCurrencyUpdate?.Invoke();
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
    public void PlayerKilled() => OnPlayerDeath?.Invoke();
}