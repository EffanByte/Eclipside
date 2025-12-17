using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour
{
    private DamageInfo currentDamageInfo;
    private List<Collider2D> hitList = new List<Collider2D>();
    private Collider2D myCollider;
    
    // Cache the filter to avoid creating garbage memory every attack
    private ContactFilter2D filter;
    private List<Collider2D> overlapResults = new List<Collider2D>();

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        myCollider.enabled = false; 
        myCollider.isTrigger = true;
        
        // Set up a filter that checks everything (or specific layers if you prefer)
        filter = new ContactFilter2D().NoFilter();
    }

    public void Start()
    {
                currentDamageInfo = new DamageInfo(
            amount: 5f,
            element: DamageElement.Physical,
            style: AttackStyle.MeleeLight,
            sourcePosition: transform.position,
            knockbackForce: 0f,
            isCritical: false
        );
        hitList.Clear();
        myCollider.enabled = true; // Ensure it's on

        Physics2D.OverlapCollider(myCollider, filter, overlapResults);

        foreach (var col in overlapResults)
        {
            // Manually process the hit
            Debug.Log("Hit detected on initialization: " + col.name);
            TryDealDamage(col);
        }
    }

    public void DisableHitbox()
    {
        myCollider.enabled = false;
    }

    // Unity calls this automatically when something enters
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealDamage(collision);
    }

    // Extracted logic so we can call it from both OnTriggerEnter and Initialize
    private void TryDealDamage(Collider2D collision)
    {

        EnemyBase target = collision.GetComponent<EnemyBase>();

        if (target != null)
        {
                target.ReceiveDamage(currentDamageInfo);
        }
    }
}