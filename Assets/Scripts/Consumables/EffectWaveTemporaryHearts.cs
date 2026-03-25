using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Wave Temporary Hearts")]
public class EffectWaveTemporaryHearts : ItemEffect
{
    public float heartsAmount = 20f;
    public int wavesDuration = 1;

    public override void Apply(PlayerController player, string source = "Wave Temp Hearts")
    {
        if (player == null)
        {
            return;
        }

        string effectKey = $"{source}_TempHearts_{Guid.NewGuid():N}";
        player.AddTemporaryHeartsForWaves(effectKey, heartsAmount, wavesDuration);
        Debug.Log($"[{source}] Granted {heartsAmount} temporary health for {wavesDuration} wave(s).");
    }
}
