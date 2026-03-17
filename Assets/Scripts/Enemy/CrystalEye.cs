using UnityEngine;
using System.Collections;

public class CrystalEye : EnemyBase
{
    [Header("Crystal Eye Settings")]
    public bool isElite = false;
    public GameObject crystalBeamPrefab; // The laser prefab
    
    // --- Timings & Stats ---
    private float laserDuration = 2.0f;
    private float overheatDuration = 2.0f;
    private float extraDamageDuringOverheat = 1.25f; // +25%
    
    private float laserWidth = 0.15f; // 1.5 tiles * 0.1 ratio
    
    private bool isOverheating = false;
    private Vector2 lockedFiringDirection;

    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        base.Start();
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 50f;
        stats.contactDamage = 7.5f; 
        stats.damage = 15f; // 1.5 hearts
        
        stats.attackWindup = 0.8f;
        laserDuration = 2.5f;
        overheatDuration = 2.5f;
        extraDamageDuringOverheat = 1.35f; // +35%
        
        // Base stats like speed and aggro should be handled via the Inspector/EnemyStats scaling
    }

    // ---------------------------------------------------------
    // THE ATTACK PIPELINE
    // ---------------------------------------------------------

    protected override IEnumerator AttackWindup()
    {
        rb.linearVelocity = Vector2.zero; // Stop moving
        
        if (anim != null) anim.SetTrigger("Attack"); 
        
        // Lock the direction at the start of the charge (or end, depending on preference)
        // GDD: "Locks a firing direction toward the player's current position"
        lockedFiringDirection = (playerTarget.position - transform.position).normalized;

        // Optional: Spawn a small "Charging" particle effect here

        yield return new WaitForSeconds(stats.attackWindup);
    }

    protected override void ExecuteAttack()
    {
        // 2. FIRE BEAM
        if (crystalBeamPrefab != null)
        {
            GameObject beamObj = Instantiate(crystalBeamPrefab, transform.position, Quaternion.identity);
            CrystalBeam beamScript = beamObj.GetComponent<CrystalBeam>();
            
            if (beamScript != null)
            {
                beamScript.FireBeam(transform.position, lockedFiringDirection, laserWidth, stats.damage, laserDuration);
            }
        }
    }

    protected override IEnumerator AttackRecovery()
    {
        // 3. OVERHEAT (The Twist)
        isOverheating = true;
        
        // Visuals: Dim glow, turn gray
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.gray; 

        // Wait out the self-stun penalty
        yield return new WaitForSeconds(overheatDuration + laserDuration);

        // Recover
        spriteRenderer.color = originalColor;
        isOverheating = false;
        
    }

    // ---------------------------------------------------------
    // TWIST: VULNERABILITY
    // ---------------------------------------------------------

    protected override float ModifyIncomingDamage(float currentCalculatedDamage, DamageInfo dmg)
    {
        if (isOverheating)
        {
            // Apply bonus damage if hit during the Overheat recovery phase
            float boostedDamage = currentCalculatedDamage * extraDamageDuringOverheat;
            Debug.Log($"[Crystal Eye] Overheat Vulnerability! Took {boostedDamage} instead of {currentCalculatedDamage}");
            return boostedDamage;
        }
        return currentCalculatedDamage;
    }
}