using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Aracnola : EnemyBase
{
    [Header("Aracnola Settings")]
    public bool isElite = false;
    public GameObject webProjectilePrefab;
    
    // --- Web Management ---
    private Queue<GameObject> activeWebs = new Queue<GameObject>();
    private int maxWebs = 2;

    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        base.Start();
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 45f;
        stats.damage = 7.5f; // 0.75 hearts
        stats.moveSpeed = 3.0f; // Remember to multiply by 0.1 if using world speed
        stats.attackCooldown = 2.8f;
        maxWebs = 3;
    }

    // Called by the projectile when it hits the ground
    public void RegisterNewWeb(GameObject webPuddle)
    {
        // Enforce the hard limit of webs per Aracnola
        if (activeWebs.Count >= maxWebs)
        {
            GameObject oldestWeb = activeWebs.Dequeue();
            if (oldestWeb != null) Destroy(oldestWeb);
        }
        
        activeWebs.Enqueue(webPuddle);
    }

    protected override void LogicChasing()
    {
        if (playerTarget == null || statusMgr.HasStatus(StatusType.Freeze)) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        Vector2 dirToPlayer = (playerTarget.position - transform.position).normalized;

        if (dist > 0.8f) 
        {
            MoveTowardsTarget(dirToPlayer); // Approach
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Hold ground at preferred range
        }

        spriteRenderer.flipX = dirToPlayer.x > 0;

        if (dist <= 3f && isAttackReady && !isAttackRoutineRunning && dist >= 0.3f)
        {
            ChangeState(EnemyState.Attacking);
        }
    }

    protected override IEnumerator AttackWindup()
    {
        yield return new WaitForSeconds(stats.attackWindup);
    }
        protected override void ExecuteAttack()
    {
        if (playerTarget != null && webProjectilePrefab != null)
        {
            Vector2 aimDir = (playerTarget.position - transform.position).normalized;
            GameObject webProj = Instantiate(webProjectilePrefab, transform.position, Quaternion.identity);
            
            SpiderWebProjectile webScript = webProj.GetComponent<SpiderWebProjectile>();
            
            // Assuming 'isWalking' is public in PlayerController
            if (isElite && PlayerController.Instance.isWalking) 
            {
                stats.projectileSpeed *= 1.20f; 
            }

            webScript.Setup(this, aimDir, stats.projectileSpeed, stats.damage, isElite); 
        }
    }

    // TWIST: Weakness to Fire
    public override void ReceiveDamage(DamageInfo dmg)
    {
        if (dmg.element == DamageElement.Fire)
        {
            dmg.amount *= 2f; // Takes double damage
            // statusMgr.FlashSpriteRoutine(DamageElement.Fire);
        }
        base.ReceiveDamage(dmg);
    }
}