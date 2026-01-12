using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random; // Resolve ambiguity

[RequireComponent(typeof(WaveManager))]
public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }

    [Header("Progression Settings")]
    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private float difficultyScaling = 0.1f; // +10% per wave

    [Header("Level Generation (Zones)")]
    [Tooltip("Prefabs for Shops, Elite Arenas, Shrines, etc.")]
    [SerializeField] private List<GameObject> zonePrefabs;
    
    [Tooltip("Drag the parent object that holds all empty SpawnPoint objects")]
    [SerializeField] private Transform zoneSpawnPointContainer;

    [Header("Rewards")]
    [SerializeField] private GameObject timedChestPrefab;
    [SerializeField] private float chestSpawnRadius = 5f;

    // --- Events & State ---
    public event Action<bool> OnCombatStateChanged; 
    public bool IsWaveActive { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    
    public int CurrentWave { get; private set; } = 1;
    public float CurrentDifficulty { get; private set; } = 1.0f;
    
    private float waveTimer;
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // 1. Generate the Map Zones
        SpawnZones();

        // 2. Start Game Loop
        waveTimer = timeBetweenWaves;
        waveManager.TriggerWave(CurrentWave, CurrentDifficulty);
    }

    private void Update()
    {
        if (IsPaused) return;

        waveTimer -= Time.deltaTime;

        if (waveTimer <= 0)
        {
            AdvanceWave();
        }
    }

    // ---------------------------------------------------------
    // ZONE GENERATION LOGIC
    // ---------------------------------------------------------
    private void SpawnZones()
    {
        if (zoneSpawnPointContainer == null || zonePrefabs.Count == 0) return;

        // Get all child transforms as spawn points
        List<Transform> points = new List<Transform>();
        foreach (Transform child in zoneSpawnPointContainer)
        {
            points.Add(child);
        }

        // Shuffle logic or just iterate
        foreach (Transform spot in points)
        {
            // Pick a random zone type (Shop, Shrine, etc)
            GameObject prefabToSpawn = zonePrefabs[Random.Range(0, zonePrefabs.Count)];
            
            // Instantiate at the point
            Instantiate(prefabToSpawn, spot.position, Quaternion.identity);
        }
    }

    // ---------------------------------------------------------
    // WAVE LOGIC
    // ---------------------------------------------------------
    private void AdvanceWave()
    {
        waveTimer = timeBetweenWaves;
        CurrentWave++;
        CurrentDifficulty += difficultyScaling;
        waveManager.TriggerWave(CurrentWave, CurrentDifficulty);
    }

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
    }

    private void SpawnWaveReward()
    {
        if (timedChestPrefab == null || playerTransform == null) return;

        // "Spawn around the player position +-5 x and y"
        Vector2 randomOffset = Random.insideUnitCircle * chestSpawnRadius;
        Vector3 spawnPos = playerTransform.position + (Vector3)randomOffset;

        Instantiate(timedChestPrefab, spawnPos, Quaternion.identity);
        Debug.Log("Wave Cleared! Chest Spawned.");
    }

    public float GetTimer() => waveTimer;
}