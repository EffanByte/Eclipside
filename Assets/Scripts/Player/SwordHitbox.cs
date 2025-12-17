using UnityEngine;
using System.Collections.Generic;

public class SwordHitbox : MonoBehaviour
{
    // Reference to the player so we know WHICH weapon is equipped
    [SerializeField] private PlayerController player;
    
    // List to prevent hitting the same enemy twice in one swing
    private List<Collider2D> hitList = new List<Collider2D>();

    // This runs every time the Animation turns the object ON
    private void OnEnable()
    {
        hitList.Clear();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Check if we hit an enemy
        if (collision.CompareTag("Enemy"))
        {
            // 2. Check if we already hit them this swing
            if (!hitList.Contains(collision))
            {
                hitList.Add(collision);

                // 3. Get damage from the Player's equipped weapon (ScriptableObject)
                if (player.currentWeapon != null)
                {
                    float dmg = player.currentWeapon.damage;
                    float knockback = player.currentWeapon.knockbackForce;

                    Debug.Log($"Hit {collision.name} with {player.currentWeapon.weaponName} for {dmg} Damage!");

                    // TODO: Actually apply damage
                    // collision.GetComponent<EnemyHealth>()?.TakeDamage(dmg);
                }
            }
        }
    }
}