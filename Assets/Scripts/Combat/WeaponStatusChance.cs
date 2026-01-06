using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Status Chance")]
public class WeaponStatusChance : WeaponEffect
{
    public StatusType statusToApply;
    public float chance; // 0.2 = 20%

    public override void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo)
    {
        if (Random.value <= chance)
        {
            target.TryAddStatus(statusToApply);
            Debug.Log($"Applied {statusToApply} via weapon effect!");
        }
    }
}