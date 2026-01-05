using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class WeaponData : ScriptableObject
{
    [Header("Core Info")]
    public string weaponName;
    public Sprite icon;
    public AnimatorOverrideController animatorOverride;

    [Header("Visuals")]
    public GameObject weaponPrefab;

    [Header("Stats")]
    public float damage;
    public float cooldown;
    public float knockbackForce;
    public float hitDuration = 0.2f;

    [Header("Damage Type")]
    public DamageElement element;
    public AttackStyle style;

    // --- NEW: THE EFFECTS LIST ---
    [Header("Special Effects")]
    public List<WeaponEffect> effects = new List<WeaponEffect>();

    public abstract IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox);

    // Helper to process hits
    public float ApplyEffectsOnHit(PlayerController player, EnemyBase target, float baseDamage)
    {
        float finalDamage = baseDamage;
        foreach (var effect in effects)
        {
            finalDamage = effect.OnHit(player, target, finalDamage);
        }
        return finalDamage;
    }
    
    // Helper for kills
    public void NotifyKill(PlayerController player, EnemyBase target)
    {
         foreach (var effect in effects) effect.OnKill(player, target);
    }
}   