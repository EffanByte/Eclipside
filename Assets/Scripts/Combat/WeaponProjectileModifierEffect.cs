using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Projectile Modifier")]
public class WeaponProjectileModifierEffect : WeaponEffect
{
    public float speedMultiplier = 1f;
    public int additionalPierces = 0;
    public float colliderRadiusMultiplier = 1f;
    public float lifetime = 3f;

    public override void OnProjectileSpawned(PlayerController player, MagicProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        if (!Mathf.Approximately(speedMultiplier, 1f))
        {
            projectile.MultiplySpeed(speedMultiplier);
        }

        if (additionalPierces > 0)
        {
            projectile.AddPierceCount(additionalPierces);
        }

        if (!Mathf.Approximately(colliderRadiusMultiplier, 1f))
        {
            projectile.MultiplyColliderRadius(colliderRadiusMultiplier);
        }

        projectile.SetLifetime(lifetime);
    }
}
