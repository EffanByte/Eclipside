using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))] // IsTrigger = true
public class GrovewellPond : MonoBehaviour
{
    [Header("Pond Settings")]
    [SerializeField] private float radiusTiles = 2.5f;
    [SerializeField] private float duration = 10.0f;
    [SerializeField] private float healRatePerSecond = 3.5f; // 0.35 hearts

    private Sylvara owner;
    private bool isSylvaraInside = false;
    private const float TILE = 0.3f;

    public void Initialize(Sylvara sylvaraReference)
    {
        owner = sylvaraReference;
        
        // Scale visual/collider to match GDD radius
        float worldRadius = radiusTiles * TILE;
        transform.localScale = new Vector3(worldRadius * 2f, worldRadius * 2f, 1f);

        StartCoroutine(LifeRoutine());
        StartCoroutine(HealRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private IEnumerator HealRoutine()
    {
        // Heal tick loop
        while (true)
        {
            if (isSylvaraInside && owner != null && owner.currentState != EnemyState.Dead)
            {
                // Healing does not exceed max health
                if (owner.GetCurrentHealth() < owner.GetMaxHealth())
                {
                    // Negative damage = Heal in most systems, but we can just manipulate it
                    // Assuming EnemyBase doesn't have a direct Heal method, we do it safely:
                    DamageInfo healInfo = new DamageInfo(-healRatePerSecond, DamageElement.True, AttackStyle.Environment, transform.position, 0f);
                    owner.ReceiveDamage(healInfo); 
                    
                    // Flash green
                    owner.StartCoroutine(owner.GetComponent<StatusManager>().FlashSpriteRoutine(DamageElement.Psychic));
                }
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    // Called by Sylvara when hit by Fire
    public void BurnAway()
    {
        StopAllCoroutines();
        // Optional: Burn VFX here
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner != null && collision.gameObject == owner.gameObject)
        {
            isSylvaraInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (owner != null && collision.gameObject == owner.gameObject)
        {
            isSylvaraInside = false;
        }
    }
}