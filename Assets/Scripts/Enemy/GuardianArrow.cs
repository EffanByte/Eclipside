using UnityEngine;

public class GuardianArrow : MonoBehaviour
{
    private float damage;
    private ShadowLine activeShadow;
    private Rigidbody2D rb;

    public void Setup(Vector2 dir, float speed, float dmg, bool isShadowArrow, GameObject shadowPrefab, float width)
    {
        damage = dmg;
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * speed;

        // Rotate arrow
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 45f, Vector3.forward); // +45 offset for sprite alignment

        // If this is the special first shot, spawn the shadow line
        if (isShadowArrow && shadowPrefab != null)
        {
            GameObject lineObj = Instantiate(shadowPrefab, Vector3.zero, Quaternion.identity);
            activeShadow = lineObj.GetComponent<ShadowLine>();
            activeShadow.StartGrowing(transform.position, transform, width);
        }
        
        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            DamageInfo info = new DamageInfo(damage, DamageElement.Physical, AttackStyle.Ranged, transform.position, 1f);
            PlayerController.Instance.ReceiveDamage(info);
            
            // GDD: "If it hits the player... No ground effect is created."
            if (activeShadow != null) Destroy(activeShadow.gameObject);

            Destroy(gameObject);
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Environment") || col.CompareTag("Wall"))
        {
            // Decouple the shadow so it stays on the ground while arrow dies
            if (activeShadow != null) activeShadow.StopGrowing();
            
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Cleanup if destroyed by max range
        if (activeShadow != null) activeShadow.StopGrowing();
    }
}