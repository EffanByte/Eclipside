using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Eclipside/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterID; // Must match ID in SaveFile (e.g. "char_knight")
    public string characterName;
    [TextArea] public string lore;
    public CharacterRarity rarity;
    
    [Header("Visuals")]
    public Sprite portrait; // For Character Select / UI
    public Sprite inGameSprite; // Or AnimatorOverrideController if you use animations
    public AnimatorOverrideController animatorOverride; // Essential for "Flashier animations" on Epics

    [Header("Base Stats")]
    public float maxHealth = 100f; // 10 Hearts
    public float moveSpeed = 5f;
    public float dashCooldown = 1f;
    [Tooltip("Base damage multiplier (1.0 = 100%)")]
    public float damageMultiplier = 1.0f;
    public float defense = 0f; // Damage reduction %

    [Header("Combat Loadout")]
    [Tooltip("The weapon they start with.")]
    public WeaponData startingWeapon;

    [Tooltip("If TRUE, this character CANNOT pick up new weapons (Mythic Rule).")]
    public bool IsWeaponLocked => rarity == CharacterRarity.Mythic;

    [Header("Special Ability")]
    public CharacterAbility specialAbility;
    public AbilityChargeType chargeType;
    public float chargeMax = 100f; // How much damage/healing needed to fill bar

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
}