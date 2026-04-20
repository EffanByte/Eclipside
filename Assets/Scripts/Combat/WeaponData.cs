using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class WeaponData : ItemData
{
    protected override string DefaultLocalizationTable => "Weapons";

    [Header("Core Info")]
    
    public AnimatorOverrideController animatorOverride;

    [Header("Visuals")]
    public GameObject weaponPrefab;

    [Header("Stats")]
    public float damage;
    [SerializeField] protected float cooldown;
    public float Cooldown => cooldown;
    [SerializeField] protected float knockbackForce;
    public float KnockbackForce => knockbackForce;
    [SerializeField] protected float hitDuration = 0.2f;
    public float HitDuration => hitDuration;
    [SerializeField] protected float criticalChance;
    public float CriticalChance => criticalChance;
    [SerializeField] protected float projectileSpeed;
    public float ProjectileSpeed => projectileSpeed;

    [Header("Damage Type")]
    public DamageElement element;
    public AttackStyle style;

    // --- EFFECTS LIST ---
    [Header("Special Effects")]
    public List<WeaponEffect> effects = new List<WeaponEffect>();

    // --- PIPELINE METHODS ---

    public void Initialize(PlayerController player)
    {
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.OnEquip(player);
            }
        }
    }

    public void Cleanup(PlayerController player)
    {
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.OnUnequip(player);
            }
        }
    }

    public void ConfigureCoreStats(
        float damageValue,
        float cooldownValue,
        float knockbackValue,
        float hitDurationValue,
        float criticalChanceValue,
        float projectileSpeedValue,
        DamageElement elementValue,
        AttackStyle styleValue)
    {
        damage = damageValue;
        cooldown = cooldownValue;
        knockbackForce = knockbackValue;
        hitDuration = hitDurationValue;
        criticalChance = criticalChanceValue;
        projectileSpeed = projectileSpeedValue;
        element = elementValue;
        style = styleValue;
    }

    public void ReplaceEffects(List<WeaponEffect> newEffects)
    {
        effects = newEffects ?? new List<WeaponEffect>();
    }

    // This calculates the FINAL damage packet by running it through all effects
    public DamageInfo GetDamageInfoOnHit(PlayerController player, EnemyBase target)
    {
        // 1. Start with Base Stats
        float finalDamage = damage;
        float finalKnockback = knockbackForce;
        if (player != null)
        {
            finalDamage *= player.GetDamageMultiplierForWeapon(this);
        }
        
        float critChanceToUse = player != null ? player.GetCriticalChanceForWeapon(this, criticalChance) : criticalChance;
        float critDamageMultiplier = player != null ? player.GetCriticalDamageMultiplier() : 1f;

        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.OnBeforeHit(player, target, ref finalDamage, ref critChanceToUse, ref critDamageMultiplier, ref finalKnockback);
            }
        }

        bool isCrit = critChanceToUse >= Random.Range(0f, 100f);
        if (isCrit)
        {
            finalDamage *= Mathf.Max(1f, critDamageMultiplier);
        }

        DamageInfo info = new DamageInfo(
            amount: finalDamage,
            element: element,
            style: style,
            sourcePosition: player != null ? (Vector2)player.transform.position : Vector2.zero,
            knockbackForce: finalKnockback,
            isCritical: isCrit
        );

        // 2. Run through Effects (They can modify 'info' or apply statuses to 'target')
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.OnHit(player, target, ref info);
            }
        }

        return info;
    }

    public void ConfigureProjectile(PlayerController player, MagicProjectile projectile)
    {
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.OnProjectileSpawned(player, projectile);
            }
        }
    }

    public abstract IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox);
}
