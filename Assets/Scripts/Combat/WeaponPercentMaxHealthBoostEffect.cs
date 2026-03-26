using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Percent Max Health Boost")]
public class WeaponPercentMaxHealthBoostEffect : WeaponEffect
{
    public float percent = 0.12f;

    private PlayerController owner;
    private string buffKey;

    public override void OnEquip(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        if (owner != null)
        {
            OnUnequip(owner);
        }

        owner = player;
        buffKey = $"WeaponMaxHealth_{GetInstanceID()}";
        float amount = player.GetMaxHealth() * percent;
        player.ApplyPermanentBuff(buffKey, StatType.MaxHealth, amount);
    }

    public override void OnUnequip(PlayerController player)
    {
        if (owner == null)
        {
            return;
        }

        if (owner != null)
        {
            owner.RemoveBuff(buffKey);
        }
        owner = null;
        buffKey = null;
    }
}
