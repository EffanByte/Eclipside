using UnityEngine;
using System.Collections;

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