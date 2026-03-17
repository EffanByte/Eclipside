using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

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

    // --- Biome Data ---
    private List<GameObject> commonEnemyPool;
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
    }

    // ---------------------------------------------------------
    // WAVE EXECUTION (Called by GameDirector when timer hits 0)
    // ---------------------------------------------------------
    public void TriggerNextWave(float difficultyMultiplier)
    {
        // Tell Director we are locking the shop
        GameDirector.Instance.NotifyWaveStarted();

        int count = Mathf.CeilToInt(3 * difficultyMultiplier);
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
            
            if (StatisticsManager.Instance != null)
                StatisticsManager.Instance.IncrementStat("KILLS_REGULAR");
                
            // WAVE CLEAR LOGIC
            if (enemiesAlive <= 0)
            {
                // Give Perfect Wave Achievement
                if (!tookDamageThisWave && StatisticsManager.Instance != null)
                    StatisticsManager.Instance.IncrementStat("PERFECT_WAVES");

                // Tell Director to unlock shops and spawn chests
                GameDirector.Instance.NotifyWaveFinished();

                // Advance our internal counter for the next time Director calls TriggerNextWave
                CurrentWave++; 
                tookDamageThisWave = false; 
            }
        }
    }

    public void TookDamageThisWave()
    {
        tookDamageThisWave = true;
    }
}