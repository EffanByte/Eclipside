using UnityEngine;
using System.Collections;
using System.Buffers.Text; // Required for IEnumerator

// =========================================================
// 1. LIGHT MELEE (Rusty Dagger)
// Uses the Hitbox logic: Turn On -> Wait -> Turn Off
// =========================================================
[CreateAssetMenu(fileName = "New Light Melee", menuName = "Eclipside/Weapons/1. Light Melee")]
public class LightMeleeWeapon : WeaponData
{
    public override IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox)
    {
        if (player.anim != null) player.anim.SetTrigger("Attack");

        yield return new WaitForSeconds(hitDuration);

        if (activeHitbox != null)
        {
            activeHitbox.DisableHitbox();
        }
    }
}