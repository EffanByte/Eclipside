using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Heal")]
public class EffectHeal : ItemEffect
{
    public float heartsAmount; // 0.5, 1.0, etc.
    public bool isTemporary;   // For Glass Orb / Spirit Nectar

    public override void Apply(PlayerController player, string source = "Heal")
    {
        if (isTemporary)
            player.AddTemporaryHearts(heartsAmount);
        else
            player.Heal(heartsAmount);
    }
}