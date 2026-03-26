using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour
{
    private DamageInfo currentDamageInfo;
    private Collider2D myCollider;
    private ContactFilter2D filter;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>();
    private PlayerController playerController;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        myCollider.enabled = false;
        myCollider.isTrigger = true;
        playerController = GetComponentInParent<PlayerController>();
        filter = ContactFilter2D.noFilter;
    }

    public void EnableHitbox()
    {
        if (myCollider == null)
        {
            return;
        }

        overlapResults.Clear();
        myCollider.enabled = true;
        Physics2D.OverlapCollider(myCollider, filter, overlapResults);

        foreach (Collider2D overlap in overlapResults)
        {
            TryDealDamage(overlap);
        }
    }

    public void DisableHitbox()
    {
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealDamage(collision);
    }

    private void TryDealDamage(Collider2D collision)
    {
        if (playerController == null || playerController.currentWeapon == null)
        {
            return;
        }

        EnemyBase target = collision.GetComponent<EnemyBase>() ?? collision.GetComponentInParent<EnemyBase>();
        if (target == null)
        {
            return;
        }
        currentDamageInfo = playerController.currentWeapon.GetDamageInfoOnHit(playerController, target);
        target.ReceiveDamage(currentDamageInfo);
        playerController.NotifyWeaponHit(target, currentDamageInfo);
    }
}
