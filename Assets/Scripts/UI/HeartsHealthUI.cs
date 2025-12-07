using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsHealthUI : MonoBehaviour
{
    [Header("Dependencies")]
    private PlayerHealth playerHealth; // Drag the player here

    [Header("Visual Setup")]
    [SerializeField] private Image heartPrefab;      
    [SerializeField] private Transform heartsParent; 
    
    private List<Image> hearts = new List<Image>();

    private void OnEnable()
    {
        playerHealth = GameObject.FindWithTag("Player").GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Subscribe to events
            playerHealth.OnMaxHealthChanged += InitHearts;
            playerHealth.OnHealthChanged += UpdateHearts;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            // Unsubscribe to avoid errors if object is destroyed
            playerHealth.OnMaxHealthChanged -= InitHearts;
            playerHealth.OnHealthChanged -= UpdateHearts;
        }
    }

    // Called automatically when PlayerHealth starts
    private void InitHearts(int maxHearts)
    {
        // Clear existing children
        foreach (Transform child in heartsParent)
            Destroy(child.gameObject);

        hearts.Clear();

        for (int i = 0; i < maxHearts; i++)
        {
            Image heart = Instantiate(heartPrefab, heartsParent);
            heart.type = Image.Type.Filled; 
            heart.fillAmount = 1f;
            hearts.Add(heart);
        }
    }

    // Called automatically whenever PlayerHealth changes
    private void UpdateHearts(float currentHealth)
    {
        // Logic: 1.0f in health equals 1.0f fill in one heart
        // Example: Health 9.5
        // Heart 0-8: (9.5 - 0..8) > 1.0 -> Filled
        // Heart 9:   (9.5 - 9) = 0.5    -> Half Full
        // Heart 10:  (9.5 - 10) < 0     -> Empty

        for (int i = 0; i < hearts.Count; i++)
        {
            float heartFill = Mathf.Clamp01(currentHealth - i);
            hearts[i].fillAmount = heartFill;
        }
    }
}