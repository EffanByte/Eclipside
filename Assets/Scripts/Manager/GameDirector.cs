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

    [Header("Generation & Rewards")]
    [SerializeField] private GameObject timedChestPrefab;
    [SerializeField] private float chestSpawnRadius = 5f;
    
    // Events
    public event Action<bool> OnCombatStateChanged; 
    public event Action<int> OnWaveAdvanced; 
    public event Action OnLevelCompleted;
    public event Action OnRunCompleted;

    // State
    public bool IsWaveActive { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    public int CurrentWave { get; private set; } = 1;
    
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
        
        // Start the first Biome
        if (campaignBiomes != null && campaignBiomes.Count > 0)
        {
            LoadBiome(0);
        }
        else
        {
            Debug.LogError("No Biomes assigned to GameDirector!");
        }
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

        // Setup wave limits based on the biome
        maxWaveCount = currentBiome.wavesToClear;
        CurrentWave = 1; // Reset wave counter for new biome

        // Tell WaveManager what enemies to use
        waveManager.InitializeBiome(currentBiome);

        // Tell ZoneSpawner what environmental objects to use
        // Note: You need to update ZoneSpawner to accept this parameter
        ZoneSpawner.SpawnZones();
        
        SpawnChests();

        StartCoroutine(GameLoopRoutine());
    }

    // ---------------------------------------------------------
    // THE GAME LOOP
    // ---------------------------------------------------------
    private IEnumerator GameLoopRoutine()
    {
        TriggerCurrentWave();

        while (CurrentWave <= maxWaveCount)
        {
            currentTimerValue = timeBetweenWaves;
            
            while (currentTimerValue > 0)
            {
                if (IsPaused) { yield return null; continue; }

                currentTimerValue -= Time.deltaTime;
                yield return null;
            }

            AdvanceWave();
        }
    }

    private void TriggerCurrentWave()
    {
        waveManager.TriggerNextWave(currentDifficultyVal);
        OnWaveAdvanced?.Invoke(CurrentWave);
    }

    private void AdvanceWave()
    {
        if (CurrentWave >= maxWaveCount) return; 

        CurrentWave++;
        currentDifficultyVal += difficultyScaling;
        TriggerCurrentWave();
    }

    // ---------------------------------------------------------
    // CALLBACKS (Called by WaveManager)
    // ---------------------------------------------------------

    public void NotifyWaveStarted()
    {
        IsWaveActive = true;
        OnCombatStateChanged?.Invoke(true);
    }

    public void NotifyWaveFinished()
    {
        IsWaveActive = false;
        OnCombatStateChanged?.Invoke(false);
        OnWaveAdvanced?.Invoke(CurrentWave);

        // Check Biome Victory Condition
        if (CurrentWave >= maxWaveCount)
        {
            Debug.Log($"BIOME {campaignBiomes[currentBiomeIndex].biomeName} COMPLETE!");
            OnLevelCompleted?.Invoke();
            StopAllCoroutines(); 
            
            // Advance to next Biome
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