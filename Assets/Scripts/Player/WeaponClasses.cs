using UnityEngine;
using System.Collections; // Required for IEnumerator

// =========================================================
// 1. LIGHT MELEE (Rusty Dagger)
// Uses the Hitbox logic: Turn On -> Wait -> Turn Off
// =========================================================
[CreateAssetMenu(fileName = "New Light Melee", menuName = "Eclipside/Weapons/1. Light Melee")]
public class LightMeleeWeapon : WeaponData
{
    public override IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox)
    {
        // 1. Create the Damage Packet
        DamageInfo info = new DamageInfo(
            damage,
            element,
            style,
            player.transform.position,
            knockbackForce
        );

        // 2. Play Animation
        if (player.anim != null) player.anim.SetTrigger("Attack");

        // 3. ENABLE COLLIDER
        if (activeHitbox != null)
        {
            activeHitbox.Initialize(info);
        }

        // 4. Wait for the swing duration (defined in WeaponData)
        yield return new WaitForSeconds(hitDuration);

        // 5. DISABLE COLLIDER
        if (activeHitbox != null)
        {
            activeHitbox.DisableHitbox();
        }
    }
}

// =========================================================
// 2. HEAVY MELEE (Iron Hammer)
// Identical logic to Light Melee, but relying on the Inspector 
// stats (higher damage, longer hitDuration) to feel "Heavy".
// =========================================================
[CreateAssetMenu(fileName = "New Heavy Melee", menuName = "Eclipside/Weapons/2. Heavy Melee")]
public class HeavyMeleeWeapon : WeaponData
{
    public float screenShakeAmount = 0.2f; // Optional extra for heavy hits

    public override IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox)
    {
        // 1. Create Damage Packet
        DamageInfo info = new DamageInfo(
            damage,
            element,
            style,
            player.transform.position,
            knockbackForce
        );

        // 2. Play Animation
        if (player.anim != null) player.anim.SetTrigger("Attack");
        
        // (Optional: Trigger Screen Shake here if you have a Camera script)
        // CameraShake.Instance.Shake(screenShakeAmount);

        // 3. ENABLE COLLIDER
        if (activeHitbox != null)
        {
            activeHitbox.Initialize(info);
        }

        // 4. Wait (This will likely be a longer duration than Light Melee)
        yield return new WaitForSeconds(hitDuration);

        // 5. DISABLE COLLIDER
        if (activeHitbox != null)
        {
            activeHitbox.DisableHitbox();
        }
    }
}

// =========================================================
// 3. MAGIC RANGED (Apprentice's Staff)
// Spawns a projectile. Ignores the 'activeHitbox' on the player.
// =========================================================
[CreateAssetMenu(fileName = "New Magic Ranged", menuName = "Eclipside/Weapons/3. Magic Ranged")]
public class MagicRangedWeapon : WeaponData
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab; // The "Magic Orb" prefab
    public float projectileSpeed = 8f;

    public override IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox)
    {
        // 1. Play Animation
        if (player.anim != null) player.anim.SetTrigger("Shoot");

        // 2. Determine Direction
        // (If player is standing still, default to Right, otherwise use input)
        Vector2 aimDirection = player.GetLastMovementDirection(); 
        if (aimDirection == Vector2.zero) aimDirection = Vector2.right;

        // 3. Calculate Rotation
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 4. Spawn Projectile
        if (projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab, player.transform.position, rotation);
            
            // 5. Setup Projectile Logic
            MagicProjectile script = proj.GetComponent<MagicProjectile>();
            if (script != null)
            {
                script.Setup(aimDirection, projectileSpeed, damage, knockbackForce);
            }
        }

        // Coroutines must yield something. 
        // We yield 'break' to finish immediately as projectiles don't need to wait.
        yield break; 
    }
}