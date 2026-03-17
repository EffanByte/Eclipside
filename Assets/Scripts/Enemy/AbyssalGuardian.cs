using UnityEngine;
using System.Collections;

public class AbyssalGuardian : EnemyBase
{
    [Header("Guardian Config")]
    public bool isElite = false;
    public GameObject guardianArrowPrefab;
    public GameObject shadowLinePrefab;

    private bool hasFiredShadowLine = false;
    private const float TILE = 0.3f; 

    private void Awake()
    {
        if (isElite)
        {
            stats.maxHealth = 45f;      
            stats.moveSpeed = 8.0f;
            stats.damage = 10f;         
            stats.attackCooldown = 1.4f;
            stats.attackWindup = 0.45f; // 0.45s elite windup
            stats.aggroRadius = 11f;
            stats.preferredRangeMin = 6f;
            stats.preferredRangeMax = 9f;
            stats.contactDamage = 7.5f;
        }
    }

    protected override void Start()
    {
        base.Start();
        hasFiredShadowLine = false;
    }


    protected override IEnumerator AttackWindup()
    {
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(stats.attackWindup);
    }

    protected override void ExecuteAttack()
    {
        float dist = Vector2.Distance(transform.position, playerTarget.position);
        
        if (dist <= 1.6f * 0.1f) 
        {
            // Jab
            PerformAttack(playerTarget.gameObject);
        }
        else 
        {
            // Throw
            FireSpear();
        }
    }
    private void FireSpear()
    {
        Vector2 aimDir = (playerTarget.position - transform.position).normalized;
        GameObject spear = Instantiate(guardianArrowPrefab, transform.position, Quaternion.identity);
        
        spear.GetComponent<GuardianArrow>().Setup(aimDir, 12f * TILE, stats.damage, !hasFiredShadowLine, shadowLinePrefab, 0.25f * TILE);
        
        hasFiredShadowLine = true; // First shot logic
    }
}

