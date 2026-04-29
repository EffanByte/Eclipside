using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }

    [Header("Campaign Settings")]
    [SerializeField] private List<BiomeData> campaignBiomes;
    private int currentBiomeIndex = 0;

    [Header("Progression Settings")]
    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private float difficultyScaling = 0.1f; 
    [SerializeField] private float difficultyCurveWeight = 0.05f;
    [SerializeField] private int maxWaveCount = 10; // Will be overridden by BiomeData

    [Header("Progression Objects")]
    [Tooltip("The Portal prefab that spawns when the biome loads")]
    [SerializeField] private GameObject biomePortalPrefab;

    [Header("Generation & Rewards")]
    [SerializeField] private GameObject timedChestPrefab;
    [SerializeField] private float chestSpawnRadius = 5f;

    [Header("Boss Health Bar")]
    [SerializeField] private Sprite bossHealthFillSprite;
    [SerializeField] private Sprite bossHealthFrameSprite;
    [SerializeField] private Sprite bossHealthBackgroundSprite;
    
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
    private BiomePortal activePortal;
    private bool bossEncounterTriggered;
    private bool bossDefeatedForBiome;
    private readonly Dictionary<Tilemap, Color> baseTilemapColors = new Dictionary<Tilemap, Color>();

#if UNITY_EDITOR
    private const string BossHealthBarPath = "Assets/UI/healthbar.png";
    private const string BossHealthBarFramePath = "Assets/UI/healthbar_dragon_frame.png";
