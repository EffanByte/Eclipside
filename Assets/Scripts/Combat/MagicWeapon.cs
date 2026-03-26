using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "New Magic Weapon ", menuName = "Eclipside/Weapons/2. Magic Weapon")]
public class MagicWeapon : WeaponData
{
    public override IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox)
    {
        if (player.anim != null) player.anim.SetTrigger("Attack");

        MagicProjectile.Spawn(player, this);

        yield return new WaitForSeconds(hitDuration);

        if (activeHitbox != null)
        {
            activeHitbox.DisableHitbox();
        }
    }
}
