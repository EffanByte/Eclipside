using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Data;

public class LevelUpUI : MonoBehaviour
{
    [Header("UI References")]
    private GameObject panel;
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject upgradeButtonPrefab;

    // The list of possible upgrades (from GDD)
    private List<StatType> possibleUpgrades = new List<StatType>
    {
        StatType.BaseDamage,
        StatType.MagicDamage,
        StatType.HeavyDamage,
        StatType.MaxHealth,
        StatType.Speed,
        StatType.AttackSpeed,
    };

    private void Start()
    {
        panel = gameObject;
        panel.SetActive(false);
        PlayerController.Instance.OnLevelUp += ShowLevelUpOptions;
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnLevelUp -= ShowLevelUpOptions;
    }

    private void ShowLevelUpOptions()
    {
        // 1. Pause Game
        Time.timeScale = 0f;
        panel.SetActive(true);

        // 2. Clear old buttons
        foreach (Transform child in buttonsContainer) Destroy(child.gameObject);

        // 3. Pick 3 Random Options
        List<StatType> options = GetRandomOptions(3);

        // 4. Create Buttons
        foreach (StatType stat in options)
        {
            GameObject btnObj = Instantiate(upgradeButtonPrefab, buttonsContainer);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI text = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Set Text
            string desc = GetStatDescription(stat);
            text.text = desc;

            // Add Click Listener
            btn.onClick.AddListener(() => SelectUpgrade(stat));
        }
    }

    private void SelectUpgrade(StatType stat)
    {
        // Apply the upgrade
        PlayerController.Instance.ApplyPermanentUpgrade(stat);

        // Close UI and Resume Game
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private List<StatType> GetRandomOptions(int count)
    {
        List<StatType> pool = new List<StatType>(possibleUpgrades);
        List<StatType> selected = new List<StatType>();

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;
            int index = Random.Range(0, pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return selected;
    }

    private string GetStatDescription(StatType stat)
    {
        switch (stat)
        {
            case StatType.BaseDamage: return "Melee Damage";
            case StatType.MagicDamage: return "Magic Damage";
            case StatType.HeavyDamage: return "Heavy Damge";
            case StatType.MaxHealth: return "Health";
            case StatType.Speed: return "Move Speed";
            case StatType.AttackSpeed: return "Attack Speed";
            default: return stat.ToString();
        }
    }
}