using UnityEngine;
using System.Collections;

public class BarkGuardian : EnemyBase
{
    [Header("Bark Guardian Specifics")]
    [Tooltip("How close the player must be to wake up this enemy.")]
    [SerializeField] private float wakeUpRange = 3.5f;
    
    [Tooltip("Color when camouflaged/dormant.")]
    [SerializeField] private Color dormantColor = new Color(0.5f, 0.4f, 0.3f); // Dark wood color

    protected override void Start()
    {
        // 1. Setup Specific Stats (Or set these in Inspector)
        // PDF Page 28: "Resistance but low damage"
        stats.maxHealth = 40f;          // High HP
        stats.damage = 5f;          // Low Damage (0.5 Hearts)
        stats.moveSpeed = 1.8f;     // Slow
        stats.enemyTag = "Forest_Plant"; // Important for Fire Weakness Logic
        
        // PDF Page 28: "Camouflaged"
        // Start visually dormant
        if (spriteRenderer != null) spriteRenderer.color = dormantColor;

        base.Start(); // Initializes RB, HP, etc.
    }

    // ----------------------------------------------------------------------
    // STATE OVERRIDES
    // ----------------------------------------------------------------------

    // Override Idle to handle "Camouflage/Ambush"
    protected override void LogicIdle()
    {
        if (playerTarget == null) return;

        // Calculate distance to player
        float dist = Vector2.Distance(transform.position, playerTarget.position);

        // Wake up if player is too close OR if we took damage (currentHp < max)
        if (dist < wakeUpRange || currentHealth < stats.maxHealth)
        {
            StartCoroutine(WakeUpRoutine());
        }
    }

    // We do NOT override LogicChasing or LogicAttacking.
    // The standard EnemyBase melee logic is perfect for this enemy.

    // ----------------------------------------------------------------------
    // SPECIAL BEHAVIOR
    // ----------------------------------------------------------------------

    private IEnumerator WakeUpRoutine()
    {
        // Prevent re-triggering
        ChangeState(EnemyState.Stunned); // Temporary state so it doesn't move

        // Visual "Wake Up" feedback
        // In a real game, play an animation: anim.SetTrigger("WakeUp");
        spriteRenderer.color = Color.white; // Restore normal color
        
        // Small shake effect or delay before moving
        float shakeDuration = 0.5f;
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            transform.position = originalPos + (Vector3)Random.insideUnitCircle * 0.1f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        // Officially start chasing
        ChangeState(EnemyState.Chasing);
    }

    // ----------------------------------------------------------------------
    // DAMAGE OVERRIDE (Optional Visuals)
    // ----------------------------------------------------------------------

    // The Base class handles the math and death. 
    // We override this ONLY to add specific visual flair for the Fire Weakness.
    public override void ReceiveDamage(DamageInfo dmg)
    {
        base.ReceiveDamage(dmg); // Let the parent do the math and HP reduction

        // PDF Page 28: "Weakness: Fire"
        if (dmg.element == DamageElement.Fire)
        {
            // Visual feedback specifically for burning wood
            StartCoroutine(BurnFlashRoutine());
        }
    }

    private IEnumerator BurnFlashRoutine()
    {
        spriteRenderer.color = Color.red; // Flash Red
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = Color.white; // Return to normal
    }
}