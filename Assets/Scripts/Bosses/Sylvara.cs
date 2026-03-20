using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sylvara : BossBase
{
    [Header("Vine Lance (Attack)")]
    [SerializeField] private Vector2 vineLanceHitboxSize = new Vector2(0.9f, 8.0f);

    [Header("Grovewell Pond (Sustain)")]
    [SerializeField] private GameObject pondPrefab;
    [SerializeField] private float pondCastTime = 1.0f;
    [SerializeField] private float pondCooldown = 15.0f;
    
    [Header("Call of the Web (Summons)")]
    [SerializeField] private GameObject aracnolaPrefab;
    [SerializeField] private int maxAracnolas = 4;
    [SerializeField] private float phaseOneHpThreshold = 0.7f; // 70%
    [SerializeField] private float phaseTwoHpThreshold = 0.4f; // 40%

    // --- Internal State ---
    private GrovewellPond activePond;
    private float lastPondTime = -999f;
    private List<EnemyBase> livingSpiders = new List<EnemyBase>();
    private Vector2 lockedAttackDirection;
    
    private const float TILE = 0.3f;

    private void Awake()
    {
        bossName = "Sylvara, Spirit of the Grove";
        stats = new EnemyStats();
        
        stats.maxHealth = isElite ? 220f : 160f;
        stats.contactDamage = isElite ? 10f : 7.5f; 
        stats.damage = isElite ? 15f : 10f;         
        stats.moveSpeed = isElite ? 2.8f : 2.6f;
        
        stats.attackCooldown = 2.0f; 
        stats.preferredRangeMin = 4f * TILE;
        stats.preferredRangeMax = 7f * TILE;
        stats.attackRange = 8f * TILE; 
        stats.aggroRadius = 15f * TILE;

        stats.moveSpeed *= TILE;
    }

    // ---------------------------------------------------------
    // BOSS PHASE LOGIC (Call of the Web)
    // ---------------------------------------------------------
    protected override void CheckPhaseTransitions()
    {
        float hpPercentage = currentHealth / stats.maxHealth;

        if (currentPhase == 1 && hpPercentage <= phaseOneHpThreshold)
        {
            currentPhase = 2;
            SpawnAracnolas();
        }
        else if (currentPhase == 2 && hpPercentage <= phaseTwoHpThreshold)
        {
            currentPhase = 3;
            SpawnAracnolas();
        }
    }

    private void SpawnAracnolas()
    {
        if (aracnolaPrefab == null) return;

        livingSpiders.RemoveAll(s => s == null || s.currentState == EnemyState.Dead);
        int amountToSpawn = maxAracnolas - livingSpiders.Count;

        for (int i = 0; i < amountToSpawn; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = transform.position + (Vector3)(randomDir * (8f * TILE));
            
            GameObject spider = Instantiate(aracnolaPrefab, spawnPos, Quaternion.identity);
            livingSpiders.Add(spider.GetComponent<EnemyBase>());
        }
        Debug.Log($"[Sylvara] Phase {currentPhase} reached! Summoned {amountToSpawn} Aracnolas.");
    }

    // ---------------------------------------------------------
    // BOSS ATTACK AI
    // ---------------------------------------------------------
    protected override string ChooseNextAttack(float distanceToPlayer)
    {
        // AI Priority 1: If injured and pond is off cooldown -> Cast Pond
        if (currentHealth < stats.maxHealth && activePond == null && Time.time >= lastPondTime + pondCooldown)
        {
            return "CastPond";
        }

        // AI Priority 2: If too close (<= 3 tiles), reposition (return None to keep moving)
        if (distanceToPlayer <= 3f * TILE)
        {
            return "None";
        }

        // AI Priority 3: Otherwise, Vine Lance
        if (distanceToPlayer <= stats.attackRange)
        {
            return "VineLance";
        }

        return "None";
    }

    // ---------------------------------------------------------
    // BOSS ATTACK EXECUTION
    // ---------------------------------------------------------
    protected override IEnumerator BossAttackWindup(string attackName)
    {
        lockedAttackDirection = (playerTarget.position - transform.position).normalized;

        if (attackName == "VineLance")
        {
            if (anim != null) anim.SetTrigger("Attack"); 
            yield return new WaitForSeconds(0.75f); // 0.75s Telegraph
        }
        else if (attackName == "CastPond")
        {
            if (anim != null) anim.SetTrigger("Cast"); 
            yield return new WaitForSeconds(pondCastTime); // 1.0s Cast
        }
    }

    protected override void ExecuteBossAttack(string attackName)
    {
        if (attackName == "VineLance")
        {
            Vector2 center = (Vector2)transform.position + (lockedAttackDirection * (vineLanceHitboxSize.y * TILE) / 2f);
            Vector2 size = new Vector2(vineLanceHitboxSize.x * TILE, vineLanceHitboxSize.y * TILE);
            float angle = Mathf.Atan2(lockedAttackDirection.y, lockedAttackDirection.x) * Mathf.Rad2Deg;

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle - 90f, LayerMask.GetMask("Player"));

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    PerformAttack(hit.gameObject);
                    hit.GetComponent<PlayerController>()?.TryAddStatus(StatusType.Freeze);
                }
            }
        }
        else if (attackName == "CastPond")
        {
            if (pondPrefab != null)
            {
                GameObject pondObj = Instantiate(pondPrefab, transform.position, Quaternion.identity);
                activePond = pondObj.GetComponent<GrovewellPond>();
                if (activePond != null) activePond.Initialize(this);
            }
            lastPondTime = Time.time;
        }
    }

    protected override IEnumerator BossAttackRecovery(string attackName)
    {
        yield return null; // Immediately starts attackCooldown (2.0s)
    }

    // ---------------------------------------------------------
    // WEAKNESS LOGIC
    // ---------------------------------------------------------
    public override void ReceiveDamage(DamageInfo dmg)
    {
        base.ReceiveDamage(dmg);

        // Weakness: Fire attacks burn away her healing vines
        if (dmg.element == DamageElement.Fire && activePond != null)
        {
            activePond.BurnAway();
            activePond = null;
            Debug.Log("<color=orange>[Sylvara] Grovewell Pond burned away!</color>");
        }
    }
}