#endif

    private void Awake()
    {
        Instance = this;
        waveManager = gameObject.AddComponent<WaveManager>();

        if (GetComponent<SceneBoundaryWalls>() == null)
        {
            gameObject.AddComponent<SceneBoundaryWalls>();
        }

        BossHealthBarUI bossHealthBarUi = GetComponent<BossHealthBarUI>();
        if (bossHealthBarUi == null)
        {
            bossHealthBarUi = gameObject.AddComponent<BossHealthBarUI>();
        }

        bossHealthBarUi.Configure(bossHealthFillSprite, bossHealthFrameSprite, bossHealthBackgroundSprite);
    }

    private void Start()
    {
        if (ChallengeManager.Instance != null)
        {
            ChallengeManager.Instance.ApplyActiveChallenges();
        }

        PlayerController.Instance.OnPlayerDeath += CompleteRun;
        waveManager.OnWaveFinished += WaveFinishedRoutine;
        BossBase.OnBossDefeated += HandleBossDefeated;
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

        currentTimerValue = RunSceneTransitionState.CurrentRunTimeSeconds;
        currentDifficultyVal = CalculateDifficultyFromTime(currentTimerValue);
        LoadBiome(RunSceneTransitionState.CurrentBiomeIndex);
    }

    private void Update()
    {
        if (IsPaused)
        {
            return;
        }

        currentTimerValue += Time.deltaTime;
        currentDifficultyVal = CalculateDifficultyFromTime(currentTimerValue);

        if (RunSceneTransitionState.HasActiveRun)
        {
            RunSceneTransitionState.SetBiomeState(currentBiomeIndex, currentDifficultyVal, currentTimerValue);
        }
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

        BossBase.OnBossDefeated -= HandleBossDefeated;
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
        RunSceneTransitionState.SetBiomeState(currentBiomeIndex, currentDifficultyVal, currentTimerValue);
        maxWaveCount = Mathf.Max(1, currentBiome.wavesToClear);
        bossEncounterTriggered = false;
        bossDefeatedForBiome = false;
        playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;

        PrepareSceneForBiomeLoad();
        ApplyBiomeTileTint(currentBiome);

        Debug.Log($"=== ENTERING BIOME: {currentBiome.GetDisplayName()} ===");
        BiomeTitleOverlay.Show(currentBiome);

        // Tell WaveManager what enemies to use
        waveManager.InitializeBiome(currentBiome);

        ZoneSpawner.SpawnZones();

        SpawnBiomePortal();
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

        if (bossEncounterTriggered)
        {
            return;
        }

        if (waveManager.CurrentWave > maxWaveCount)
        {
            Debug.Log($"All waves cleared in {campaignBiomes[currentBiomeIndex].biomeName}. Activate the portal to summon the boss.");
            StopAllCoroutines(); 
            return;
        }
        else
            OnWaveAdvanced?.Invoke();
    }

    private void SpawnBiomePortal()
    {
        if (activePortal != null)
        {
            Destroy(activePortal.gameObject);
            activePortal = null;
        }

        if (biomePortalPrefab != null && playerTransform != null)
        {
            // Spawn the portal a few units away from the player
            Vector3 spawnPos = playerTransform.position + new Vector3(0, 3f, 0); 
            GameObject portalObj = Instantiate(biomePortalPrefab, spawnPos, Quaternion.identity);
            activePortal = portalObj.GetComponent<BiomePortal>();
        }
        else
        {
            Debug.LogWarning("No Portal Prefab assigned.");
        }
    }

    public void HandlePortalInteraction()
    {
        if (bossDefeatedForBiome)
        {
            AdvanceToNextBiome();
            return;
        }

        if (bossEncounterTriggered)
        {
            Debug.Log("Boss encounter already active.");
            return;
        }

        if (!waveManager.HasBossAvailable())
        {
            Debug.LogWarning("No boss configured for this biome. Advancing immediately.");
            AdvanceToNextBiome();
            return;
        }

        Debug.Log($"Summoning biome boss for {campaignBiomes[currentBiomeIndex].biomeName}.");
        bossEncounterTriggered = true;
        ClearActiveEnemies();
        waveManager.ResetCombatTracking();
        waveManager.StartBossEncounter(currentDifficultyVal);
    }

    public string GetPortalPrompt()
    {
        if (bossDefeatedForBiome)
        {
            return L("portal.enter", "Enter Portal");
        }

        if (bossEncounterTriggered)
        {
            return L("portal.boss_active", "Boss Active");
        }

        return L("portal.challenge_boss", "Challenge Boss");
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

    private void HandleBossDefeated(BossBase boss)
    {
        if (!bossEncounterTriggered || bossDefeatedForBiome)
        {
            return;
        }

        bossEncounterTriggered = false;
        bossDefeatedForBiome = true;
        IsWaveActive = false;
        OnCombatStateChanged?.Invoke(false);
        OnLevelCompleted?.Invoke();

        Debug.Log($"Boss defeated in {campaignBiomes[currentBiomeIndex].biomeName}. Portal is now ready.");
    }

    private float CalculateDifficultyFromTime(float elapsedSeconds)
    {
        float elapsedSteps = Mathf.Max(0f, elapsedSeconds / Mathf.Max(1f, timeBetweenWaves));
        float linearRamp = elapsedSteps * difficultyScaling;
        float curveRamp = Mathf.Pow(elapsedSteps, 1.35f) * difficultyCurveWeight;
        return Mathf.Max(1f, 1f + linearRamp + curveRamp);
    }

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

    private void ApplyBiomeTileTint(BiomeData biome)
    {
        if (biome == null)
        {
            return;
        }

        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null)
            {
                continue;
            }

            if (!baseTilemapColors.ContainsKey(tilemap))
            {
                baseTilemapColors[tilemap] = tilemap.color;
            }

            Color baseColor = baseTilemapColors[tilemap];
            Color tint = biome.tileTint;
            tilemap.color = new Color(
                baseColor.r * tint.r,
                baseColor.g * tint.g,
                baseColor.b * tint.b,
                baseColor.a * tint.a);
        }
    }

    private void PrepareSceneForBiomeLoad()
    {
        StopAllCoroutines();
        ZoneSpawner.ClearZones();
        waveManager.ResetCombatTracking();
        activePortal = null;

        ClearActiveEnemies();

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

    private void ClearActiveEnemies()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    private string L(string key, string fallback, params object[] args)
    {
        return LocalizationManager.GetString(LocalizationManager.DefaultTable, key, fallback, args);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveBossHealthBarSprites();
    }

    private void ResolveBossHealthBarSprites()
    {
        if (bossHealthFillSprite == null || bossHealthBackgroundSprite == null)
        {
            UnityEngine.Object[] barAssets = AssetDatabase.LoadAllAssetsAtPath(BossHealthBarPath);
            for (int i = 0; i < barAssets.Length; i++)
            {
                Sprite sprite = barAssets[i] as Sprite;
                if (sprite == null)
                {
                    continue;
                }

                if (bossHealthFillSprite == null && string.Equals(sprite.name, "healthbar_3", StringComparison.Ordinal))
                {
                    bossHealthFillSprite = sprite;
                }
                else if (bossHealthBackgroundSprite == null && string.Equals(sprite.name, "healthbar_0", StringComparison.Ordinal))
                {
                    bossHealthBackgroundSprite = sprite;
                }
            }
        }

        if (bossHealthFrameSprite == null)
        {
            UnityEngine.Object[] frameAssets = AssetDatabase.LoadAllAssetsAtPath(BossHealthBarFramePath);
            for (int i = 0; i < frameAssets.Length; i++)
            {
                Sprite sprite = frameAssets[i] as Sprite;
                if (sprite != null && string.Equals(sprite.name, "healthbar_dragon_frame_0", StringComparison.Ordinal))
                {
                    bossHealthFrameSprite = sprite;
                    break;
                }
            }
        }
    }
#endif
}
