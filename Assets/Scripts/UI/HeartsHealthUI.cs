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

    private readonly List<Image> redHearts = new();
    private readonly List<Image> tempHearts = new();

    // 1. Initialization moved to Start to wait for PlayerController Awake
    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            playerHealth = PlayerController.Instance.GetComponent<PlayerHealth>();
            SubscribeEvents();
            
            // Force Initial Draw
            // We assume GetMaxHealth() exists, or we wait for the first event
            if (playerHealth != null)
            {
                // InitRedHearts requires float now based on your code
                InitRedHearts(playerHealth.GetMaxHealth()); 
                UpdateRedHearts(playerHealth.GetCurrentHealth());
            }
        }
    }

    private void OnEnable()
    {
        // Only re-subscribe if we already initialized (e.g., UI was hidden then shown)
        if (playerHealth != null)
        {
            SubscribeEvents();
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            UnsubscribeEvents();
        }
    }

    private void SubscribeEvents()
    {
        // Safety check to avoid double subscription
        UnsubscribeEvents();

        playerHealth.OnMaxHealthChanged += InitRedHearts;
        playerHealth.OnHealthChanged += UpdateRedHearts;
        playerHealth.OnTempHealthChanged += UpdateTempHearts;
    }

    private void UnsubscribeEvents()
    {
        playerHealth.OnMaxHealthChanged -= InitRedHearts;
        playerHealth.OnHealthChanged -= UpdateRedHearts;
        playerHealth.OnTempHealthChanged -= UpdateTempHearts;
    }

    // ---------------------------------------------------------
    // RED HEARTS (Permanent)
    // ---------------------------------------------------------

    private void InitRedHearts(float maxHealth)
    {
        foreach (Image heart in redHearts) if(heart != null) Destroy(heart.gameObject);
        redHearts.Clear();

        foreach (Image heart in tempHearts) if (heart != null) Destroy(heart.gameObject);
        tempHearts.Clear();

        int heartCount = Mathf.CeilToInt(maxHealth / HEALTH_PER_HEART);

        for (int i = 0; i < heartCount; i++)
        {
            Image heart = Instantiate(redHeartPrefab, heartsParent);
            heart.type = Image.Type.Filled;
            heart.fillMethod = Image.FillMethod.Horizontal; 
            heart.fillAmount = 1f;
            redHearts.Add(heart);
        }
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
        int neededCount = Mathf.CeilToInt(currentTempHealth / HEALTH_PER_HEART);

        while (tempHearts.Count < neededCount)
        {
            Image heart = Instantiate(tempHeartPrefab, heartsParent);
            heart.type = Image.Type.Filled;
            heart.fillMethod = Image.FillMethod.Horizontal;
            tempHearts.Add(heart);
        }

        while (tempHearts.Count > neededCount)
        {
            Image heartToRemove = tempHearts[tempHearts.Count - 1];
            tempHearts.RemoveAt(tempHearts.Count - 1);
            Destroy(heartToRemove.gameObject);
        }

        for (int i = 0; i < tempHearts.Count; i++)
        {
            float heartHealthStart = i * HEALTH_PER_HEART;
            float heartFill = (currentTempHealth - heartHealthStart) / HEALTH_PER_HEART;
            tempHearts[i].fillAmount = Mathf.Clamp01(heartFill);
        }
    }
}