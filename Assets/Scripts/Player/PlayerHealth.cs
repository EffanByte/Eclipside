using System; // Required for Actions
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;
    [Header("Health Stats")]
    // PDF Page 12: "1 Heart = 10 damage units". 
    // If you want 10 hearts, set this to 10.
    [SerializeField] private float maxHearts = 100f; 
    
    private float currentHealth;

    // Events: The UI will listen to these
    public event Action<int> OnMaxHealthChanged; // Sends max hearts count
    public event Action<float> OnHealthChanged;  // Sends current health value
    public event Action OnPlayerDeath;           // Notification for Game Over

    private void Start()
    {
        // Initialize HP
        currentHealth = maxHearts;
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Notify the UI immediately so it draws the hearts
        OnMaxHealthChanged?.Invoke((int)maxHearts);
        OnHealthChanged?.Invoke(currentHealth);
    }

    // ----------------------------------------------------
    // IDamageable Implementation
    // ----------------------------------------------------

    public void ReceiveDamage(float finalDamage, DamageElement element)
    {
        // 1. Apply Damage
        // Note: You can add Defense/Armor logic here later
        Debug.Log($"Player took {finalDamage} {element} damage!");
        currentHealth -= finalDamage;

        // 2. Clamp values (Cannot go below 0 or above Max)
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHearts);
        
        // 3. Notify UI
        
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log("Health Change Invoked");

        // 4. Check Death
        if (currentHealth <= 0)
            Die();
    }

    // Public method for Potions (Healing)
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHearts);
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        OnPlayerDeath?.Invoke();
        // Disable controls, show Game Over screen, etc.
        gameObject.SetActive(false); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}