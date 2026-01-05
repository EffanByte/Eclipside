using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsHealthUI : MonoBehaviour
{
    private const float HEALTH_PER_HEART = 10f;

    [Header("Dependencies")]
    private PlayerHealth playerHealth;

    [Header("Visual Setup")]
    [SerializeField] private Image heartPrefab;
    [SerializeField] private Transform heartsParent;

    private readonly List<Image> hearts = new();

    private void OnEnable()
    {
        playerHealth = GameObject.FindWithTag("Player").GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnMaxHealthChanged += InitHearts;
            playerHealth.OnHealthChanged += UpdateHearts;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnMaxHealthChanged -= InitHearts;
            playerHealth.OnHealthChanged -= UpdateHearts;
        }
    }

    private void InitHearts(int maxHealth)
    {
        foreach (Transform child in heartsParent)
            Destroy(child.gameObject);

        hearts.Clear();

        int heartCount = Mathf.CeilToInt(maxHealth / HEALTH_PER_HEART);

        for (int i = 0; i < heartCount; i++)
        {
            Image heart = Instantiate(heartPrefab, heartsParent);
            heart.type = Image.Type.Filled;
            heart.fillAmount = 1f;
            hearts.Add(heart);
        }
    }

    private void UpdateHearts(float currentHealth)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            float heartHealthStart = i * HEALTH_PER_HEART;
            float heartFill = (currentHealth - heartHealthStart) / HEALTH_PER_HEART;

            hearts[i].fillAmount = Mathf.Clamp01(heartFill);
        }
    }
}
