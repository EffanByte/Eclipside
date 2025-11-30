using UnityEngine;
using UnityEngine.InputSystem; // Required namespace
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("--- Stats (Page 1) ---")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float maxHearts = 3f; // 1 Heart = 10 HP
    [SerializeField] private float attackSpeed = 1f; // Attacks per second
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
    private float currentHealth; // Calculated as Hearts * 10
    private Rigidbody2D rb;
    private Vector2 rawInputMovement;
    private bool isDashing;
    private bool canAttack = true;
    private bool isDead = false;
    private bool isAttackPressed = false; // For holding down attack button

    // --- Status Effects Flags (Page 6) ---
    private bool isFrozen = false;
    private bool isConfused = false; // Inverts controls

    // --- Input System Reference ---
    private PlayerControls controls; 

    public delegate void OnStatChange();
    public event OnStatChange onUIUpdate;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Initialize Input System
        controls = new PlayerControls();

        // Bind Actions
        controls.Player.Move.performed += ctx => rawInputMovement = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => rawInputMovement = Vector2.zero;

        controls.Player.Fire.started += ctx => isAttackPressed = true;
        controls.Player.Fire.canceled += ctx => isAttackPressed = false;

        controls.Player.Dash.performed += ctx => AttemptDash();
        controls.Player.Special.performed += ctx => AttemptSpecial();

        // Initialize Health (Page 5: 1 Heart = 10 damage units)
        currentHealth = maxHearts * 10f;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        
        if (isDead) return;

        // Handle Auto-Attack while holding button
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

    #region 1. Movement & Controls (Page 1 & 10)
    
    private void Move()
    {
        if (isDashing) return;

        Vector2 finalInput = rawInputMovement;

        // Page 6: Confusion Status - "Effect: Player controls are inverted or randomized"
        if (isConfused)
        {
            finalInput *= -1; // Invert controls
        }

        float finalSpeed = movementSpeed;

        // Page 6: Freeze Status - "Effect on Player: Movement speed reduced by 40%"
        if (isFrozen)
        {
            finalSpeed *= 0.6f;
        }

        rb.linearVelocity = finalInput * finalSpeed;

        // Page 1: "Event: Player is moving" - Trigger Walk Animation here
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
        // Page 12: SFX "short wind burst" / Page 13: "fwoosh"
        isDashing = true;
        
        // Dash direction: Use input if moving, otherwise face right (default)
        Vector2 dashDir = rawInputMovement == Vector2.zero ? Vector2.right : rawInputMovement.normalized;
        
        rb.linearVelocity = dashDir * dashForce;
        
        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
        rb.linearVelocity = Vector2.zero; // Optional: stop momentum after dash
    }
    #endregion

    #region 2. Combat & Abilities (Page 1 & 7)

    private void PerformBasicAttack()
    {
        // Page 12: SFX "melee hits", "sharp tink"
        Debug.Log("Basic Attack!");
        
        // Simulating a hit for logic demonstration
        // In real implementation, this would be a collision event
        float simulatedDamageDealt = 10f; 
        OnDealtDamage(simulatedDamageDealt);

        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        
        // Page 6: Freeze reduces attack speed by 40%
        float speedMultiplier = isFrozen ? 0.6f : 1.0f;
        float cooldown = 1f / (attackSpeed * speedMultiplier);

        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }

    public void OnDealtDamage(float damageAmount)
    {
        // Page 1: "Player deals damage -> ability bar fills"
        if (!isSpecialReady)
        {
            currentSpecialCharge += damageAmount;
            
            // Check threshold
            if (currentSpecialCharge >= specialChargeMax)
            {
                currentSpecialCharge = specialChargeMax;
                isSpecialReady = true;
                
                // Page 13: SFX "bip low -> bip high"
                Debug.Log("Special Ability Ready!"); 
            }
            onUIUpdate?.Invoke();
        }
    }

    private void AttemptSpecial()
    {
        // Page 1: "Condition: Ability bar fills completely"
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
        // Page 1: "Output: Ability activates against enemies"
        // Page 13: SFX "whoosh explosion + short echo"
        Debug.Log("SPECIAL ABILITY UNLEASHED!");

        // Reset
        currentSpecialCharge = 0;
        isSpecialReady = false;
        onUIUpdate?.Invoke();
    }
    #endregion

    #region 3. Health & Status Effects (Page 5 & 6)
    
    public void TakeDamage(float damage)
    {
        if (isDead || isDashing) return; // Dash i-frames

        currentHealth -= damage;
        
        // Page 10: "Brief vibration and red screen flash"
        // Page 13: SFX "dry thud + short grunt"
        Debug.Log($"Took {damage} damage. HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        onUIUpdate?.Invoke();
    }

    public void Heal(float hearts)
    {
        // Page 5: 1 Heart = 10 damage units
        currentHealth += hearts * 10f;
        if (currentHealth > maxHearts * 10f) currentHealth = maxHearts * 10f;
        
        // Page 13: SFX "gentle chime + energy rising"
        onUIUpdate?.Invoke();
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        controls.Disable(); // Disable input on death
        
        // Page 8: Death animation
        // Page 13: SFX "deep boom + low bell echo"
        Debug.Log("Player Died");
    }

    // --- Status Effect System (Page 6) ---
    public void ApplyStatusEffect(string effectName)
    {
        StartCoroutine(HandleStatusEffect(effectName));
    }

    private IEnumerator HandleStatusEffect(string effectName)
    {
        switch (effectName)
        {
            case "Burn":
                // Page 6: 0.2 hearts (2 units), 3 ticks/sec, 3 sec duration
                // Visual: Flame overlay + sprite flickers red
                // Page 13: SFX "fire crackling"
                for (int i = 0; i < 9; i++) 
                {
                    TakeDamage(2f);
                    yield return new WaitForSeconds(0.33f);
                }
                break;

            case "Poison":
                // Page 6: 0.1 hearts (1 unit), 1 tick/sec, 6-8 sec duration
                // Visual: Green/purple mist
                // Page 14: SFX "bubbling / glup glup"
                for (int i = 0; i < 7; i++) 
                {
                    TakeDamage(1f);
                    yield return new WaitForSeconds(1f);
                }
                break;

            case "Freeze":
                // Page 6: "Paralyzes for 2 seconds" then slows
                // Visual: Blue tint + ice crystals
                // Page 14: SFX "ice forming / glass sound"
                isFrozen = true;
                
                // Full Paralyze (Stop movement)
                float storedSpeed = movementSpeed;
                movementSpeed = 0f; 
                
                yield return new WaitForSeconds(2.0f); 
                
                // Return to base speed (but isFrozen flag keeps it at 60% in Move/Attack)
                movementSpeed = storedSpeed; 
                yield return new WaitForSeconds(0.5f); // Remaining duration
                
                isFrozen = false;
                break;

            case "Confusion":
                // Page 6: "Controls are inverted"
                // Visual: Purple haze + dizzy stars
                // Page 14: SFX "distorted wobble / woOoo-woOoo"
                isConfused = true;
                yield return new WaitForSeconds(4f);
                isConfused = false;
                break;
        }
    }
    #endregion

    #region 4. Interactions (Page 2 & 5)
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Page 2: "Chest loot/keys"
        if (collision.CompareTag("Chest"))
        {
            if (keys > 0)
            {
                keys--;
                // Page 13: SFX Chest Open (common/epic/mythic variants)
                Debug.Log("Chest Opened.");
                Destroy(collision.gameObject);
                // Grant Loot Logic Here
            }
            else
            {
                // Page 2: UI Message
                Debug.Log("UI: 'Chests sometimes appear during waves. You need keys to open them.'");
            }
        }
        
        // Page 5: Healing Items
        if (collision.CompareTag("Potion"))
        {
            Heal(0.5f); // Small Potion = 0.5 hearts
            Destroy(collision.gameObject);
        }

        // Page 5: Rupee Collection
        if (collision.CompareTag("Rupee"))
        {
            rupees++;
            // Page 13: SFX "cling cling cling"
            Destroy(collision.gameObject);
            onUIUpdate?.Invoke();
        }
    }

    public void AddExperience(float amount)
    {
        currentExp += amount;
        if (currentExp >= expToNextLevel)
        {
            // Page 1: "Level-up, the player can select one of the following stat upgrades"
            // Page 13: SFX "soft whoosh + bright bell tone"
            currentLevel++;
            currentExp = 0;
            Debug.Log("Level Up! Display Upgrade UI.");
        }
    }
    #endregion
}