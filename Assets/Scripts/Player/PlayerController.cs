using UnityEngine;
using System;
using System.Collections;
public enum StatType 
    { 
        AttackSpeed, 
        BaseDamage, 
        MagicDamage,
        HeavyDamage,
        MaxHealth,
        CritChance,
        CritDamage,
        Luck,
        Defense,
        Speed,
        projectileSpeed
    }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InventoryManager))] 

public class PlayerController : MonoBehaviour
{
    [Header("--- Stats (Page 1) ---")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float dashForce = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float luck = 0;
    public float playerAttackSpeedMultiplier = 1f; 

    
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactionLayer; // Set this to "Interactable" layer
    
    [Header("--- Progression ---")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 10f;
    public int rupees = 100;
    public int keys = 0;

    [Header("--- Combat Setup ---")]
    public WeaponData currentWeapon; // The data file (Stats)
    public Transform weaponHolder;   // The empty child object where sword spawns
    private WeaponHitbox currentWeaponHitBox; // The hitbox script on the spawned weapon
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
    private bool isDashing;
    private bool canAttack = true;
    private bool isDead = false;
    private bool isAttackPressed = false; 

    // --- Buff State ---
    private float baseMovementSpeed; 
    private bool hasLuck = false; 
    private bool isLuckLocked = false;

    // --- References ---
    private PlayerControls controls; 
    private InventoryManager inventory; 


    public event Action onCurrencyUpdate;
    public event Action OnLevelUp; // Trigger UI
    public event Action<float, float> OnExpChanged; // Update XP Bar UI (Current, Max)
    public event Action OnPlayerDeath;

    private float lastAttackTime = -999f; 
    
    [HideInInspector] public Animator anim; 

    public static PlayerController Instance {get; private set;}

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        rb = GetComponent<Rigidbody2D>();
        inventory = GetComponent<InventoryManager>();
        controls = new PlayerControls();
        anim = GetComponent<Animator>();
        healthComp = GetComponent<PlayerHealth>();
        healthComp.OnPlayerDeath += PlayerKilled;
        statusMgr = GetComponent<StatusManager>();
        statusMgr.Initialize(rb, this, StatusDamage, GetComponent<SpriteRenderer>());
        baseMovementSpeed = movementSpeed;

        // Input Bindings
        controls.Player.Move.performed += ctx => rawInputMovement = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => rawInputMovement = Vector2.zero;
        controls.Player.Fire.started += ctx => isAttackPressed = true;
        controls.Player.Fire.canceled += ctx => isAttackPressed = false;
        controls.Player.Dash.performed += ctx => AttemptDash();
        controls.Player.Special.performed += ctx => AttemptSpecial();
        controls.Player.Interact.performed += ctx => AttemptInteract(); 

        // Items (0, 1, 2 are the array indexes)
        controls.Player.Item1.performed += ctx => inventory.TriggerItemUse(0); 
        controls.Player.Item2.performed += ctx => inventory.TriggerItemUse(1); 
        controls.Player.Item3.performed += ctx => inventory.TriggerItemUse(2); 

    }

    // FIX: added Start to spawn the weapon visual
    private void Start()
    {
        specialMeterFill = FindFirstObjectByType<SpecialMeterFill>();
        EquipWeapon(currentWeapon);
        onCurrencyUpdate.Invoke();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        if (isDead) return;

        if (isAttackPressed && canAttack)
        {
            PerformBasicAttack();
        }
    }

    
    private void FixedUpdate()
    {
        if (isDead) return;
        Move();
    }

    // FIX: New Method to handle spawning the weapon prefab
    public void EquipWeapon(WeaponData newWeapon)
    {
        if (Challenges.theGladiator && newWeapon is MagicWeapon)
        {
            Debug.Log("Can't equip weapon due to gladiator");
        }
        currentWeapon = newWeapon;

        // 1. Clear old weapon
        if (weaponHolder.childCount > 0)
        {
            foreach (Transform child in weaponHolder) Destroy(child.gameObject);
        }

        // 2. Spawn new weapon
        if (currentWeapon != null && currentWeapon.weaponPrefab != null)
        {
            GameObject spawnedWeapon = Instantiate(currentWeapon.weaponPrefab, weaponHolder);
            
            // 3. Get the hitbox script so we can turn it on/off later
            currentWeaponHitBox = spawnedWeapon.GetComponent<WeaponHitbox>();
            
            // 4. Update Animator if needed
            if (currentWeapon.animatorOverride != null)
            {
                anim.runtimeAnimatorController = currentWeapon.animatorOverride;
            }
        }
    }

    public void ReceiveDamage(DamageInfo dmg)
    {
        // 1. Controller Logic: Check I-Frames / Dodging
        if (isDashing) return;

        // 2. Status Logic: Check Fragile / Protection Buffs
        // The Controller asks the StatusManager for the multiplier
        float finalAmount = dmg.amount * statusMgr.DamageTakenMultiplier;

        // 3. Status Application: Check for new effects (Burn/Poison)
        if (dmg.element != DamageElement.True)
        {
            StatusType effect = statusMgr.GetStatusFromElement(dmg.element);
            TryAddStatus(effect);
        }
        statusMgr.FlashSpriteRoutine(dmg.element);
        // 4. Pass the FINAL result to Health
        healthComp.ReceiveDamage(finalAmount, dmg.element);
        FindFirstObjectByType<WaveManager>().TookDamageThisWave();
    }

    public void StatusDamage(DamageInfo dmg)
    {
        float finalAmount = dmg.amount * statusMgr.DamageTakenMultiplier;
    
        statusMgr.FlashSpriteRoutine(dmg.element);
        // 4. Pass the FINAL result to Health
        healthComp.ReceiveDamage(finalAmount, dmg.element);
    }

    public void ApplyBuff(StatType type, float amount, float duration)
    {
        StartCoroutine(BuffRoutine(type, amount, duration));
    }

    private IEnumerator BuffRoutine(StatType type, float amount, float duration)
    {
        // Apply Buff
        switch (type)
        {
            case StatType.Defense:
                statusMgr.ChangeDamageMultiplier(-amount);
                break;
            case StatType.BaseDamage:
                currentWeapon.damage += currentWeapon.damage * amount; // its in %age
                break;
            case StatType.Speed:
                movementSpeed += baseMovementSpeed * amount;
                break;
            case StatType.AttackSpeed:
                playerAttackSpeedMultiplier += amount;
                break;
        }

        // Wait for duration
        yield return new WaitForSeconds(duration);

        // Revert Buff
        switch (type)
        {
            case StatType.Defense:
                statusMgr.ChangeDamageMultiplier(amount);
                break;
            case StatType.BaseDamage:
                currentWeapon.damage -= currentWeapon.damage * amount; // its in %age 
                // fix this later cos rn it reduces from buffed value
                break;
            case StatType.Speed:
                movementSpeed -= baseMovementSpeed * amount;
                break;
            case StatType.AttackSpeed:
                playerAttackSpeedMultiplier -= amount;
                break;
        }
    }
    #region 1. Movement & Controls
    
    private void Move()
    {
        if (isDashing) return;

        Vector2 finalInput = rawInputMovement;
        if (statusMgr.IsConfused) finalInput *= -1; 
        float finalSpeed = movementSpeed;
        if (statusMgr.IsFrozen) finalSpeed *= 0.6f;

        // FLIP LOGIC (Using your specific scale values)
        if (rawInputMovement.x > 0)
        {
            // Face Right
            transform.localScale = new Vector3(-0.42f, 0.42f, 1);
        }
        else if (rawInputMovement.x < 0)
        {
            // Face Left
            transform.localScale = new Vector3(0.42f, 0.42f, 1);
        }
    
        rb.linearVelocity = finalInput * finalSpeed;
    }

    // FIX: Added helper for Magic Weapons to know where to aim
    public Vector2 GetLastMovementDirection()
    {
        if (rawInputMovement != Vector2.zero) return rawInputMovement;
        // Check localScale to see if we are facing left or right
        return transform.localScale.x < 0 ? Vector2.right : Vector2.left; 
        // Note: Based on your Flip Logic above: -0.42 is Right, +0.42 is Left.
        // Adjusted return values to match your specific Flip Logic.
    }

    public void AttemptDash()
    {
        if (!isDashing && !isDead && !statusMgr.IsFrozen)
        {
            StartCoroutine(DashRoutine());
        }
    }
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        Vector2 dashDir = rawInputMovement == Vector2.zero ? Vector2.right : rawInputMovement.normalized;
        rb.linearVelocity = dashDir * dashForce;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        rb.linearVelocity = Vector2.zero; 
    }
    #endregion

