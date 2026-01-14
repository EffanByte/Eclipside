using UnityEngine;
using System.Collections;


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

[CreateAssetMenu(fileName = "New Magic Weapon ", menuName = "Eclipside/Weapons/2. Magic Weapon")]
public class MagicWeapon : WeaponData
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

[CreateAssetMenu(fileName = "New Heavy Weapon", menuName = "Eclipside/Weapons/3. Heavy Melee")]
public class HeavyMelee : WeaponData
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