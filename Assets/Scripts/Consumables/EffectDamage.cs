using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Damage")]
public class EffectDamage : ItemEffect
{
    public float damageAmount; // 0.5, 1.0, etc.

    public override void Apply(PlayerController player, string source = "Damage")
    {
        if (player != null)
        {
            DamageInfo damageInfo = new DamageInfo(damageAmount, DamageElement.Physical, AttackStyle.Environment, player.transform.position);
            player.ReceiveDamage(damageInfo);
        }
    }
}