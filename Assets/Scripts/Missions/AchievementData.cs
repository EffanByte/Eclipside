using UnityEngine;

public enum AchievementType
{
    Incremental,    // Count up to X (Kills, Gold Spent)
    State,          // Boolean check (Complete run < 1 heart)
    DateCheck       // Daily logins
}

[CreateAssetMenu(menuName = "Eclipside/Achievement")]
public class AchievementData : ScriptableObject
{
    private const string DefaultLocalizationTable = "Achievements";

    [Header("Identity")]
    public string id; // Unique Key e.g., "ACH_KILL_100"
    public string title;
    [TextArea] public string description;
    public Sprite icon;
    [SerializeField] private string localizationTable = DefaultLocalizationTable;
    [SerializeField] private string titleLocalizationKey;
    [SerializeField] private string descriptionLocalizationKey;

    [Header("Logic")]
    public AchievementType type;
    public string statKey; // The variable we are watching (e.g., "TOTAL_KILLS")
    public int targetValue; // 100, 1000, 5000

    [Header("Reward")]
    public RewardType rewardType;
    public int rewardAmount; // For Gold/Orbs
    public ItemData rewardItem; // For Consumables

    public string GetTitle()
    {
        return ResolveLocalizedValue(titleLocalizationKey, title, ".title");
    }

    public string GetDescription()
    {
        return ResolveLocalizedValue(descriptionLocalizationKey, description, ".description");
    }

    public string GetTitleLocalizationKey()
    {
        return !string.IsNullOrWhiteSpace(titleLocalizationKey) ? titleLocalizationKey.Trim() : BuildDefaultKey(".title");
    }

    public string GetDescriptionLocalizationKey()
    {
        return !string.IsNullOrWhiteSpace(descriptionLocalizationKey) ? descriptionLocalizationKey.Trim() : BuildDefaultKey(".description");
    }

    public string GetLocalizationTable()
    {
        return string.IsNullOrWhiteSpace(localizationTable) ? DefaultLocalizationTable : localizationTable;
    }

    private string ResolveLocalizedValue(string explicitKey, string fallback, string suffix)
    {
        string key = !string.IsNullOrWhiteSpace(explicitKey) ? explicitKey.Trim() : BuildDefaultKey(suffix);
        if (string.IsNullOrWhiteSpace(key))
        {
            return fallback;
        }

        string table = string.IsNullOrWhiteSpace(localizationTable) ? DefaultLocalizationTable : localizationTable;
        return LocalizationManager.GetString(table, key, fallback);
    }

    private string BuildDefaultKey(string suffix)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return string.Empty;
        }

        return $"achievement.{SanitizeForKey(id)}{suffix}";
    }

    private static string SanitizeForKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char character = char.ToLowerInvariant(value[i]);
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character == ' ' || character == '-' || character == '_')
            {
                builder.Append('_');
            }
        }

        return builder.ToString().Trim('_');
    }
}
