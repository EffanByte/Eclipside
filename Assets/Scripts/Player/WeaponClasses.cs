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
            5f,
            DamageElement.Physical,
            AttackStyle.MeleeLight,
            player.transform.position,
            knockbackForce: 1f,
            isCritical: false
        );

        // 2. Play Animation
        if (player.anim != null) player.anim.SetTrigger("Attack");


        // 4. Wait for the swing duration (defined in WeaponData)
        yield return new WaitForSeconds(hitDuration);

        // 5. DISABLE COLLIDER
        if (activeHitbox != null)
        {
            activeHitbox.DisableHitbox();
        }
    }
}
