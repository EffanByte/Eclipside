using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Eclipside/Environment/Biome Data")]
public class BiomeData : ScriptableObject
{
    private const string DefaultLocalizationTable = "Biomes";

    [Header("Identity")]
    public string biomeName;
    [SerializeField] private string localizationTable = DefaultLocalizationTable;
    [SerializeField] private string localizationKey;
    [Tooltip("Tint applied to all tilemaps when this biome becomes active.")]
    public Color tileTint = Color.white;
    [Tooltip("Scene to load when this biome becomes active. Leave blank to stay in the current scene.")]
    public string sceneName;
    [Tooltip("How many waves before moving to the next biome")]
    public int wavesToClear = 10; 

    [Header("Spawning Pools")]
    [Tooltip("Normal enemies for this biome")]
    public List<GameObject> commonEnemies;

    [Header("Boss Settings")]
    public List<GameObject> bossPool;    

    public string GetDisplayName()
    {
        string resolvedKey = GetResolvedLocalizationKey();
        if (string.IsNullOrWhiteSpace(resolvedKey))
        {
            return biomeName;
        }

        string table = string.IsNullOrWhiteSpace(localizationTable) ? DefaultLocalizationTable : localizationTable;
        return LocalizationManager.GetString(table, resolvedKey, biomeName);
    }

    public string GetLocalizationTable()
    {
        return string.IsNullOrWhiteSpace(localizationTable) ? DefaultLocalizationTable : localizationTable;
    }

    public string GetResolvedLocalizationKey()
    {
        if (!string.IsNullOrWhiteSpace(localizationKey))
        {
            return localizationKey.Trim();
        }

        if (string.IsNullOrWhiteSpace(biomeName))
        {
            return string.Empty;
        }

        return $"biome.{SanitizeForKey(biomeName)}.title";
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
