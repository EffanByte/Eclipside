using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Conditional Damage")]
public class ConditionalDamageEffect : WeaponEffect
{
    public enum Condition
    {
        Below30PercentHP,
        TargetIsPoisoned,
        TargetIsFrozen,
        TargetIsConfused,
        TargetIsBurning
    }

    public Condition condition;
    public float damageMultiplier;
    public float critChanceBonus;
    public float speedMultiplier;
    public float knockbackMultiplier;
    public float speedBuffDuration = 0.9f;

    public override void OnBeforeHit(PlayerController player, EnemyBase target, ref float damage, ref float criticalChance, ref float criticalDamageMultiplier, ref float knockback)
    {
        if (!IsConditionMet(target))
        {
            return;
        }

        if (damageMultiplier > 0f)
        {
            damage *= 1f + damageMultiplier;
        }

        if (critChanceBonus > 0f)
        {
            criticalChance += critChanceBonus;
        }

        if (knockbackMultiplier > 0f)
        {
            knockback *= 1f + knockbackMultiplier;
        }
    }

    public override void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo)
    {
        if (player == null || speedMultiplier <= 0f || !IsConditionMet(target))
        {
            return;
        }

        player.ApplyBuff(GetSpeedBuffKey(), StatType.AttackSpeed, speedMultiplier, speedBuffDuration);
    }

    private bool IsConditionMet(EnemyBase target)
    {
        if (target == null)
        {
            return false;
        }

        switch (condition)
        {
            case Condition.Below30PercentHP:
                return target.GetMaxHealth() > 0f && (target.GetCurrentHealth() / target.GetMaxHealth()) < 0.3f;
            case Condition.TargetIsPoisoned:
                return target.HasStatus(StatusType.Poison);
            case Condition.TargetIsFrozen:
                return target.HasStatus(StatusType.Freeze);
            case Condition.TargetIsConfused:
                return target.HasStatus(StatusType.Confusion);
            case Condition.TargetIsBurning:
                return target.HasStatus(StatusType.Burn);
            default:
                return false;
        }
    }

    private string GetSpeedBuffKey()
    {
        return $"WeaponConditionalSpeed_{GetInstanceID()}";
    }
}
