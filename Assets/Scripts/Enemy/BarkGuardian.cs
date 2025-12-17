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
        movementType = MovementType.Flip;
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
            WakeUpRoutine();

    }

    // We do NOT override LogicChasing or LogicAttacking.
    // The standard EnemyBase melee logic is perfect for this enemy.

    // ----------------------------------------------------------------------
    // SPECIAL BEHAVIOR
    // ----------------------------------------------------------------------

    private void WakeUpRoutine()
    {
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
    }

}