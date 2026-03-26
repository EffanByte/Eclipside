using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Target Status Projectile Speed")]
public class WeaponTargetStatusProjectileSpeedEffect : WeaponEffect
{
    public StatusType requiredStatus = StatusType.Burn;
    public float speedMultiplier = 1.15f;

    public override void OnProjectileSpawned(PlayerController player, MagicProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        EnemyBase target = projectile.GetCurrentTargetEnemy();
        if (target != null && target.HasStatus(requiredStatus))
        {
            projectile.MultiplySpeed(speedMultiplier);
        }
    }
}
