using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Stat Boost")]
public class WeaponStatBoost : WeaponEffect
{
    public StatType statToBuff;
    public float amount; 

    public override void OnEquip(PlayerController player)
    {
        player.ModifyPlayerStat(statToBuff, amount);
    }

    public override void OnUnequip(PlayerController player)
    {
        player.ModifyPlayerStat(statToBuff, -amount); // Remove the buff
    }
}