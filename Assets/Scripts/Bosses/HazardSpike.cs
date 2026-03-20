using UnityEngine;

[RequireComponent(typeof(Collider2D))] // IsTrigger = true
public class HazardSpike : MonoBehaviour
{
    private float damage;

    public void Setup(float dmgAmount, float duration)
    {
        damage = dmgAmount;
        
        // Optional: Play emerging animation here
        
        Destroy(gameObject, duration);
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            // Single hit logic requires a debounce or using ReceiveDamage's built-in i-frames
            DamageInfo info = new DamageInfo(damage, DamageElement.Physical, AttackStyle.Environment, transform.position, 0f);
            col.GetComponent<PlayerController>()?.ReceiveDamage(info);
        }
    }
}