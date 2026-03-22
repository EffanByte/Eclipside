using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }

    [Header("Campaign Settings")]
    [SerializeField] private List<BiomeData> campaignBiomes;
    private int currentBiomeIndex = 0;

    [Header("Progression Settings")]
    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private float difficultyScaling = 0.1f; 
    [SerializeField] private int maxWaveCount = 10; // Will be overridden by BiomeData

    [Header("Progression Objects")]
    [Tooltip("The Portal prefab that spawns after the boss dies")]
    [SerializeField] private GameObject biomePortalPrefab;

    [Header("Generation & Rewards")]
    [SerializeField] private GameObject timedChestPrefab;
    [SerializeField] private float chestSpawnRadius = 5f;
    
    // Events
    public event Action<bool> OnCombatStateChanged; 
    public event Action OnWaveAdvanced; 
    public event Action OnLevelCompleted;
    public event Action OnRunCompleted;

    // State
    public bool IsWaveActive { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    
    private float currentTimerValue; 
    public float currentDifficultyVal = 1.0f;
    
    private WaveManager waveManager;
    private Transform playerTransform;

    private void Awake()
    {
        Instance = this;
        waveManager = gameObject.AddComponent<WaveManager>();
    }

    private void Start()
    {
        if (ChallengeManager.Instance != null)
        {
            ChallengeManager.Instance.ApplyActiveChallenges();
        }

        PlayerController.Instance.OnPlayerDeath += CompleteRun;
        waveManager.OnWaveFinished += WaveFinishedRoutine;
        OnWaveAdvanced += TriggerWave;
        
        // Start the first Biome
        if (campaignBiomes != null && campaignBiomes.Count > 0)
            LoadBiome(0);
        else
            Debug.LogError("No Biomes assigned to GameDirector!");
        
        
    }

    // ---------------------------------------------------------
    // BIOME LOGIC
    // ---------------------------------------------------------
    private void LoadBiome(int index)
    {
        if (index >= campaignBiomes.Count)
        {
            Debug.Log("YOU WON THE GAME! All Biomes Cleared.");
            CompleteRun();
            return;
        }

        currentBiomeIndex = index;
        BiomeData currentBiome = campaignBiomes[index];

        Debug.Log($"=== ENTERING BIOME: {currentBiome.biomeName} ===");

        // Tell WaveManager what enemies to use
        waveManager.InitializeBiome(currentBiome);

        ZoneSpawner.SpawnZones();
        
        SpawnChests();
        TriggerWave();  
    }

    // ---------------------------------------------------------
    // THE GAME LOOP
    // ---------------------------------------------------------


    private void TriggerWave()
    {
        waveManager.TriggerWave(currentDifficultyVal);
    }

    // ---------------------------------------------------------
    // CALLBACKS (Called by WaveManager)
    // ---------------------------------------------------------

    public void NotifyWaveStarted()
    {
        IsWaveActive = true;
        OnCombatStateChanged?.Invoke(true);
    }


     public void WaveFinishedRoutine(int waveCount)
    {
        IsWaveActive = false;
        OnCombatStateChanged?.Invoke(false);
        currentDifficultyVal += difficultyScaling; 
        if (waveManager.CurrentWave > maxWaveCount)
        {
            Debug.Log($"BIOME {campaignBiomes[currentBiomeIndex].biomeName} COMPLETE!");
            OnLevelCompleted?.Invoke();
            StopAllCoroutines(); 
            
            SpawnBiomePortal();
        }
        else
            OnWaveAdvanced?.Invoke();
    }

    private void SpawnBiomePortal()
    {
        if (biomePortalPrefab != null && playerTransform != null)
        {
            // Spawn the portal a few units away from the player
            Vector3 spawnPos = playerTransform.position + new Vector3(0, 3f, 0); 
            Instantiate(biomePortalPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Failsafe: If no portal prefab is assigned, just auto-advance
            Debug.LogWarning("No Portal Prefab assigned! Auto-advancing...");
            AdvanceToNextBiome();
        }
    }

    // Called by the BiomePortal.cs script when the player interacts with it
    public void AdvanceToNextBiome()
    {
        // Check if we just beat the final biome
        if (currentBiomeIndex + 1 >= campaignBiomes.Count)
        {
            Debug.Log("VICTORY! All Biomes Cleared!");
            CompleteRun();
        }
        else
        {
            // Load the next one
            LoadBiome(currentBiomeIndex + 1);
        }
    }

    private void SpawnChests(int count = 3)
    {
        if (timedChestPrefab == null) return;
        
        playerTransform = PlayerController.Instance.transform;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * chestSpawnRadius;
            GameObject chest = Instantiate(timedChestPrefab, playerTransform.position + (Vector3)offset, Quaternion.identity);
            
            TimedChest chestScript = chest.GetComponent<TimedChest>();
            if (chestScript != null) chestScript.Setup(1); 
        }
    }

    public void SetMaxWaveCount(int count) { maxWaveCount = count; }
    public int GetMaxWaveCount() { return maxWaveCount; }

    private void CompleteRun()
    {
        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncrementStat("RUNS_COMPLETED");
        else
            Debug.Log("Statistics Manager not initialized");
    }

    public float GetTimer() => currentTimerValue;
}