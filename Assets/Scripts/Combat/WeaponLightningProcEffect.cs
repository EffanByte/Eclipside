using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Lightning Proc")]
public class WeaponLightningProcEffect : WeaponEffect
{
    public float chance = 0.05f;
    public float damageAmount = 15f;
    public DamageElement damageElement = DamageElement.Magic;
    public AttackStyle attackStyle = AttackStyle.Environment;

    public override void OnHit(PlayerController player, EnemyBase target, ref DamageInfo dmgInfo)
    {
        if (Random.value > chance)
        {
            return;
        }

        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        List<EnemyBase> validEnemies = new List<EnemyBase>();
        foreach (EnemyBase enemy in allEnemies)
        {
            if (enemy == null || enemy.currentState == EnemyState.Dead || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            validEnemies.Add(enemy);
        }

        if (validEnemies.Count == 0)
        {
            return;
        }

        EnemyBase randomTarget = validEnemies[Random.Range(0, validEnemies.Count)];
        Vector2 sourcePosition = player != null ? (Vector2)player.transform.position : Vector2.zero;
        randomTarget.ReceiveDamage(new DamageInfo(damageAmount, damageElement, attackStyle, sourcePosition));
        Debug.Log($"[Weapon] Lightning proc hit {randomTarget.name} for {damageAmount}.");
    }
}
