using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Heal Every Hits")]
public class WeaponHealEveryHitsEffect : WeaponEffect
{
    public int hitThreshold = 15;
    public float healAmount = 1f;

    private int currentHits;
    private PlayerController owner;

    public override void OnEquip(PlayerController player)
    {
        owner = player;
        currentHits = 0;
    }

    public override void OnUnequip(PlayerController player)
    {
        if (owner == player)
        {
            owner = null;
            currentHits = 0;
        }
    }

    public override void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo)
    {
        if (target == null || owner == null || hitThreshold <= 0)
        {
            return;
        }

        currentHits++;
        if (currentHits < hitThreshold)
        {
            return;
        }

        currentHits = 0;
        owner.Heal(healAmount);
        Debug.Log($"[Weapon] {name} healed {healAmount} after {hitThreshold} hits.");
    }
}
