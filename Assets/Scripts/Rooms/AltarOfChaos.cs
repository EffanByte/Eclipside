using UnityEngine;
using System;

public class AltarOfChaos : EventObject
{   
    [SerializeField] private GameObject lootPedestalPrefab;
    [SerializeField] private ItemData reward;
    
    // --- TRACKING ---
    private string uniqueBuffKey; // Ensures this specific altar's buff doesn't overwrite others
    private bool isBuffActive = false;
    private int waveActivatedOn = -1; // Tracks when the buff started

    protected void Start()
    {
        // Generate a unique ID for THIS specific altar in the world
        uniqueBuffKey = "AltarDamage_" + Guid.NewGuid().ToString().Substring(0, 8);
        
        if (GameDirector.Instance != null)
        {
            GameDirector.Instance.OnWaveAdvanced += OnWaveAdvanced; 
        }
    }

    private void OnDestroy() // Changed from OnDisable to ensure it cleans up properly
    {
        if (GameDirector.Instance != null)
        {
            GameDirector.Instance.OnWaveAdvanced -= OnWaveAdvanced;
        }
        
        // Failsafe: If the altar is destroyed (e.g. room despawns) before the wave ends,
        // we must remove the buff so the player doesn't keep it forever!
        RemoveBuff();
    }
    
    protected override void PerformEvent(PlayerController player)
    {
        AlterOfChaos();
    }

    private void AlterOfChaos()
    {
        float chance = UnityEngine.Random.Range(0f, 1f);
        
        // Cost: Sacrifice 1 heart (10 HP) and lose Max Health (Optional based on your GDD, 
        // but here is the damage part).
        DamageInfo info = new DamageInfo(
            amount: 10f,
            element: DamageElement.True,
            style: AttackStyle.Environment,     
            sourcePosition: transform.position,
            knockbackForce: 0f
        );
        PlayerController.Instance.ReceiveDamage(info);
        
        // Also deduct Max Health based on your GDD: "Risk: Lose maximum health"
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, -1f); 

        if (chance < 0.5f)
        {
            // ----------------------------------------------------
            // REWARD A: +30% Damage for 1 Wave
            // ----------------------------------------------------
            
            // Assuming your ApplyPermanentBuff handles percentage logic now
            // We use ApplyPermanentBuff instead of ApplyBuff with 9999s, because we 
            // want precise control over when it ends (when the wave advances).
            PlayerController.Instance.ApplyPermanentBuff(uniqueBuffKey, StatType.BaseDamage, 0.30f); 
            
            isBuffActive = true;
            waveActivatedOn = GameDirector.Instance.CurrentWave;
            
            Debug.Log($"[Altar] Player gained 30% damage buff! (Key: {uniqueBuffKey})");
        }
        else
        {
            // ----------------------------------------------------
            // REWARD B: Epic Consumable
            // ----------------------------------------------------
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
            
            LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
            if (pedestal != null)
            {
                pedestal.Setup(reward); // Make sure 'reward' is an Epic ItemData in the inspector
            }
            
            Debug.Log("[Altar] Player received an Epic Consumable!");
        }
    }

    // Called every time the wave changes
    private void OnWaveAdvanced(int newWaveNumber)
    {
        if (isBuffActive)
        {
            // Check if 1 full wave has passed since we activated it.
            // If activated on Wave 3, it expires when Wave 4 starts.
            if (newWaveNumber > waveActivatedOn)
            {
                RemoveBuff();
            }
        }
    }

    private void RemoveBuff()
    {
        if (isBuffActive && PlayerController.Instance != null)
        {
            PlayerController.Instance.RemoveBuff(uniqueBuffKey);
            isBuffActive = false;
            Debug.Log($"[Altar] Damage buff expired. (Key: {uniqueBuffKey})");
        }
    }
}