    #region 2. Combat & Abilities

    private void PerformBasicAttack()
    {
        // Calculate Cooldown
        float actualCooldown = currentWeapon.Cooldown / playerAttackSpeedMultiplier;

        if (Time.time >= lastAttackTime + actualCooldown)
        {
            if (currentWeapon == null)
            {
                Debug.Log("No Weapon Equipped!");
                return;
            }
            lastAttackTime = Time.time;

            // FIX: Use StartCoroutine because OnAttack is now an IEnumerator (Timeline)
            // We pass the hitbox instance we grabbed in EquipWeapon
            StartCoroutine(currentWeapon.OnAttack(this, currentWeaponHitBox));
        
            // Special Meter Fill 
            OnDealtDamage(5f); // Hard-coded fix later 
        }
    }

    // Need to change this later to not depend directly on damage amount

    public void OnDealtDamage(float damageAmount)
    {
        if (!isSpecialReady)
        {
            specialMeterFill.SetValue(currentSpecialCharge);
            currentSpecialCharge += damageAmount;
            if (currentSpecialCharge >= specialChargeMax)
            {
                currentSpecialCharge = specialChargeMax;
                isSpecialReady = true;
                Debug.Log("Special Ability Ready!"); 
            }
        }
    }

    private void AttemptInteract()
    {
        // 1. Find all interactables nearby
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactionLayer);
        
