using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class ChallengeToggle : MonoBehaviour
{
    [Header("Settings")]
    public ChallengeType challengeType;
    public TextMeshProUGUI label; // Optional: Drag the label here to auto-name it

    private Toggle toggle;

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshLabel;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLabel;
    }

    private void Start()
    {
        toggle = GetComponent<Toggle>();
        label = label ?? GetComponentInChildren<TextMeshProUGUI>();
        RefreshLabel();

        if (ChallengeManager.Instance != null)
        {
            toggle.isOn = ChallengeManager.Instance.activeChallenges.Contains(challengeType);
        }

        toggle.onValueChanged.AddListener((isOn) => 
        {
            if (ChallengeManager.Instance != null)
            {
                ChallengeManager.Instance.ToggleChallenge(challengeType, isOn);
            }
        });
    }

    private void RefreshLabel()
    {
        if (label == null)
        {
            return;
        }

        label.text = LocalizationManager.GetString(
            LocalizationManager.DefaultTable,
            GetChallengeKey(),
            GetFallbackLabel());
        LocalizedFontResolver.ApplyTo(label);
    }

    private string GetChallengeKey()
    {
        switch (challengeType)
        {
            case ChallengeType.FragileCrystal:
                return "menu.challenges.type.fragile_crystal";
            case ChallengeType.ThePurge:
                return "menu.challenges.type.the_purge";
            case ChallengeType.EndlessGreed:
                return "menu.challenges.type.endless_greed";
            case ChallengeType.TheGladiator:
                return "menu.challenges.type.the_gladiator";
            case ChallengeType.BloodForPower:
                return "menu.challenges.type.blood_for_power";
            case ChallengeType.Crossfire:
                return "menu.challenges.type.crossfire";
            case ChallengeType.TotalConfusion:
                return "menu.challenges.type.total_confusion";
            case ChallengeType.RainOfFire:
                return "menu.challenges.type.rain_of_fire";
            case ChallengeType.TheUnlucky:
                return "menu.challenges.type.the_unlucky";
            case ChallengeType.LastBreath:
                return "menu.challenges.type.last_breath";
            default:
                return string.Empty;
        }
    }

    private string GetFallbackLabel()
    {
        switch (challengeType)
        {
            case ChallengeType.FragileCrystal:
                return "Fragile Crystal";
            case ChallengeType.ThePurge:
                return "The Purge";
            case ChallengeType.EndlessGreed:
                return "Endless Greed";
            case ChallengeType.TheGladiator:
                return "The Gladiator";
            case ChallengeType.BloodForPower:
                return "Blood for Power";
            case ChallengeType.Crossfire:
                return "Crossfire";
            case ChallengeType.TotalConfusion:
                return "Total Confusion";
            case ChallengeType.RainOfFire:
                return "Rain of Fire";
            case ChallengeType.TheUnlucky:
                return "The Unlucky";
            case ChallengeType.LastBreath:
                return "Last Breath";
            default:
                return challengeType.ToString();
        }
    }
}
