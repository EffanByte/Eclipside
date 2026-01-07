using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsHealthUI : MonoBehaviour
{
    private const float HEALTH_PER_HEART = 10f;

    [Header("Dependencies")]
    private PlayerHealth playerHealth;

    [Header("Visual Setup")]
    [Tooltip("The Red Heart Image Prefab")]
    [SerializeField] private Image redHeartPrefab;
    
    [Tooltip("The Yellow/Blue Heart Image Prefab")]
    [SerializeField] private Image tempHeartPrefab;
    
    [SerializeField] private Transform heartsParent;

    // We keep two separate lists to manage them independently
    private readonly List<Image> redHearts = new();
    private readonly List<Image> tempHearts = new();

    private void OnEnable()
    {
        // Find Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            // Subscribe to events
            playerHealth.OnMaxHealthChanged += InitRedHearts;
            playerHealth.OnHealthChanged += UpdateRedHearts;
            
            // NEW: Subscribe to temp health updates
            playerHealth.OnTempHealthChanged += UpdateTempHearts;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnMaxHealthChanged -= InitRedHearts;
            playerHealth.OnHealthChanged -= UpdateRedHearts;
            playerHealth.OnTempHealthChanged -= UpdateTempHearts;
        }
    }

    // ---------------------------------------------------------
    // RED HEARTS (Permanent)
    // ---------------------------------------------------------

    private void InitRedHearts(int maxHealth)
    {
        // 1. Clear ONLY the red hearts from the UI
        foreach (Image heart in redHearts)
        {
            if(heart != null) Destroy(heart.gameObject);
        }
        redHearts.Clear();

        // 2. Clear Temp hearts too (to ensure order stays correct: Red then Gold)
        // We will rebuild temp hearts immediately after via the event or manual call
        foreach (Image heart in tempHearts)
        {
            if (heart != null) Destroy(heart.gameObject);
        }
        tempHearts.Clear();

        // 3. Create Red Hearts
        int heartCount = Mathf.CeilToInt(maxHealth / HEALTH_PER_HEART);

        for (int i = 0; i < heartCount; i++)
        {
            Image heart = Instantiate(redHeartPrefab, heartsParent);
            heart.type = Image.Type.Filled;
            heart.fillMethod = Image.FillMethod.Horizontal; // Ensure fill works left-to-right
            heart.fillAmount = 1f;
            redHearts.Add(heart);
        }
        
        // Force refresh temp hearts if we had any
        // (This assumes PlayerHealth stores the value and we can access it, 
        //  otherwise we wait for the next update event)
    }

    private void UpdateRedHearts(float currentHealth)
    {
        for (int i = 0; i < redHearts.Count; i++)
        {
            float heartHealthStart = i * HEALTH_PER_HEART;
            float heartFill = (currentHealth - heartHealthStart) / HEALTH_PER_HEART;

            redHearts[i].fillAmount = Mathf.Clamp01(heartFill);
        }
    }

    // ---------------------------------------------------------
    // GOLD HEARTS (Temporary)
    // ---------------------------------------------------------

    private void UpdateTempHearts(float currentTempHealth)
    {
        // 1. Calculate how many hearts we NEED
        int neededCount = Mathf.CeilToInt(currentTempHealth / HEALTH_PER_HEART);

        // 2. Adjust the list size to match needed count
        // If we have too few, add more
        while (tempHearts.Count < neededCount)
        {
            Image heart = Instantiate(tempHeartPrefab, heartsParent);
            heart.type = Image.Type.Filled;
            heart.fillMethod = Image.FillMethod.Horizontal;
            tempHearts.Add(heart);
        }

        // If we have too many, remove them
        while (tempHearts.Count > neededCount)
        {
            // Remove from the end
            Image heartToRemove = tempHearts[tempHearts.Count - 1];
            tempHearts.RemoveAt(tempHearts.Count - 1);
            Destroy(heartToRemove.gameObject);
        }

        // 3. Update Fill Amounts
        for (int i = 0; i < tempHearts.Count; i++)
        {
            float heartHealthStart = i * HEALTH_PER_HEART;
            float heartFill = (currentTempHealth - heartHealthStart) / HEALTH_PER_HEART;

            tempHearts[i].fillAmount = Mathf.Clamp01(heartFill);
        }
    }
}