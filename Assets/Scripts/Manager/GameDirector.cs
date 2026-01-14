using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public List<GameObject> zonePrefabs;
    [SerializeField] private Transform zoneSpawnPointContainer;
    [SerializeField] private GameObject timedChestPrefab;
    [SerializeField] private float chestSpawnRadius = 5f;

    // Events
    public event Action<bool> OnCombatStateChanged; 
    public event Action<int> OnWaveAdvanced; 
    public event Action OnLevelCompleted;    

    // State
    public bool IsWaveActive { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    public int CurrentWave { get; private set; } = 1;
    
    private float currentTimerValue; 
    private float currentDifficulty = 1.0f;
    
    private WaveManager waveManager;
    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        waveManager = GetComponent<WaveManager>();
    }

    private void Start()
    {
        SpawnZones();
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
        
        SpawnWaveReward();

        // Check Victory Condition
        if (CurrentWave >= maxWaveCount)
        {
            Debug.Log("LEVEL COMPLETE!");
            OnLevelCompleted?.Invoke();
            StopAllCoroutines(); 
        }
    }

    // ... (Keep SpawnZones / SpawnWaveReward / API code same as before) ...
    
    private void SpawnZones()
    {
        if (zoneSpawnPointContainer == null) return;
        foreach (Transform spot in zoneSpawnPointContainer)
        {
            if(zonePrefabs.Count > 0)
                Instantiate(zonePrefabs[Random.Range(0, zonePrefabs.Count)], spot.position, Quaternion.identity);
        }
    }

    private void SpawnWaveReward()
    {
        if (timedChestPrefab)
        {
            playerTransform = PlayerController.Instance.transform;
            Vector2 offset = Random.insideUnitCircle * chestSpawnRadius;
            Instantiate(timedChestPrefab, playerTransform.position + (Vector3)offset, Quaternion.identity);
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
    public float GetTimer() => currentTimerValue;
}