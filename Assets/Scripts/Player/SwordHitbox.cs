using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour
{
    // We will inject the damage data when the player attacks
    private DamageInfo currentDamageInfo;
    private List<Collider2D> hitList = new List<Collider2D>();
    private Collider2D myCollider;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        myCollider.enabled = false; // Off by default
        myCollider.isTrigger = true;
    }

    public void Initialize(DamageInfo info)
    {
        currentDamageInfo = info;
        hitList.Clear();
        myCollider.enabled = true; // Turn ON
    }

    public void DisableHitbox()
    {
        myCollider.enabled = false; // Turn OFF
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Check for IDamageable (Interface we made earlier)
        IDamageable target = collision.GetComponent<IDamageable>();

        if (target != null && !hitList.Contains(collision))
        {
            hitList.Add(collision);
            
            // 2. Deal the damage
            target.ReceiveDamage(currentDamageInfo);
        }
    }
}