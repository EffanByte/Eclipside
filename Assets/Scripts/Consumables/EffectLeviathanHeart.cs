using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Leviathan Heart")]
public class EffectLeviathanHeart : ItemEffect
{
    [Range(0.01f, 1f)] public float reviveHealthFraction = 0.25f;
    public float invulnerabilityDuration = 5f;

    public override void Apply(PlayerController player, string source = "Leviathan Heart")
    {
        if (player == null)
        {
            return;
        }

        player.ArmRevive(source, reviveHealthFraction, invulnerabilityDuration);
    }
}
