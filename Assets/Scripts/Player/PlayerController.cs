using UnityEngine;
using System.Collections;

    public enum StatType 
    { 
        AttackSpeed, 
        BaseDamage, 
        MagicDamage, 
        ProjectileSpeed, 
        MaxHealth,
        CritChance,
        CritDamage,
        Luck
    }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InventoryManager))] 

public class PlayerController : MonoBehaviour
{
    [Header("--- Stats (Page 1) ---")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float maxHearts = 3f; 
    [SerializeField] private float dashForce = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float luck = 0;
    public float playerAttackSpeedMultiplier = 1f; 
    [Header("--- Progression ---")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 100f;
    public int rupees = 0;
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
    private float currentHealth; 
    private SpecialMeterFill specialMeterFill;
    private Rigidbody2D rb;
    private Vector2 rawInputMovement;
    private bool isDashing;
    private bool canAttack = true;
    private bool isDead = false;
    private bool isAttackPressed = false; 

    // --- Buff State ---
    private float baseMovementSpeed; 
    public bool hasLuck = false; 

    // --- Status Effects ---
    private bool isFrozen = false;
    private bool isConfused = false; 

    // --- References ---
    private PlayerControls controls; 
    private InventoryManager inventory; 

    public delegate void OnStatChange();
    public event OnStatChange onUIUpdate;

    private float lastAttackTime = -999f; 
    
    [HideInInspector] public Animator anim; 

    public static PlayerController instance {get; private set;}

    private void Awake()
    {
        if (instance == null)
            instance = this;
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
        statusMgr = GetComponent<StatusManager>();
        statusMgr.Initialize(rb, this, ReceiveDamage, GetComponent<SpriteRenderer>());
        baseMovementSpeed = movementSpeed;
        currentHealth = maxHearts * 10f;

        // Input Bindings
        controls.Player.Move.performed += ctx => rawInputMovement = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => rawInputMovement = Vector2.zero;
        controls.Player.Fire.started += ctx => isAttackPressed = true;
        controls.Player.Fire.canceled += ctx => isAttackPressed = false;
        controls.Player.Dash.performed += ctx => AttemptDash();
        controls.Player.Special.performed += ctx => AttemptSpecial();

        // Items (0, 1, 2 are the array indexes)
        controls.Player.Item1.performed += ctx => inventory.TriggerItemUse(0); 
        controls.Player.Item2.performed += ctx => inventory.TriggerItemUse(1);
        controls.Player.Item3.performed += ctx => inventory.TriggerItemUse(2);
    }

    // FIX: added Start to spawn the weapon visual
    private void Start()
    {
        specialMeterFill = FindObjectOfType<SpecialMeterFill>();
        EquipWeapon(currentWeapon);
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
            statusMgr.TryAddStatus(effect);
        }
        statusMgr.FlashSpriteRoutine(dmg.element);
        // 4. Pass the FINAL result to Health
        healthComp.ReceiveDamage(finalAmount, dmg.element);
    }


    #region 1. Movement & Controls
    
    private void Move()
    {
        if (isDashing) return;

        Vector2 finalInput = rawInputMovement;
        if (isConfused) finalInput *= -1; 
        float finalSpeed = movementSpeed;
        if (isFrozen) finalSpeed *= 0.6f;

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
        if (!isDashing && !isDead && !isFrozen)
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
            onUIUpdate?.Invoke();
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
        onUIUpdate?.Invoke();
    }
    #endregion

    #region 3. Health & Status Effects


    public void Heal(float hearts)
    {
        currentHealth += hearts * 10f;
        if (currentHealth > maxHearts * 10f) currentHealth = maxHearts * 10f;
        onUIUpdate?.Invoke();
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
            maxHearts += value;
        if (statType == StatType.AttackSpeed)
            playerAttackSpeedMultiplier += value;
    }
    public void ToggleLuck(bool state)
    {
        hasLuck = state;
        Debug.Log($"Luck set to: {state}");
    }

    public void UseItemFromUI(int slotNumber)
    {
        // FIX: Subtract 1 because Array is 0-2, but UI usually sends 1-3
        inventory.TriggerItemUse(slotNumber - 1);
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
        
        if (collision.CompareTag("Potion"))
        {
            Heal(0.5f); 
            Destroy(collision.gameObject);
        }

        if (collision.CompareTag("Rupee"))
        {
            rupees++;
            Destroy(collision.gameObject);
            onUIUpdate?.Invoke();
        }
    }

    public void AddExperience(float amount)
    {
        currentExp += amount;
        if (currentExp >= expToNextLevel)
        {
            currentLevel++;
            currentExp = 0;
            Debug.Log("Level Up!");
        }
    }

    
    #endregion
    
}