using UnityEngine;

// =========================================================
// 1. LIGHT MELEE (Rusty Dagger)
// High speed, Low damage, Mid knockback
// =========================================================
[CreateAssetMenu(fileName = "New Light Melee", menuName = "Eclipside/Weapons/1. Light Melee")]
public class LightMeleeWeapon : WeaponData
{
    // We don't need attackRange here anymore for damage, 
    // but it might be useful for AI or Gizmos later.
    public float attackRange = 1.2f; 

    public override void OnAttack(PlayerController player, Vector2 aimDirection)
    {
        // 1. Trigger the Animation
        // The Animation clip itself is responsible for Enabling/Disabling 
        // the "Sword Hitbox" GameObject at the right frames.
        if (player.anim != null) 
        {
            player.anim.SetTrigger("Attack");
        }

        // 2. DO NOT calculate damage here.
        // We rely entirely on the 'SwordHitbox.cs' script attached to the
        // sword sprite to detect collisions during the swing.
    }
}
// =========================================================
// 2. HEAVY MELEE (Iron Hammer)
// Slow speed, High damage, High knockback
// =========================================================
[CreateAssetMenu(fileName = "New Heavy Melee", menuName = "Eclipside/Weapons/2. Heavy Melee")]
public class HeavyMeleeWeapon : WeaponData
{
    public float attackRange = 2.0f; // Larger range
    public float screenShakeAmount = 0.2f; // Heavy weapons feel heavy!

    public override void OnAttack(PlayerController player, Vector2 aimDirection)
    {
        // Heavy weapons trigger "Attack" but the animation clip should be slower
        if (player.anim != null) player.anim.SetTrigger("Attack");

        // Logic: Larger area, stronger hit
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Apply Massive Damage
                Debug.Log($"[Heavy Melee] CRUSHED {hit.name} for {damage} dmg with {knockbackForce} knockback!");
            }
        }
    }
}

// =========================================================
// 3. MAGIC RANGED (Apprentice's Staff)
// Projectile-based, Mid damage, Slightly homing
// =========================================================
[CreateAssetMenu(fileName = "New Magic Ranged", menuName = "Eclipside/Weapons/3. Magic Ranged")]
public class MagicRangedWeapon : WeaponData
{
    public GameObject projectilePrefab; // The "Magic Orb" prefab
    public float projectileSpeed = 8f;

    public override void OnAttack(PlayerController player, Vector2 aimDirection)
    {
        if (player.anim != null) player.anim.SetTrigger("Shoot");

        // Calculate rotation based on aim direction
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Spawn Projectile
        GameObject proj = Instantiate(projectilePrefab, player.transform.position, rotation);
        
        // Setup Projectile Logic
        MagicProjectile script = proj.GetComponent<MagicProjectile>();
        if (script != null)
        {
            script.Setup(aimDirection, projectileSpeed, damage, knockbackForce);
        }
    }
}