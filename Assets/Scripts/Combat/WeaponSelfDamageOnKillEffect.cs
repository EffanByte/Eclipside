using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Effects/Self Damage On Kill")]
public class WeaponSelfDamageOnKillEffect : WeaponEffect
{
    public int killThreshold = 3;
    public float selfDamageAmount = 2f;

    private PlayerController owner;
    private int killCounter;

    public override void OnEquip(PlayerController player)
    {
        EnemyBase.OnEnemyKilled -= HandleEnemyKilled;
        owner = player;
        killCounter = 0;
        EnemyBase.OnEnemyKilled += HandleEnemyKilled;
    }

    public override void OnUnequip(PlayerController player)
    {
        EnemyBase.OnEnemyKilled -= HandleEnemyKilled;

        if (owner == player)
        {
            owner = null;
            killCounter = 0;
        }
    }

    private void HandleEnemyKilled(EnemyBase enemy)
    {
        if (owner == null || killThreshold <= 0)
        {
            return;
        }

        killCounter++;
        if (killCounter < killThreshold)
        {
            return;
        }

        killCounter = 0;
        owner.StatusDamage(new DamageInfo(selfDamageAmount, DamageElement.True, AttackStyle.Environment, owner.transform.position));
        Debug.Log($"[Weapon] {name} dealt {selfDamageAmount} self-damage after {killThreshold} kills.");
    }
}
