using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstridAshZone : MonoBehaviour
{
    private readonly Dictionary<int, float> enemyTickTimes = new Dictionary<int, float>();

    private PlayerController ownerPlayer;
    private float duration;
    private float radius;
    private float burnDuration;
    private float enemyAttackSpeedMultiplier;
    private string buffKey;
    private Coroutine playerExitRoutine;
    private bool playerBuffApplied;

    public void Initialize(PlayerCharacterRuntime runtime, PlayerController player, float zoneDuration, float zoneRadius, float ashBurnDuration, float attackSpeedMultiplier)
    {
        ownerPlayer = player;
        duration = zoneDuration;
        radius = zoneRadius;
        burnDuration = ashBurnDuration;
        enemyAttackSpeedMultiplier = attackSpeedMultiplier;
        buffKey = "AstridAsh_" + GetInstanceID();

        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = radius;

        StartCoroutine(ExpireRoutine());
    }

    private IEnumerator ExpireRoutine()
    {
        yield return new WaitForSeconds(duration);
        CleanupPlayerPenalty();
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyBase enemy = collision.GetComponent<EnemyBase>() ?? collision.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            ApplyEnemyAshEffect(enemy);
            return;
        }

        PlayerController player = collision.GetComponent<PlayerController>() ?? collision.GetComponentInParent<PlayerController>();
        if (player != null && player == ownerPlayer)
        {
            ApplyPlayerPenalty();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        EnemyBase enemy = collision.GetComponent<EnemyBase>() ?? collision.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            ApplyEnemyAshEffect(enemy);
            return;
        }

        PlayerController player = collision.GetComponent<PlayerController>() ?? collision.GetComponentInParent<PlayerController>();
        if (player != null && player == ownerPlayer)
        {
            ApplyPlayerPenalty();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>() ?? collision.GetComponentInParent<PlayerController>();
        if (player == null || player != ownerPlayer)
        {
            return;
        }

        if (playerExitRoutine != null)
        {
            StopCoroutine(playerExitRoutine);
        }

        playerExitRoutine = StartCoroutine(RemovePlayerPenaltyAfterDelay());
    }

    private void ApplyEnemyAshEffect(EnemyBase enemy)
    {
        int instanceId = enemy.GetInstanceID();
        if (enemyTickTimes.TryGetValue(instanceId, out float nextTickTime) && Time.time < nextTickTime)
        {
            return;
        }

        enemyTickTimes[instanceId] = Time.time + 1f;
        enemy.TryAddStatus(StatusType.Burn, burnDuration);
        enemy.ApplyTemporaryAttackSpeedMultiplier(enemyAttackSpeedMultiplier, 1.25f);
    }

    private void ApplyPlayerPenalty()
    {
        if (playerExitRoutine != null)
        {
            StopCoroutine(playerExitRoutine);
            playerExitRoutine = null;
        }

        if (playerBuffApplied)
        {
            return;
        }

        ownerPlayer.ApplyPermanentBuff(buffKey + "_Move", StatType.Speed, -0.08f);
        ownerPlayer.ApplyPermanentBuff(buffKey + "_Attack", StatType.AttackSpeed, -0.10f);
        playerBuffApplied = true;
    }

    private IEnumerator RemovePlayerPenaltyAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        CleanupPlayerPenalty();
        playerExitRoutine = null;
    }

    private void CleanupPlayerPenalty()
    {
        if (!playerBuffApplied || ownerPlayer == null)
        {
            return;
        }

        ownerPlayer.RemoveBuff(buffKey + "_Move");
        ownerPlayer.RemoveBuff(buffKey + "_Attack");
        playerBuffApplied = false;
    }
}
