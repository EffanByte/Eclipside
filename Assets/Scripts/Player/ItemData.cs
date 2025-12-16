using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ---------------- BASE CLASS ----------------
public abstract class ItemData : ScriptableObject
{
    [Header("General Info")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;

    // The Item receives the GameObject of the user to manipulate it
    public abstract void Use(GameObject user);
}

// ---------------- 1. HEALING LOGIC ----------------
[CreateAssetMenu(menuName = "Eclipside/Items/Healing Potion")]
public class HealingItem : ItemData
{
    public float heartsToHeal; // 0.5

    public override void Use(GameObject user)
    {
        // We look for the PlayerController (or a HealthComponent)
        var player = user.GetComponent<PlayerController>();
        if (player != null)
        {
            player.Heal(heartsToHeal);
            Debug.Log($"<color=green>Used {itemName}: Healed {heartsToHeal} hearts.</color>");
        }
    }
}

// ---------------- 2. BUFF LOGIC (Speed / Luck) ----------------
[CreateAssetMenu(menuName = "Eclipside/Items/Stat Buff")]
public class StatBuffItem : ItemData
{
    public enum StatType { Speed, Luck }
    public StatType type;
    public float duration;
    public float percentageAmount; // 0.12 for 12%

    public override void Use(GameObject user)
    {
        var player = user.GetComponent<PlayerController>();
        if (player != null)
        {
            // We need to run a Coroutine, but ScriptableObjects can't do that.
            // We tell the Player (MonoBehaviour) to run the logic defined here.
            player.StartCoroutine(ApplyBuffRoutine(player));
        }
    }

    private IEnumerator ApplyBuffRoutine(PlayerController player)
    {
        Debug.Log($"<color=cyan>Buff Active: {type}</color>");
        
        if (type == StatType.Speed)
            player.ModifySpeed(percentageAmount); // Increase
        else if (type == StatType.Luck)
            player.ToggleLuck(true);

        yield return new WaitForSeconds(duration);

        if (type == StatType.Speed)
            player.ModifySpeed(-percentageAmount); // Decrease (Revert)
        else if (type == StatType.Luck)
            player.ToggleLuck(false);
            
        Debug.Log($"Buff Ended: {type}");
    }
}

// ---------------- 3. COMBAT LOGIC (Thunder Stone) ----------------
[CreateAssetMenu(menuName = "Eclipside/Items/Thunder Stone")]
public class ThunderItem : ItemData
{
    public float damage;
    public float range = 10f;
    public GameObject lightningVFXPrefab; // Drag a particle prefab here later

    public override void Use(GameObject user)
    {
        // The logic for finding enemies is HERE, not in PlayerController
        Collider2D[] hits = Physics2D.OverlapCircleAll(user.transform.position, range);
        List<Collider2D> enemies = new List<Collider2D>();

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy")) enemies.Add(hit);
        }

        if (enemies.Count > 0)
        {
            var target = enemies[Random.Range(0, enemies.Count)];
            
            // Logic to deal damage
            // target.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            
            // Visuals
            if(lightningVFXPrefab != null) 
                Instantiate(lightningVFXPrefab, target.transform.position, Quaternion.identity);

            Debug.Log($"<color=yellow>THUNDER struck {target.name}!</color>");
        }
        else
        {
            Debug.Log("Thunder Stone used, but no targets found.");
        }
    }
}