        IInteractable closestInteractable = null;
        float closestDist = Mathf.Infinity;

        // 2. Find the closest one
        foreach (var hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestInteractable = interactable;
                }
            }
        }

        // 3. Perform Interaction
        if (closestInteractable != null)
        {
            closestInteractable.Interact(this);
        }
    }
    private void AttemptSpecial()
    {
        if (isSpecialReady && !isDead)
        {
            ActivateSpecialAbility();
        }
        else
        {
            Debug.Log("Special not ready yet.");
        }
    }

    private void ActivateSpecialAbility()
    {
        Debug.Log("SPECIAL ABILITY UNLEASHED!");
        currentSpecialCharge = 0;
        isSpecialReady = false;
    }
    #endregion

    #region 3. Health & Status Effects


    public void Heal(float amount)
    {
        healthComp.Heal(amount);
    }
   
    public void AddTemporaryHearts(float amount)
    {
        healthComp.AddTemporaryHearts(amount);
    }
    #endregion

    #region 4. Interactions & NEW API

    public void ModifySpeed(float percentageAmount)
    {
        movementSpeed += baseMovementSpeed * percentageAmount;
    }

    public void ModifyPlayerStat(StatType statType, float value)
    {
        if (statType == StatType.MaxHealth)
           healthComp.SetMaxHealth(value);
        if (statType == StatType.AttackSpeed)
            playerAttackSpeedMultiplier += value;
    }
    public void ToggleLuck(bool state)
    {
        hasLuck = state;
        Debug.Log($"Luck set to: {state}");
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Rupee:
                rupees += amount;
                break;
            case CurrencyType.Key:
                keys += amount;
                break;
        }
        onCurrencyUpdate?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Chest"))
        {
            if (keys > 0)
            {
                keys--;
                Debug.Log("Chest Opened.");
                Destroy(collision.gameObject);
                // Trigger your Loot Drop logic here later
            }
            else Debug.Log("Need keys!");
        }
    }

     public void AddExperience(float amount)
    {
        currentExp += amount;
        
        // Notify UI (XP Bar)
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        if (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
    }

     private void LevelUp()
    {
        currentLevel++;
        currentExp -= expToNextLevel;
        
        // Increase requirement (e.g., +20% per level)
        expToNextLevel *= 1.2f;

        // Trigger Event (Pauses game via UI)
        Debug.Log($"Leveled Up to {currentLevel}!");
        OnLevelUp?.Invoke();
        
        // If we had enough XP for multiple levels, handling carry-over:
        // Update UI again just in case
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
    }

    public void ApplyPermanentUpgrade(StatType stat)
    {
        switch (stat)
        {
            case StatType.BaseDamage:
                // Increase base modifier by 10%
                // You might need a separate 'permanentDamageMultiplier' variable 
                // so it stacks with item buffs cleanly
                if (currentWeapon != null) currentWeapon.damage *= 1.1f; 
                break;

            case StatType.MagicDamage:
                // magicDamageMultiplier += 0.1f;
                break;

            case StatType.MaxHealth:
                // +1 Heart (10 units)
                healthComp.ModifyMaxHealth(10); // Method you added previously
                healthComp.Heal(1f); // level up heals the new heart
                break;

            case StatType.Speed:
                baseMovementSpeed *= 1.05f;
                movementSpeed = baseMovementSpeed; // Refresh current
                break;

            case StatType.AttackSpeed:
                playerAttackSpeedMultiplier += 0.05f;
                break;
        }
        
        Debug.Log($"Applied Upgrade: {stat}");
    }

    public float GetMaxHealth()
    {
        return healthComp.GetMaxHealth();
    }

    public void TryAddStatus(StatusType effect)
    {
        statusMgr.TryAddStatus(effect);
    }
    
    public void LockLuck()
    {
        isLuckLocked = true;
    }
    public void SetLuck(int value)
    {
        if (isLuckLocked)
        {
            Debug.Log("Luck is Locked");
        }
        else
        {
            hasLuck = true;
            luck = value;
        }
    }

    public void PlayerKilled()
    {
        OnPlayerDeath.Invoke();
    }
    #endregion
    
}