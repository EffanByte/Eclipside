using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
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
    public int CurrentBiomeIndex => currentBiomeIndex;
    public float CurrentDifficultyValue => currentDifficultyVal;
    
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

        if (campaignBiomes == null || campaignBiomes.Count == 0)
        {
            Debug.LogError("No Biomes assigned to GameDirector!");
            return;
        }

        if (!RunSceneTransitionState.HasActiveRun)
        {
            RunSceneTransitionState.BeginNewRun();
        }

        currentDifficultyVal = RunSceneTransitionState.CurrentDifficultyValue;
        LoadBiome(RunSceneTransitionState.CurrentBiomeIndex);
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnPlayerDeath -= CompleteRun;
        }

        if (waveManager != null)
        {
            waveManager.OnWaveFinished -= WaveFinishedRoutine;
        }

        OnWaveAdvanced -= TriggerWave;

        if (Instance == this)
        {
            Instance = null;
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
        RunSceneTransitionState.SetBiomeState(currentBiomeIndex, currentDifficultyVal);

        if (TryLoadBiomeScene(currentBiome))
        {
            return;
        }

        PrepareSceneForBiomeLoad();

        Debug.Log($"=== ENTERING BIOME: {currentBiome.biomeName} ===");
        BiomeTitleOverlay.Show(currentBiome.biomeName);

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
        int nextBiomeIndex = currentBiomeIndex + 1;

        if (nextBiomeIndex >= campaignBiomes.Count)
        {
            Debug.Log("VICTORY! All Biomes Cleared!");
            RunSceneTransitionState.Clear();
            CompleteRun();
        }
        else
        {
            LoadBiome(nextBiomeIndex);
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
        RunSceneTransitionState.Clear();

        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncrementStat("RUNS_COMPLETED");
        else
            Debug.Log("Statistics Manager not initialized");
    }

    public float GetTimer() => currentTimerValue;

    private bool TryLoadBiomeScene(BiomeData biome)
    {
        if (biome == null || string.IsNullOrWhiteSpace(biome.sceneName))
        {
            return false;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (string.Equals(activeSceneName, biome.sceneName, StringComparison.Ordinal))
        {
            return false;
        }

        Debug.Log($"[GameDirector] Loading biome scene '{biome.sceneName}' for '{biome.biomeName}'.");
        SceneManager.LoadScene(biome.sceneName);
        return true;
    }

    private void PrepareSceneForBiomeLoad()
    {
        StopAllCoroutines();
        ZoneSpawner.ClearZones();

        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        TimedChest[] chests = FindObjectsByType<TimedChest>(FindObjectsSortMode.None);
        foreach (TimedChest chest in chests)
        {
            if (chest != null)
            {
                Destroy(chest.gameObject);
            }
        }

        BiomePortal[] portals = FindObjectsByType<BiomePortal>(FindObjectsSortMode.None);
        foreach (BiomePortal portal in portals)
        {
            if (portal != null)
            {
                Destroy(portal.gameObject);
            }
        }
    }
}
