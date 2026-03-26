using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class MagicProjectile : MonoBehaviour
{
    private readonly HashSet<int> hitTargets = new HashSet<int>();

    private float speed;
    private Rigidbody2D rb;
    private Transform target;
    private EnemyBase targetEnemy;
    private PlayerController owner;
    private WeaponData weaponData;
    private int remainingPierces;
    private float lifetime = 3f;

    [Header("Homing Stats")]
    public float homingStrength = 1.5f;
    public float detectionRadius = 5f;

    public static void Spawn(PlayerController player, WeaponData weapon)
    {
        if (player == null || weapon == null)
        {
            return;
        }

        Vector2 direction = player.GetLastMovementDirection().normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        GameObject projectileObject = new GameObject($"{weapon.name}_Projectile");
        projectileObject.transform.position = player.transform.position + (Vector3)(direction * 0.75f);

        Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.24f;

        MagicProjectile projectile = projectileObject.AddComponent<MagicProjectile>();
        projectile.Setup(player, weapon, direction);
    }

    public void Setup(PlayerController player, WeaponData weapon, Vector2 direction)
    {
        owner = player;
        weaponData = weapon;
        speed = Mathf.Max(4f, weapon.ProjectileSpeed * (player != null ? player.GetProjectileSpeedMultiplier() : 1f));

        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;

        FindNearestTarget();
        weapon.ConfigureProjectile(player, this);

        Destroy(gameObject, lifetime);
    }

    public EnemyBase GetCurrentTargetEnemy()
    {
        return targetEnemy;
    }

    public void MultiplySpeed(float multiplier)
    {
        speed = Mathf.Max(1f, speed * Mathf.Max(0.01f, multiplier));
        if (rb != null && rb.linearVelocity != Vector2.zero)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }

    public void AddPierceCount(int additionalPierces)
    {
        remainingPierces += Mathf.Max(0, additionalPierces);
    }

    public void MultiplyColliderRadius(float multiplier)
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            circle.radius *= Mathf.Max(0.1f, multiplier);
        }
    }

    public void SetLifetime(float newLifetime)
    {
        lifetime = Mathf.Max(0.1f, newLifetime);
    }

    private void FindNearestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        float closestDist = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>() ?? hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.currentState == EnemyState.Dead)
            {
                continue;
            }

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                target = enemy.transform;
                targetEnemy = enemy;
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || target == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        Vector2 newVelocity = Vector2.Lerp(rb.linearVelocity.normalized, direction, homingStrength * Time.fixedDeltaTime);
        rb.linearVelocity = newVelocity * speed;

        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyBase enemy = collision.GetComponent<EnemyBase>() ?? collision.GetComponentInParent<EnemyBase>();
        if (enemy == null || weaponData == null || owner == null)
        {
            return;
        }

        int enemyId = enemy.GetInstanceID();
        if (hitTargets.Contains(enemyId))
        {
            return;
        }

        hitTargets.Add(enemyId);
        DamageInfo info = weaponData.GetDamageInfoOnHit(owner, enemy);
        enemy.ReceiveDamage(info);
        owner.NotifyWeaponHit(enemy, info);

        if (remainingPierces > 0)
        {
            remainingPierces--;
            return;
        }

        Destroy(gameObject);
    }
}
