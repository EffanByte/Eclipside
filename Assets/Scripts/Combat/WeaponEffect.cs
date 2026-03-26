using UnityEngine;

public abstract class WeaponEffect : ScriptableObject
{
    public virtual void OnEquip(PlayerController player) { }

    public virtual void OnUnequip(PlayerController player) { }

    public virtual void OnBeforeHit(PlayerController player, EnemyBase target, ref float damage, ref float criticalChance, ref float criticalDamageMultiplier, ref float knockback) { }

    public virtual void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo) { }

    public virtual void OnProjectileSpawned(PlayerController player, MagicProjectile projectile) { }
}
