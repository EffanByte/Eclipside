using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))] // Ensure LineRenderer exists
public class BarkGuardian : EnemyBase
{
    [Header("Bark Guardian Settings")]
    public bool isElite = false;
    
    // --- LineRenderer Telegraph ---
    private LineRenderer telegraphLine;
    private int segments = 20; // How smooth the arc is
    [SerializeField] private float arcAngle = 120f; // 120 degree cone from GDD
    [SerializeField] private float slamRange = 0.75f;
    private float currentFacingDirectionX = 1f;

    protected override void Start()
    {
        if (isElite) ApplyEliteStats();
        
        telegraphLine = GetComponent<LineRenderer>();
        telegraphLine.positionCount = 0; // Hide initially
        telegraphLine.useWorldSpace = false; // Draw relative to the Guardian
        telegraphLine.loop = true; // Connect the last point back to the center
        
        // Setup LineRenderer visuals (You can also do this in Inspector)
        telegraphLine.startWidth = 0.05f;
        telegraphLine.endWidth = 0.05f;
        telegraphLine.material = new Material(Shader.Find("Sprites/Default"));
        telegraphLine.startColor = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent Red
        telegraphLine.endColor = new Color(1f, 0f, 0f, 0.5f);
        
        base.Start();
    }

    private void ApplyEliteStats()
    {
        stats.maxHealth = 70f;
        stats.contactDamage = 10f; 
        stats.damage = 17.5f;      
        stats.moveSpeed = 2.0f;
        stats.attackCooldown = 3.5f;
    }

    protected override void HandleSpriteRotation(Vector2 direction)
    {
        if (currentState == EnemyState.Attacking || isAttackRoutineRunning) return;

        if (direction != Vector2.zero)
        {
            currentFacingDirectionX = direction.x > 0 ? 1f : -1f;

            if (movementType == MovementType.Flip)
            {
                spriteRenderer.flipX = currentFacingDirectionX < 0; 
            }
            else if (movementType == MovementType.Rotate)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    protected override float ModifyIncomingDamage(float currentCalculatedDamage, DamageInfo dmg)
    {
        float playerRelativeX = dmg.sourcePosition.x - transform.position.x;
        float playerDirX = playerRelativeX > 0 ? 1f : -1f;

        bool hitFromFront = (currentFacingDirectionX == playerDirX);

        if (hitFromFront)
        {
            float reduction = isElite ? 0.4f : 0.5f; 
            return currentCalculatedDamage * reduction;
        }

        return currentCalculatedDamage;
    }

    // ---------------------------------------------------------
    // ATTACK PIPELINE & TELEGRAPH
    // ---------------------------------------------------------

    protected override IEnumerator AttackWindup()
    {
        rb.linearVelocity = Vector2.zero;

        
        if (anim != null) anim.SetTrigger("Attack");

        // 1. Draw the Telegraph
        DrawAttackTelegraph();

        // 2. Wait for windup
        yield return new WaitForSeconds(stats.attackWindup);

        // 3. Hide Telegraph right before impact
        telegraphLine.positionCount = 0;
    }

    private void DrawAttackTelegraph()
    {
        // 1. Determine facing direction (0 degrees is Right, 180 is Left)
        float baseAngle = currentFacingDirectionX > 0 ? 0f : 180f;

        // 2. We need points for the Origin (0,0), the arc, and back to Origin.
        // Array size = Center Point + Arc Points
        telegraphLine.positionCount = segments + 2; 

        // 3. Set Center Point (Origin in local space)
        telegraphLine.SetPosition(0, Vector3.zero);

        // 4. Calculate Arc Points
        float halfAngle = arcAngle / 2f;
        float startAngle = baseAngle - halfAngle;
        float angleStep = arcAngle / segments;
        float radius = slamRange; // Use converted world range

        for (int i = 0; i <= segments; i++)
        {
            // Current angle in radians
            float currentAngleDeg = startAngle + (angleStep * i);
            float currentAngleRad = currentAngleDeg * Mathf.Deg2Rad;

            // Calculate X and Y on the circle
            float x = Mathf.Cos(currentAngleRad) * radius;
            float y = Mathf.Sin(currentAngleRad) * radius;

            // Set position (Index + 1 because 0 is the center)
            telegraphLine.SetPosition(i + 1, new Vector3(x, y, 0f));
        }
    }

    protected override void ExecuteAttack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stats.attackRange);

        // For precise arc detection, use the same math as the telegraph
        Vector2 facingDir = currentFacingDirectionX > 0 ? Vector2.right : Vector2.left;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Vector2 dirToPlayer = (hit.transform.position - transform.position).normalized;
                
                // If the angle between facing direction and player is within half our arc
                if (Vector2.Angle(facingDir, dirToPlayer) <= (arcAngle / 2f)) 
                {
                    PerformAttack(hit.gameObject);
                }
            }
        }
    }

    protected override IEnumerator AttackRecovery()
    {
        yield return new WaitForSeconds(0.2f);
    }
}