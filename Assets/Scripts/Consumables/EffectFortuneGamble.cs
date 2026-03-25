using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Max HP Gamble")]
public class EffectMaxHPGamble : ItemEffect
{
    [Range(0f, 1f)] public float winChance = 0.75f;
    public int winAmount = 1;  // +1 Heart Container
    public int loseAmount = -1; // -1 Heart Container
    private EffectHeal HealthAdded = new EffectHeal { heartsAmount = 10f, isTemporary = false };
    private EffectHeal HealthRemoved = new EffectHeal { heartsAmount = -10f, isTemporary = false };
    public override void Apply(PlayerController player, string source = "Fortune Cookie")
    {
        bool win = Random.value <= winChance;
        if (win)
            HealthAdded.Apply(player);
        else
            HealthRemoved.Apply(player);
        Debug.Log(win ? "Fortune Cookie: Win!" : "Fortune Cookie: Lose...");
    }
}