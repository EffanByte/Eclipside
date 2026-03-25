using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Wave Damage Buff")]
public class EffectWaveDamageBuff : ItemEffect
{
    public float healthCost = 5f;
    public float damageBuffAmount = 0.3f;
    public int wavesDuration = 1;
    public bool buffBaseDamage = true;
    public bool buffMagicDamage = true;
    public bool buffHeavyDamage = true;

    public override void Apply(PlayerController player, string source = "Wave Damage Buff")
    {
        if (player == null)
        {
            return;
        }

        if (healthCost > 0f)
        {
            DamageInfo cost = new DamageInfo(healthCost, DamageElement.True, AttackStyle.Environment, player.transform.position);
            player.ReceiveDamage(cost);

            if (player.IsDead)
            {
                Debug.Log($"[{source}] Damage buff cancelled because the health cost was lethal.");
                return;
            }
        }

        string buffPrefix = $"{source}_WaveDamage_{Guid.NewGuid():N}";
        if (buffBaseDamage)
        {
            player.ApplyBuffForWaves($"{buffPrefix}_Base", StatType.BaseDamage, damageBuffAmount, wavesDuration);
        }

        if (buffMagicDamage)
        {
            player.ApplyBuffForWaves($"{buffPrefix}_Magic", StatType.MagicDamage, damageBuffAmount, wavesDuration);
        }

        if (buffHeavyDamage)
        {
            player.ApplyBuffForWaves($"{buffPrefix}_Heavy", StatType.HeavyDamage, damageBuffAmount, wavesDuration);
        }

        Debug.Log($"[{source}] Applied +{damageBuffAmount:P0} damage buffs for {wavesDuration} wave(s) at a cost of {healthCost} health.");
    }
}
