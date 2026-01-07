using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Cure Status")]
public class EffectCureStatus : ItemEffect
{
    public StatusType statusToRemove; // Select Poison

    public override void Apply(PlayerController player)
    {
        player.CureStatus(statusToRemove);
    }
}