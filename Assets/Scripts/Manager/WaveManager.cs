using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private List<GameObject> enemyPrefabs; 
    [SerializeField] private float spawnDistance = 12f;
    public float spawnStagger = 0.5f;

    private Transform playerTransform;
    private int enemiesAlive = 0; // Tracks active enemies
    private bool tookDamageThisWave = false;

    private void Start()
    {
        if (PlayerController.Instance != null)
            playerTransform = PlayerController.Instance.transform; // start position for now, will need to be changed
    }

    private void OnEnable()
    {
        EnemyBase.OnEnemyKilled += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyKilled -= HandleEnemyDeath;
    }

    // Called by GameDirector
    public void TriggerWave(int waveNumber, float difficultyMultiplier)
    {
        if (GameDirector.Instance != null)
            GameDirector.Instance.NotifyWaveStarted();

        int count = Mathf.CeilToInt(3 * difficultyMultiplier);
        Debug.Log($"[WaveManager] Wave {waveNumber} Started! Enemies: {count}");

        StartCoroutine(SpawnRoutine(count, difficultyMultiplier));

        // SAFE CHECK
        if (!tookDamageThisWave && StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncrementStat("PERFECT_WAVES");
            
        // Reset flag for the new wave!
        tookDamageThisWave = false; 
    }

    private IEnumerator SpawnRoutine(int count, float difficulty)
    {
        for (int i = 0; i < count; i++)
        {
            if (GameDirector.Instance.IsPaused) yield break;

            SpawnEnemy(difficulty, spawnDistance);
            yield return new WaitForSeconds(spawnStagger);
        }
    }

    public void SpawnEnemy(float difficulty, float distance = 10f)
    {
        if (playerTransform == null || enemyPrefabs.Count == 0) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = playerTransform.position + (Vector3)(randomDir * distance);

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Track Alive Count
        enemiesAlive++;

        EnemyBase enemyScript = enemyObj.GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
             enemyScript.ApplyDifficultyScaling(difficulty); 
        }
    }

    private void HandleEnemyDeath(EnemyBase enemy)
    {
        if (enemiesAlive > 0)
        {
            enemiesAlive--;
            StatisticsManager.Instance.IncrementStat("KILLS_REGULAR");
            if (enemiesAlive <= 0)
            {
                GameDirector.Instance.NotifyWaveFinished();
            }
        }
    }

    public void TookDamageThisWave()
    {
        tookDamageThisWave = true;
        
    }
}