using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Conditional Damage")]
public class ConditionalDamage : WeaponEffect
{
    public enum Condition { Below30HP, IsPoisoned, IsFrozen, IsConfused }
    public Condition condition;
    public float damageMultiplier = 1.0f; // +8% = 0.08

    public override float OnHit(PlayerController player, EnemyBase enemy, float incomingDamage)
    {
        bool conditionMet = false;

        switch (condition)
        {
            case Condition.Below30HP:
                if (enemy.GetCurrentHealth() / enemy.GetMaxHealth() < 0.3f) conditionMet = true;
                break;
            case Condition.IsPoisoned:
                if (enemy.HasStatus(StatusType.Poison)) conditionMet = true; // You need flags in EnemyBase
                break;
            case Condition.IsFrozen:
                if (enemy.HasStatus(StatusType.Freeze)) conditionMet = true;
                break;
             case Condition.IsConfused:
                if (enemy.HasStatus(StatusType.Confusion)) conditionMet = true;
                break;
        }

        if (conditionMet)
        {
            return incomingDamage * (1 + damageMultiplier);
        }
        return incomingDamage;
    }
}
