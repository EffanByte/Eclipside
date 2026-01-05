    // using UnityEngine;
    // [CreateAssetMenu(menuName = "Eclipside/Effects/Stat Boost")]
    // public class StatBoost : WeaponEffect
    // {
    //     public enum StatType { AttackSpeed, MaxHealth, BaseDamage, MagicDamage, ProjectileSpeed }
    //     public StatType stat;
    //     public float amount; // 0.10 = 10%

    //     public override void OnEquip(PlayerController player)
    //     {
    //         // Add logic to PlayerController to handle these modifiers
    //         if (stat == StatType.AttackSpeed) player.attackSpeedMultiplier += amount;
    //         if (stat == StatType.MaxHealth) player.IncreaseMaxHealthPercent(amount);
    //         // etc...
    //     }

    //     public override void OnUnequip(PlayerController player)
    //     {
    //         if (stat == StatType.AttackSpeed) player.attackSpeedMultiplier -= amount;
    //         if (stat == StatType.MaxHealth) player.IncreaseMaxHealthPercent(-amount);
    //     }
    // }