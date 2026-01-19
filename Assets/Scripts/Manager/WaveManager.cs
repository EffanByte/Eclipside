using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private List<GameObject> enemyPrefabs; 
    [SerializeField] private float spawnDistance = 12f;
    [SerializeField] private float spawnStagger = 0.5f;

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
        // 1. Notify Director that combat started
        GameDirector.Instance.NotifyWaveStarted();

        // 2. Calculate Count
        int count = Mathf.CeilToInt(3 * difficultyMultiplier);
        
        Debug.Log($"[WaveManager] Wave {waveNumber} Started! Enemies: {count}");

        StartCoroutine(SpawnRoutine(count, difficultyMultiplier));

        if (!tookDamageThisWave)
            StatisticsManager.Instance.IncrementStat("PERFECT_WAVES");
    }

    private IEnumerator SpawnRoutine(int count, float difficulty)
    {
        for (int i = 0; i < count; i++)
        {
            if (GameDirector.Instance.IsPaused) yield break;

            SpawnEnemy(difficulty);
            yield return new WaitForSeconds(spawnStagger);
        }
    }

    private void SpawnEnemy(float difficulty)
    {
        if (playerTransform == null || enemyPrefabs.Count == 0) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = playerTransform.position + (Vector3)(randomDir * spawnDistance);

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
            Debug.Log($"[WaveManager] Enemy Defeated! Remaining: {enemiesAlive}");
            if (enemiesAlive <= 0)
            {
                Debug.Log("[WaveManager] Wave Completed!");
                GameDirector.Instance.NotifyWaveFinished();
            }
        }
    }

    public void TookDamageThisWave()
    {
        tookDamageThisWave = true;
        
    }
}