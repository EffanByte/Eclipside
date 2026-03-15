using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Stat Buff")]
public class EffectStatBuff : ItemEffect
{
    public StatType type;
    public float duration;
    public float amount; // 0.15 for 15%

    public override void Apply(PlayerController player, string source)
    {
        // You'll need to add a "ApplyBuff" method to PlayerController
        // that handles coroutines for these stats
        player.ApplyBuff(source, type, amount, duration);
    }
}   