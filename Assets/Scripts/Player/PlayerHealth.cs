using System; 
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;
    
    [Header("Health Stats")]
    [SerializeField] private float maxHealth = 40f; // 10 Hearts (10 units each)
    [SerializeField] public DeathMenu deathMenu;    
    public float currentHealth;
    private float temporaryHealth = 0f; // NEW: Golden/Blue Hearts

    // Events
    public event Action<float> OnMaxHealthChanged; 
    public event Action<float> OnHealthChanged;  
    public event Action<float> OnTempHealthChanged; // NEW: UI needs to know about temp hearts
    public static event Action OnPlayerDeath;           

    private void Start()
    {
        deathMenu = FindFirstObjectByType<DeathMenu>(FindObjectsInactive.Include);
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        OnMaxHealthChanged?.Invoke(maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
        OnTempHealthChanged?.Invoke(temporaryHealth);
    }

    // ----------------------------------------------------
    // DAMAGE LOGIC
    // ----------------------------------------------------

    public void ReceiveDamage(float finalDamage, DamageElement element)
    {
        Debug.Log($"Player hit for {finalDamage} ({element})");

        // 1. Absorb Damage with Temporary Health First
        if (temporaryHealth > 0)
        {
            // Determine how much temp health can absorb
            float damageToTemp = Mathf.Min(finalDamage, temporaryHealth);
            
            temporaryHealth -= damageToTemp;
            finalDamage -= damageToTemp; // Reduce incoming damage by amount absorbed

            // Update Temp Health UI
            OnTempHealthChanged?.Invoke(temporaryHealth);
        }

        // 2. Apply remaining damage to Real Health
        if (finalDamage > 0)
        {
            currentHealth -= finalDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            
            OnHealthChanged?.Invoke(currentHealth);
        }

        if (currentHealth <= 0)
            Die();
    }

    // ----------------------------------------------------
    // HEALING & BUFFS
    // ----------------------------------------------------

    // NEW: Adds health that sits on top of max health but doesn't heal back
    public void AddTemporaryHearts(float amount)
    {
        temporaryHealth += amount;
        Debug.Log($"Added {amount} Temp HP. Total: {temporaryHealth}");
        OnTempHealthChanged?.Invoke(temporaryHealth);
    }

    public void RemoveTemporaryHearts(float amount)
    {
        temporaryHealth = Mathf.Max(0f, temporaryHealth - amount);
        OnTempHealthChanged?.Invoke(temporaryHealth);
    }

    // Standard healing (Only affects Red Hearts)
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player != null && player.TryTriggerRevive())
        {
            return;
        }

        Debug.Log("Player Died!");
        deathMenu = FindFirstObjectByType<DeathMenu>(FindObjectsInactive.Include);
        deathMenu.OnPauseButtonPressed();
        OnPlayerDeath?.Invoke();
    }

    // ----------------------------------------------------
    // HELPERS
    // ----------------------------------------------------

    public float GetMaxHealth()
    {
        return maxHealth;
    }
    public float GetCurrentHealth() => currentHealth;
    
    public void SetMaxHealth(float max)
    {
        maxHealth = max;
        OnMaxHealthChanged?.Invoke(maxHealth);
    }

    public void ReviveToHealth(float amount)
    {
        temporaryHealth = 0f;
        currentHealth = Mathf.Clamp(amount, 1f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
        OnTempHealthChanged?.Invoke(temporaryHealth);
    }

    public void ModifyMaxHealth(int value)
    {
        SetMaxHealth(maxHealth + value);
    }
}
