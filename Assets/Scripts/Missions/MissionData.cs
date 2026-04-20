using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Mission Data")]
public class MissionData : ScriptableObject
{
    private const string DefaultLocalizationTable = "Missions";

    [Header("Identity")]
    public string id; // Unique ID (e.g., "DAILY_E_KILL_120")
    public string title;
    [TextArea] public string description;
    [SerializeField] private string localizationTable = DefaultLocalizationTable;
    [SerializeField] private string titleLocalizationKey;
    [SerializeField] private string descriptionLocalizationKey;
    
    [Header("Configuration")]
    public MissionDifficulty difficulty;
    public string statKey; // Must match StatisticsManager keys (e.g. "KILLS_REGULAR")
    public int targetValue; // e.g. 120

    [Header("Rewards")]
    public MissionRewardType rewardType;
    public int rewardAmount;

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

        return $"mission.{SanitizeForKey(id)}{suffix}";
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
