using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Eclipside/Item Effects/Spawn Pool")]
public class EffectSpawnPool : ItemEffect
{
    [Header("Pool Configuration")]
    public GameObject poolPrefab; // The circle sprite with AreaEffectPool script
    public float duration = 10f;
    public float tickRate = 1.0f; // Apply effects every 1 second

    [Header("Effects")]
    [Tooltip("Applied every second while inside (e.g., Healing)")]
    public List<ItemEffect> effectsPerSecond = new List<ItemEffect>();

    [Tooltip("Applied once when entering (e.g., Attack Debuff)")]
    public List<ItemEffect> effectsOnEnter = new List<ItemEffect>();

    public override void Apply(PlayerController player)
    {

        // 1. Spawn the pool at player's feet
        GameObject poolObj = Instantiate(poolPrefab, player.transform.position, Quaternion.identity);

        // 2. Get the script
        AreaEffectPool poolScript = poolObj.GetComponent<AreaEffectPool>();

        // 3. Inject the logic
        if (poolScript != null)
        {
            poolScript.Initialize(duration, tickRate, effectsPerSecond, effectsOnEnter);
            Debug.Log($"Spawned Pool: {poolObj.name}");
        }
        else
        {
            Debug.LogError("Pool Prefab missing AreaEffectPool script!");
        }
    }
}