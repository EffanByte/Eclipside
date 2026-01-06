using UnityEngine;

public abstract class WeaponEffect : ScriptableObject
{
    public virtual void OnEquip(PlayerController player) { }

    public virtual void OnUnequip(PlayerController player) { }

    public virtual void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo) { }
}