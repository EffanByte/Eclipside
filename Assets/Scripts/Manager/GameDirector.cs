using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random;

[RequireComponent(typeof(WaveManager))]
public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }
    [Header("Progression Settings")]
    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private float difficultyScaling = 0.1f; 
    [SerializeField] private int maxWaveCount = 10;

    [Header("Generation & Rewards")]
    [SerializeField] private GameObject timedChestPrefab;
    [SerializeField] private float chestSpawnRadius = 5f;

    public int CurrentDifficulty {get; private set;}
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
    public float currentDifficulty = 1.0f;
    
    private WaveManager waveManager;
    private Transform playerTransform;

    private void Awake()
    {
        Instance = this;

        waveManager = GetComponent<WaveManager>();
    }

    private void Start()
    {
        if (ChallengeManager.Instance != null)
        {
            ChallengeManager.Instance.ApplyActiveChallenges();
        }
        ZoneSpawner.SpawnZones();
        SpawnChests();
        PlayerController.Instance.OnPlayerDeath += CompleteRun;
        StartCoroutine(GameLoopRoutine());
    }

    // ---------------------------------------------------------
    // THE GAME LOOP
    // ---------------------------------------------------------
    private IEnumerator GameLoopRoutine()
    {
        // 1. Kickoff
        TriggerCurrentWave();

        while (CurrentWave <= maxWaveCount)
        {
            // 2. Timer Logic
            currentTimerValue = timeBetweenWaves;
            
            while (currentTimerValue > 0)
            {
                if (IsPaused) { yield return null; continue; }

                currentTimerValue -= Time.deltaTime;
                yield return null;
            }

            // 3. Time's Up -> Next Wave
            AdvanceWave();
        }
    }

    private void TriggerCurrentWave()
    {
        waveManager.TriggerWave(CurrentWave, currentDifficulty);
        OnWaveAdvanced?.Invoke(CurrentWave);
    }

    private void AdvanceWave()
    {
        if (CurrentWave >= maxWaveCount) return; // Wait for final clear

        CurrentWave++;
        currentDifficulty += difficultyScaling;
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

        // Check Victory Condition
        if (CurrentWave >= maxWaveCount)
        {
            Debug.Log("LEVEL COMPLETE!");
            OnLevelCompleted?.Invoke();
            StopAllCoroutines(); 
        }
    }

    private void SpawnChests(int count = 3)
    {
        if (timedChestPrefab)
        for (int i = 0; i < count; i++)
        {
            playerTransform = PlayerController.Instance.transform;
            Vector2 offset = Random.insideUnitCircle * chestSpawnRadius;
            GameObject chest = Instantiate(timedChestPrefab, playerTransform.position + (Vector3)offset, Quaternion.identity);
            TimedChest chestScript = chest.GetComponent<TimedChest>();
            chestScript.Setup(1); // not working for some reason
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * chestSpawnRadius;
            Instantiate(timedChestPrefab, spawnPos + new Vector2(Random.Range(-8,9), Random.Range(-8,6)), Quaternion.identity);
        }
    }

    public void SetMaxWaveCount(int count)
    {
        maxWaveCount = count;
    }
    public int GetMaxWaveCount()
    {
        return maxWaveCount;
    }

    private void CompleteRun()
    {
        StatisticsManager.Instance.IncrementStat("RUNS_COMPLETED");
    }
    public float GetTimer() => currentTimerValue;
}