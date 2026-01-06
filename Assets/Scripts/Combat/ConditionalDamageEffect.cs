using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Conditional Damage")]
public class ConditionalDamageEffect : WeaponEffect
{
    public enum Condition { Below30PercentHP, TargetIsPoisoned, TargetIsFrozen, TargetIsConfused }
    public Condition condition;
    public float damageMultiplier; // 0.08 for +8%
    public float critChanceBonus;  // 0.15 for +15% crit
    public float speedMultiplier;  // 0.05 for +5% attack speed

    public override void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo)
    {
        bool conditionMet = false;

        switch (condition)
        {
            case Condition.Below30PercentHP:
                if (target.GetCurrentHealth() / target.GetMaxHealth() < 0.3f) conditionMet = true;
                break;
            case Condition.TargetIsPoisoned:
                if (target.HasStatus(StatusType.Poison)) conditionMet = true;
                break;
            case Condition.TargetIsFrozen:
                if (target.HasStatus(StatusType.Freeze)) conditionMet = true;
                break;
            case Condition.TargetIsConfused:
                if (target.HasStatus(StatusType.Confusion)) conditionMet = true;
                break;
        }

        if (conditionMet)
        {
            // Apply Damage Bonus
            if (damageMultiplier > 0)
                dmgInfo.amount *= 1f + damageMultiplier;
            // Apply Crit Bonus (We might need to retroactively force crit logic here or in player)
            if (critChanceBonus > 0 && Random.value <= critChanceBonus)
                dmgInfo.isCritical = true;
            // Apply Attack Speed Bonus
            if (speedMultiplier > 0)
                player.playerAttackSpeedMultiplier *= 1f + speedMultiplier;
        }
    }
}