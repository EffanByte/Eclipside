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

    // --- State ---
    public int CurrentWave { get; private set; } = 1;
    public int MaxWaves { get; private set; } = 10;
    
    private int enemiesAlive = 0;
    private bool tookDamageThisWave = false;

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
        bossPrefab = currentBiome.bossPool[Random.Range(0, currentBiome.bossPool.Count)];
    }

    // ---------------------------------------------------------
    // WAVE EXECUTION (Called by GameDirector when timer hits 0)
    // ---------------------------------------------------------
    public void TriggerWave(float difficultyMultiplier)
    {
        // Tell Director we are locking the shop
        GameDirector.Instance.NotifyWaveStarted();

        int count = 1; // testing
        if (CurrentWave == MaxWaves)
        {
            Debug.Log("<color=magenta>BOSS WAVE TRIGGERED!</color>");
            // Boss Wave Logic
            if (bossPrefab != null)
            {
                SpawnBoss(difficultyMultiplier); // This will spawn the boss because of the CurrentWave == MaxWaveCount check in SpawnBiomeEnemy
                return; // Skip spawning regular enemies this wave
            }
        }
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
}