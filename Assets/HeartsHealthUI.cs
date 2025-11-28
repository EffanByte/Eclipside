using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsHealthUI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Image heartPrefab;      // Filled Image heart
    [SerializeField] private Transform heartsParent; // HorizontalLayoutGroup parent
    [SerializeField] private int maxHearts = 10;     // Change this later to connect to health script

    private readonly List<Image> hearts = new();

    private float currentHealth;

    private void Awake()
    {
        InitHearts();
        SetHealth(maxHearts); // start full
    }

    private void Start()
    {
        ApplyDamage (5.0f);
    }
    private void InitHearts()
    {
        // Clear existing children (optional if you spawn only once)
        foreach (Transform child in heartsParent)
            Destroy(child.gameObject);

        hearts.Clear();

        for (int i = 0; i < maxHearts; i++)
        {
            Image heart = Instantiate(heartPrefab, heartsParent);
            heart.type = Image.Type.Filled; // just in case
            heart.fillAmount = 1f;
            hearts.Add(heart);
        }
    }

    /// <summary>
    /// Set current health in [0, maxHearts], fractional allowed.
    /// </summary>
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHearts);

        for (int i = 0; i < hearts.Count; i++)
        {
            // 1 HP per heart
            float heartFill = Mathf.Clamp01(currentHealth - i);
            hearts[i].fillAmount = heartFill;
        }
    }

    /// <summary>
    /// Apply damage (positive value).
    /// </summary>
    public void ApplyDamage(float damage)
    {
        SetHealth(currentHealth - damage);
    }

}
