using UnityEngine;

// =========================================================
// 1. LIGHT MELEE (Rusty Dagger)
// High speed, Low damage, Mid knockback
// =========================================================
[CreateAssetMenu(fileName = "New Light Melee", menuName = "Eclipside/Weapons/1. Light Melee")]
public class LightMeleeWeapon : WeaponData
{
    public float attackRange = 1.2f; // Short range

    public override void OnAttack(PlayerController player, Vector2 aimDirection)
    {
        // Light weapons trigger the "Attack" animation quickly
        if (player.anim != null) player.anim.SetTrigger("Attack");

        // Logic: Fast, simple hitbox check
        // (In the future, this can be replaced by Animation Events for precise timing)
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Apply Damage & Knockback
                // hit.GetComponent<EnemyHealth>()?.TakeDamage(damage, knockbackForce);
                Debug.Log($"[Light Melee] Hit {hit.name} for {damage} dmg");
            }
        }
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