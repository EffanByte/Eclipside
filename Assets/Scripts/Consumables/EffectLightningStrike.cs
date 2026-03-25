using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Lightning Strike")]
public class EffectLightningStrike : ItemEffect
{
    public float damageAmount = 15f;
    public DamageElement damageElement = DamageElement.Magic;
    public AttackStyle attackStyle = AttackStyle.Environment;

    public override void Apply(PlayerController player, string source = "Lightning Strike")
    {
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        List<EnemyBase> validEnemies = new List<EnemyBase>();

        foreach (EnemyBase enemy in allEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.currentState == EnemyState.Dead)
            {
                continue;
            }

            validEnemies.Add(enemy);
        }

        if (validEnemies.Count == 0)
        {
            Debug.Log($"[{source}] No valid enemy found for lightning strike.");
            return;
        }

        EnemyBase target = validEnemies[Random.Range(0, validEnemies.Count)];
        Vector2 sourcePosition = player != null ? (Vector2)player.transform.position : Vector2.zero;
        DamageInfo info = new DamageInfo(damageAmount, damageElement, attackStyle, sourcePosition);
        target.ReceiveDamage(info);
        Debug.Log($"[{source}] Lightning struck {target.name} for {damageAmount} damage.");
    }
}
