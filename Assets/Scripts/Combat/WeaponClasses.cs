using UnityEngine;
using System.Collections;


[CreateAssetMenu(fileName = "New Light Melee", menuName = "Eclipside/Weapons/1. Light Melee")]
public class LightMeleeWeapon : WeaponData
{
    public override IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox)
    {
        if (activeHitbox != null)
        {
            activeHitbox.EnableHitbox();
        }

        yield return new WaitForSeconds(hitDuration);

        if (activeHitbox != null)
        {
            activeHitbox.DisableHitbox();
        }
    }
}
