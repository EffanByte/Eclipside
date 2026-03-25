using System.Collections;
using UnityEngine;

public class CharacterSupportZone : MonoBehaviour
{
    private PlayerController targetPlayer;
    private float duration;
    private float healPerSecond;
    private float damageReduction;
    private string buffKey;
    private bool playerInside;

    public void Initialize(PlayerController player, float radius, float zoneDuration, float healAmountPerSecond, float defenseReduction)
    {
        targetPlayer = player;
        duration = zoneDuration;
        healPerSecond = healAmountPerSecond;
        damageReduction = defenseReduction;
        buffKey = "SupportZone_" + GetInstanceID();

        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = radius;

        StartCoroutine(LifetimeRoutine());
        StartCoroutine(HealingRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);

        if (playerInside && targetPlayer != null)
        {
            targetPlayer.RemoveBuff(buffKey);
        }

        Destroy(gameObject);
    }

    private IEnumerator HealingRoutine()
    {
        while (true)
        {
            if (playerInside && targetPlayer != null)
            {
                targetPlayer.Heal(healPerSecond);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>() ?? collision.GetComponentInParent<PlayerController>();
        if (player == null || player != targetPlayer)
        {
            return;
        }

        playerInside = true;
        targetPlayer.ApplyPermanentBuff(buffKey, StatType.Defense, damageReduction);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>() ?? collision.GetComponentInParent<PlayerController>();
        if (player == null || player != targetPlayer)
        {
            return;
        }

        playerInside = false;
        targetPlayer.RemoveBuff(buffKey);
    }
}
