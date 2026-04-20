using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Eclipside/Character Data")]
public class CharacterData : ScriptableObject
{
    private const string DefaultLocalizationTable = "Characters";

    [Header("Identity")]
    public string characterID; // Must match ID in SaveFile (e.g. "char_knight")
    public string characterName;
    [TextArea] public string lore;
    public CharacterRarity rarity;
    [SerializeField] private string localizationTable = DefaultLocalizationTable;
    [SerializeField] private string nameLocalizationKey;
    [SerializeField] private string loreLocalizationKey;
    
    [Header("Visuals")]
    public Sprite portrait; // For Character Select / UI
    public Sprite inGameSprite; // Or AnimatorOverrideController if you use animations
    public AnimatorOverrideController animatorOverride; // Essential for "Flashier animations" on Epics

    [Header("Base Stats")]
    public float maxHealth = 50f; // 5 Hearts
    public float moveSpeed = 1f;
    public float dashCooldown = 1f;
    [Tooltip("Base damage multiplier (1.0 = 100%)")]
    public float damageMultiplier = 1.0f;
    public float defense = 0f; // Damage reduction %
    public float attackSpeedMultiplier = 1f;
    public float lightDamageMultiplier = 1f;
    public float magicDamageMultiplier = 1f;
    public float heavyDamageMultiplier = 1f;
    public float lightAttackSpeedMultiplier = 1f;
    public float magicAttackSpeedMultiplier = 1f;
    public float heavyAttackSpeedMultiplier = 1f;
    public float projectileSpeedMultiplier = 1f;
    public float critChanceMultiplier = 1f;
    public float critChanceFlatBonus = 0f;
    public float dashDistanceMultiplier = 1f;
    public float outgoingStatusDurationBonusSeconds = 0f;
    public float outgoingStatusDurationMultiplier = 1f;

    [Header("Damage Taken Multipliers")]
    public float contactDamageTakenMultiplier = 1f;
    public float projectileDamageTakenMultiplier = 1f;
    public float statusDamageTakenMultiplier = 1f;
    public float fireDamageTakenMultiplier = 1f;
    public float poisonDamageTakenMultiplier = 1f;
    public float iceDamageTakenMultiplier = 1f;
    public float magicDamageTakenMultiplier = 1f;
    public float physicalDamageTakenMultiplier = 1f;
    public float heavyDamageTakenMultiplier = 1f;

    [Header("Unique Rules")]
    public float permanentHealthCap = 0f;
    public bool overflowHealingCreatesTemporaryHealth = false;
    public bool maxHealthIncreasesBecomeTemporaryHealth = false;
    public float temporaryHealthCap = 0f;
    public float temporaryHealthDecayInterval = 0f;

    [Header("Combat Loadout")]
    [Tooltip("The weapon they start with.")]
    public WeaponData startingWeapon;

    [Tooltip("If TRUE, this character CANNOT pick up new weapons (Mythic Rule).")]
    public bool IsWeaponLocked => rarity == CharacterRarity.Mythic;

    [Header("Special Ability")]
    public CharacterAbility specialAbility;
    public AbilityChargeType chargeType;
    public float chargeMax = 100f; // How much damage/healing needed to fill bar
    public float specialCooldownSeconds = 30f;

    // ---------------------------------------------------------
    // PROGRESSION LOOKUP (Badges)
    // ---------------------------------------------------------
    
    // Checks the Save File to see if this char has beaten specific modes
    public bool HasCompletedRun(string difficultyMode) // "Easy", "Normal", "Hard"
    {
        var profile = SaveManager.Load<SaveFile_Profile>("Save_Profile");
        
        // Iterate through progress list to find this character
        foreach (var entry in profile.characters.character_progress)
        {
            if (entry.character_id == this.characterID)
            {
                if (difficultyMode == "Easy") return entry.completed_easy;
                if (difficultyMode == "Normal") return entry.completed_normal;
                if (difficultyMode == "Hard") return entry.completed_hard;
            }
        }
        return false;
    }

    // Helper for UI to get the badge color
    public Color GetBadgeColor(string difficultyMode)
    {
        if (!HasCompletedRun(difficultyMode)) return Color.clear; // No badge

        switch (difficultyMode)
        {
            case "Easy": return Color.green;   // Bronze/Green
            case "Normal": return Color.white; // Silver/White
            case "Hard": return Color.red;     // Gold/Red
            default: return Color.clear;
        }
    }

    public string GetDisplayName()
    {
        return ResolveLocalizedValue(nameLocalizationKey, characterName, ".name");
    }

    public string GetLocalizedLore()
    {
        return ResolveLocalizedValue(loreLocalizationKey, lore, ".description");
    }

    public string GetNameLocalizationKey()
    {
        return !string.IsNullOrWhiteSpace(nameLocalizationKey) ? nameLocalizationKey.Trim() : BuildDefaultKey(".name");
    }

    public string GetLoreLocalizationKey()
    {
        return !string.IsNullOrWhiteSpace(loreLocalizationKey) ? loreLocalizationKey.Trim() : BuildDefaultKey(".description");
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
        string baseValue = !string.IsNullOrWhiteSpace(characterID) ? characterID : characterName;
        if (string.IsNullOrWhiteSpace(baseValue))
        {
            return string.Empty;
        }

        return $"character.{SanitizeForKey(baseValue)}{suffix}";
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
