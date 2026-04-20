using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public class WaveManager : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private float spawnDistance = 12f; // unused value right now
    public float spawnStagger = 0.5f;
    [SerializeField] private int baseEnemiesPerWave = 8;
    [SerializeField] private float extraEnemyEveryDifficulty = 0.35f;

    // --- State ---
    public int CurrentWave { get; private set; } = 1;
    public int MaxWaves { get; private set; } = 10;
    
    private int enemiesAlive = 0;
    private bool tookDamageThisWave = false;
    private bool bossEncounterActive = false;

    public event Action<int> OnWaveFinished;

    // --- Biome Data ---
    private List<GameObject> commonEnemyPool;
    private GameObject bossPrefab;
    private void Start()
    {
    }

    private void OnEnable() => EnemyBase.OnEnemyKilled += HandleEnemyDeath;
    private void OnDisable() => EnemyBase.OnEnemyKilled -= HandleEnemyDeath;

    // ---------------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------------
    public void InitializeBiome(BiomeData currentBiome)
    {
        commonEnemyPool = currentBiome.commonEnemies;
         
        // Setup Wave Counters for this Biome
        MaxWaves = currentBiome.wavesToClear;
        CurrentWave = 1; 
        
        enemiesAlive = 0;     
        tookDamageThisWave = false;
        bossEncounterActive = false;
        bossPrefab = currentBiome.bossPool != null && currentBiome.bossPool.Count > 0
            ? currentBiome.bossPool[Random.Range(0, currentBiome.bossPool.Count)]
            : null;
    }

    // ---------------------------------------------------------
    // WAVE EXECUTION (Called by GameDirector when timer hits 0)
    // ---------------------------------------------------------
    public void TriggerWave(float difficultyMultiplier)
    {
        if (bossEncounterActive)
        {
            return;
        }

        // Tell Director we are locking the shop
        GameDirector.Instance.NotifyWaveStarted();

        int count = CalculateWaveEnemyCount(difficultyMultiplier);
        StartCoroutine(SpawnRoutine(count, difficultyMultiplier));
    }

    private IEnumerator SpawnRoutine(int count, float difficulty)
    {
        for (int i = 0; i < count; i++)
        {
            // Pause spawning if player enters shop
            if (GameDirector.Instance.IsPaused) yield return new WaitUntil(() => !GameDirector.Instance.IsPaused);

            SpawnBiomeEnemy(difficulty);
            yield return new WaitForSeconds(spawnStagger);
        }
    }

    public void SpawnBiomeEnemy(float difficulty)
    {
        if (commonEnemyPool == null || commonEnemyPool.Count == 0) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = (Vector3)(randomDir * spawnDistance);

        // Decide Common vs Elite
        GameObject prefabToSpawn;

        prefabToSpawn = commonEnemyPool[Random.Range(0, commonEnemyPool.Count)];

        GameObject enemyObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        enemiesAlive++;

        EnemyBase enemyScript = enemyObj.GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
             enemyScript.ApplyDifficultyScaling(difficulty); 
        }
    }

    public void SpawnBoss(float difficulty)
    {
        if (bossPrefab == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = (Vector3)(randomDir * spawnDistance);

        GameObject bossObj = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        enemiesAlive++;

        EnemyBase bossScript = bossObj.GetComponent<EnemyBase>();
        if (bossScript != null)
        {
            bossScript.ApplyDifficultyScaling(difficulty);
        }
    }

    public bool HasBossAvailable()
    {
        return bossPrefab != null;
    }

    public void StartBossEncounter(float difficulty)
    {
        if (bossEncounterActive || bossPrefab == null)
        {
            return;
        }

        StopAllCoroutines();
        enemiesAlive = 0;
        tookDamageThisWave = false;
        bossEncounterActive = true;

        GameDirector.Instance.NotifyWaveStarted();
        SpawnBoss(difficulty);
    }

    public void SpawnEnemyAroundPlayer(float difficulty, float distance = 8f, List<Vector2> spawnPoints = null)
    {
        if (commonEnemyPool == null || commonEnemyPool.Count == 0) return;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3 spawnPos;

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            Vector2 randomPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            spawnPos = playerPos + (Vector3)randomPoint.normalized * distance;
        }
        else
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            spawnPos = playerPos + (Vector3)(randomDir * distance);
        }

        GameObject prefabToSpawn = commonEnemyPool[Random.Range(0, commonEnemyPool.Count)];

        GameObject enemyObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        enemiesAlive++;

        EnemyBase enemyScript = enemyObj.GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
             enemyScript.ApplyDifficultyScaling(difficulty); 
        }
    }
    // ---------------------------------------------------------
    // COMBAT TRACKING
    // ---------------------------------------------------------
    private void HandleEnemyDeath(EnemyBase enemy)
    {
        if (enemiesAlive > 0)
        {
            enemiesAlive--;
            Debug.Log($"Enemy Killed! {enemiesAlive} remaining.");  
            if (StatisticsManager.Instance != null)
                StatisticsManager.Instance.IncrementStat("KILLS_REGULAR");

            // WAVE CLEAR LOGIC
            if (enemiesAlive <= 0)
            {
                if (bossEncounterActive)
                {
                    bossEncounterActive = false;
                    tookDamageThisWave = false;
                    return;
                }

                // Give Perfect Wave Achievement
                if (!tookDamageThisWave && StatisticsManager.Instance != null)
                    StatisticsManager.Instance.IncrementStat("PERFECT_WAVES");
                CurrentWave++;
                OnWaveFinished?.Invoke(CurrentWave);

                // Advance our internal counter for the next time Director calls TriggerNextWave
                tookDamageThisWave = false; 
            }
        }
    }

    public void TookDamageThisWave()
    {
        tookDamageThisWave = true;
    }

    public void ResetCombatTracking()
    {
        StopAllCoroutines();
        enemiesAlive = 0;
        tookDamageThisWave = false;
        bossEncounterActive = false;
    }

    private int CalculateWaveEnemyCount(float difficultyMultiplier)
    {
        float difficultyOverBase = Mathf.Max(0f, difficultyMultiplier - 1f);
        int bonusCount = Mathf.FloorToInt(difficultyOverBase / Mathf.Max(0.01f, extraEnemyEveryDifficulty));
        return Mathf.Max(1, baseEnemiesPerWave + bonusCount);
    }
}
