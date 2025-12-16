using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InventoryManager))] // Ensures the Inventory script is attached
public class PlayerController : MonoBehaviour
{
    [Header("--- Stats (Page 1) ---")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float maxHearts = 3f; // 1 Heart = 10 HP
    [SerializeField] private float attackSpeed = 1f; 
    [SerializeField] private float dashForce = 10f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("--- Progression ---")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 100f;
    public int rupees = 0;
    public int keys = 0;

    [Header("--- Special Ability (Page 1) ---")]
    public float specialChargeMax = 100f;
    private float currentSpecialCharge = 0f;
    private bool isSpecialReady = false;

    // --- Internal State ---
    private float currentHealth; 
    private Rigidbody2D rb;
    private Vector2 rawInputMovement;
    private bool isDashing;
    private bool canAttack = true;
    private bool isDead = false;
    private bool isAttackPressed = false; 

    // --- Buff State (New) ---
    private float baseMovementSpeed; // To remember speed before buffs
    public bool hasLuck = false; // Accessible by Loot system

    // --- Status Effects Flags (Page 6) ---
    private bool isFrozen = false;
    private bool isConfused = false; 

    // --- References ---
    private PlayerControls controls; 
    private InventoryManager inventory; // Reference to the new system

    public delegate void OnStatChange();
    public event OnStatChange onUIUpdate;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inventory = GetComponent<InventoryManager>(); // Get the manager
        controls = new PlayerControls();

        // Store original speed so we can revert buffs later
        baseMovementSpeed = movementSpeed;

        // --- EXISTING MOVEMENT/COMBAT ---
        controls.Player.Move.performed += ctx => rawInputMovement = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => rawInputMovement = Vector2.zero;
        controls.Player.Fire.started += ctx => isAttackPressed = true;
        controls.Player.Fire.canceled += ctx => isAttackPressed = false;
        controls.Player.Dash.performed += ctx => AttemptDash();
        controls.Player.Special.performed += ctx => AttemptSpecial();

        // --- UPDATED: ITEM INPUTS ---
        // Instead of handling logic here, we tell the InventoryManager to do it.
        // Array index 0 is Slot 1.
        controls.Player.Item1.performed += ctx => inventory.TriggerItemUse(0); 
        controls.Player.Item2.performed += ctx => inventory.TriggerItemUse(1);
        controls.Player.Item3.performed += ctx => inventory.TriggerItemUse(2);
        
        currentHealth = maxHearts * 10f;
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

    #region 1. Movement & Controls
    
    private void Move()
    {
        if (isDashing) return;

        Vector2 finalInput = rawInputMovement;

        if (isConfused) finalInput *= -1; 

        float finalSpeed = movementSpeed;

        if (isFrozen) finalSpeed *= 0.6f;

        // Note: Using linearVelocity (Unity 6+) based on your previous snippet. 
        // If on older Unity, change to 'velocity'
        rb.linearVelocity = finalInput * finalSpeed;
    }

    private void AttemptDash()
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
        Debug.Log("Basic Attack!");
        OnDealtDamage(10f); 
        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        float speedMultiplier = isFrozen ? 0.6f : 1.0f;
        float cooldown = 1f / (attackSpeed * speedMultiplier);
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }

    public void OnDealtDamage(float damageAmount)
    {
        if (!isSpecialReady)
        {
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
    
    public void TakeDamage(float damage)
    {
        if (isDead || isDashing) return; 

        currentHealth -= damage;
        Debug.Log($"Took {damage} damage. HP: {currentHealth}");

        if (currentHealth <= 0) Die();
        onUIUpdate?.Invoke();
    }

    public void Heal(float hearts)
    {
        currentHealth += hearts * 10f;
        if (currentHealth > maxHearts * 10f) currentHealth = maxHearts * 10f;
        onUIUpdate?.Invoke();
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        controls.Disable(); 
        Debug.Log("Player Died");
    }

    public void ApplyStatusEffect(string effectName)
    {
        StartCoroutine(HandleStatusEffect(effectName));
    }

    private IEnumerator HandleStatusEffect(string effectName)
    {
        switch (effectName)
        {
            case "Burn":
                for (int i = 0; i < 9; i++) { TakeDamage(2f); yield return new WaitForSeconds(0.33f); }
                break;
            case "Poison":
                for (int i = 0; i < 7; i++) { TakeDamage(1f); yield return new WaitForSeconds(1f); }
                break;
            case "Freeze":
                isFrozen = true;
                float storedSpeed = movementSpeed;
                movementSpeed = 0f; 
                yield return new WaitForSeconds(2.0f); 
                movementSpeed = storedSpeed; 
                yield return new WaitForSeconds(0.5f); 
                isFrozen = false;
                break;
            case "Confusion":
                isConfused = true;
                yield return new WaitForSeconds(4f);
                isConfused = false;
                break;
        }
    }
    #endregion

    #region 4. Interactions & NEW API
    
    // --- NEW METHODS FOR ITEMS TO CALL ---

    // Called by Quick Berry
    public void ModifySpeed(float percentageAmount)
    {
        // Add percentage of base speed to current speed
        movementSpeed += (baseMovementSpeed * percentageAmount);
        Debug.Log($"Speed modified. Current: {movementSpeed}");
    }

    // Called by Rabbit's Foot
    public void ToggleLuck(bool state)
    {
        hasLuck = state;
        Debug.Log($"Luck set to: {state}");
    }

    // Helper for Touch UI Buttons
    // Link your UI Button OnClick() to this function
    public void UseItemFromUI(int slotNumber)
    {
        // Convert 1-based UI slot to 0-based array index
        inventory.TriggerItemUse(slotNumber - 1);
    }

    // --- TRIGGERS ---

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Chest"))
        {
            if (keys > 0)
            {
                keys--;
                Debug.Log("Chest Opened.");
                Destroy(collision.gameObject);
            }
            else Debug.Log("Need keys!");
        }
        
        // Note: Potions are now picked up by Inventory logic or used instantly.
        // If you want instant use on pickup, keep this:
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