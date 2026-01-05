using UnityEngine;

public abstract class WeaponEffect : ScriptableObject
{
    // Called when the player equips the weapon (Passive stats like Max HP)
    public virtual void OnEquip(PlayerController player) { }
    public virtual void OnUnequip(PlayerController player) { }

    // Called when the weapon hits an enemy (Burn, Extra Damage)
    // Returns the modified damage amount
    public virtual float OnHit(PlayerController player, EnemyBase enemy, float incomingDamage) 
    { 
        return incomingDamage; 
    }

    // Called when the weapon kills an enemy (Bloodforged Colossus, Chrono Daggers)
    public virtual void OnKill(PlayerController player, EnemyBase enemy) { }
}