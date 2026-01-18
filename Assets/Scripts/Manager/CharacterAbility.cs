using UnityEngine;

public enum CharacterRarity
{
    Default,    // Balanced, easier
    Epic,       // Stronger, flashy
    Mythic      // Extreme stats, WEAPON LOCKED
}

public enum AbilityChargeType
{
    DamageDealt,    // Charge fills as you hit enemies
    DamageTaken,    // Charge fills as you get hurt (Tank/Berserker)
    HealingDone,    // Charge fills as you heal (Support)
    TimeBased,      // Charges automatically over time
    Kills           // Charges per kill
}

// The Logic for the Special Skill (Strategy Pattern)
public abstract class CharacterAbility : ScriptableObject
{
    public string abilityName;
    [TextArea] public string description;
    
    // The actual logic (e.g., Summon a meteor, Heal self, Rage mode)
    public abstract void Activate(PlayerController player);
}