using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Status Chance")]
public class WeaponStatusChance : WeaponEffect
{
    public StatusType statusToApply;
    public float chance; // 0.2 = 20%

    public override void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo)
    {
        if (target == null)
        {
            return;
        }

        float luckBonus = player != null ? player.GetStatusProcChanceBonus() : 0f;
        float finalChance = Mathf.Clamp01(chance + luckBonus);

        if (Random.value <= finalChance)
        {
            float baseDuration = player != null && player.GetStatusManager() != null
                ? player.GetStatusManager().GetBaseDuration(statusToApply)
                : 0f;
            float finalDuration = player != null ? player.GetOutgoingStatusDuration(statusToApply, baseDuration) : -1f;
            target.TryAddStatus(statusToApply, finalDuration);
            Debug.Log($"Applied {statusToApply} via weapon effect! Base: {chance:P0}, Luck Bonus: {luckBonus:P0}, Final: {finalChance:P0}");
        }
    }
}
