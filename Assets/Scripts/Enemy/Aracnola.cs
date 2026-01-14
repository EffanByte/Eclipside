using UnityEngine;
using System.Collections;

public class Aracnola : EnemyBase
{
    [Header("Aracnola Specifics")]
    [SerializeField] private GameObject webPrefab;
    [SerializeField] private float webShootRange = 5f;
    [SerializeField] private float webCooldown = 4f;

    private float lastWebTime;

    void Awake()
    {
         movementType = MovementType.Rotate;
    }
    protected override void Start()
    {
        base.Start();
    }

    // ----------------------------------------------------------------------
    // STATE OVERRIDES
    // ----------------------------------------------------------------------

protected override void LogicChasing()
{
    if (playerTarget == null) return;

    // Calculate direction ONCE
    Vector2 direction = (playerTarget.position - transform.position).normalized;

    // Pass direction to movement
    MoveTowardsTarget(direction);

        // Rotate to face player
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Assumption: Sprite faces RIGHT. If sprite faces UP, use (angle - 90)
        transform.rotation = Quaternion.AngleAxis(angle + 90 , Vector3.forward);
                // 3. Special Ability: Shoot Web
        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        
        if (distToPlayer <= webShootRange && Time.time > lastWebTime + webCooldown)
        {
            StartCoroutine(ShootWebRoutine());
        }
            
}




    // ----------------------------------------------------------------------
    // ABILITIES
    // ----------------------------------------------------------------------

    private IEnumerator ShootWebRoutine()
    {
        lastWebTime = Time.time;
        
        // Optional: Pause movement briefly to shoot
        float originalSpeed = stats.moveSpeed;
        stats.moveSpeed = 0; 
        
        // Visual feedback (Color flash or specific anim trigger)
        spriteRenderer.color = Color.cyan; 

        yield return new WaitForSeconds(0.3f); // Wind up

        if (webPrefab != null && playerTarget != null)
        {
            GameObject web = Instantiate(webPrefab, transform.position, Quaternion.identity);
            SpiderWeb script = web.GetComponent<SpiderWeb>();
            
            // Calculate direction
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            
            if (script != null) script.Setup(dir, stats.projectileSpeed);
        }

        // Return to normal
        spriteRenderer.color = Color.white;
        stats.moveSpeed = originalSpeed;
    }

    // ----------------------------------------------------------------------
    // WEAKNESS IMPLEMENTATION
    // ----------------------------------------------------------------------

    public override void ReceiveDamage(DamageInfo dmg)
    {
        // "Weakness: Fire" - Spiders take double damage from fire
        if (dmg.element == DamageElement.Fire)
        {
            dmg.amount *= 2f; 
            
            // Visual flair for critical weakness
            StartCoroutine(BurnEffect());
            
            Debug.Log($"<color=red>Aracnola took Critical Fire Damage: {dmg.amount}</color>");
        }

        // Pass the modified damage to the base class to handle HP reduction and Death
        base.ReceiveDamage(dmg);
    }

    private IEnumerator BurnEffect()
    {
        // Simple visual jitter to show panic
        spriteRenderer.color = new Color(1f, 0.4f, 0f); // Orange/Burn color
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }
}