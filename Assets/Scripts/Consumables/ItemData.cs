using UnityEngine;

public enum ItemRarity { Common, Rare, Epic, Mythical, Key }
public enum CurrencyType { Rupee, Key, XP, Gold, Orb }
public abstract class ItemData : ScriptableObject
{
    protected virtual string DefaultLocalizationTable => "Items";

    [Header("Core Info")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemRarity rarity;
    [SerializeField] private string localizationTable;
    [SerializeField] private string nameLocalizationKey;
    [SerializeField] private string descriptionLocalizationKey;

    public string GetDisplayName()
    {
        return ResolveLocalizedValue(nameLocalizationKey, itemName, ".name");
    }

    public string GetLocalizedDescription()
    {
        return ResolveLocalizedValue(descriptionLocalizationKey, description, ".description");
    }

    public string GetNameLocalizationKey()
    {
        return !string.IsNullOrWhiteSpace(nameLocalizationKey) ? nameLocalizationKey.Trim() : BuildDefaultKey(".name");
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

        return LocalizationManager.GetString(GetLocalizationTable(), key, fallback);
    }

    private string BuildDefaultKey(string suffix)
    {
        string baseValue = !string.IsNullOrWhiteSpace(name) ? name : itemName;
        if (string.IsNullOrWhiteSpace(baseValue))
        {
            return string.Empty;
        }

        return $"{GetLocalizationTable().ToLowerInvariant().Trim()}.{SanitizeForKey(baseValue)}{suffix}";
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

// Example of the wrapper class
