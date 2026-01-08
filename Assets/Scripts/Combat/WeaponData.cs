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
    [SerializeField] protected float cooldown;
    public float Cooldown => cooldown;
    [SerializeField] protected float knockbackForce;
    [SerializeField] protected float hitDuration = 0.2f;
    [SerializeField] protected float criticalChance;

    [Header("Damage Type")]
    public DamageElement element;
    public AttackStyle style;

    // --- EFFECTS LIST ---
    [Header("Special Effects")]
    public List<WeaponEffect> effects = new List<WeaponEffect>();

    // --- PIPELINE METHODS ---

    public void Initialize(PlayerController player)
    {
        foreach (var effect in effects) effect.OnEquip(player);
    }

    public void Cleanup(PlayerController player)
    {
        foreach (var effect in effects) effect.OnUnequip(player);
    }

    // This calculates the FINAL damage packet by running it through all effects
    public DamageInfo GetDamageInfoOnHit(PlayerController player, EnemyBase target)
    {
        // 1. Start with Base Stats
        float finalDamage = damage;
        // Apply Player stat multipliers here if needed (e.g. finalDamage *= player.damageMultiplier)
        
        bool isCrit = criticalChance >= Random.Range(0f, 100f);

        DamageInfo info = new DamageInfo(
            amount: finalDamage,
            element: element,
            style: style,
            sourcePosition: player.transform.position,
            knockbackForce: knockbackForce,
            isCritical: isCrit
        );

        // 2. Run through Effects (They can modify 'info' or apply statuses to 'target')
        foreach (var effect in effects)
        {
            effect.OnHit(player, target, ref info);
        }

        return info;
    }

    public abstract IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox);
}