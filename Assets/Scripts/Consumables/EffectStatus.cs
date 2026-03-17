using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Apply Status")]
public class EffectApplyStatus : ItemEffect
{
    public StatusType statusToApply;
    
    // Note: Since this is used by Enemies against the Player, 
    // it expects the PlayerController to have a TryAddStatus method.
    public override void Apply(PlayerController player, string sourceID = "")
    {
        if (player != null)
        {
            player.TryAddStatus(statusToApply);
        }
    }
}