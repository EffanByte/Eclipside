using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Status Effect")]
public class StatusOnHit : WeaponEffect
{
    public StatusType statusType; // "Burn", "Poison", "Freeze", "Confusion"
    public float chance = 0.2f; // 20%

    public override float OnHit(PlayerController player, EnemyBase enemy, float incomingDamage)
    {
        if (Random.value <= chance)
        {
            // Call the status system on the enemy
            // Assuming EnemyBase has: ApplyStatus(string name)
            // If not, you can access the specific component
            enemy.TryAddStatus(statusType);
        }
        return incomingDamage;
    }
}