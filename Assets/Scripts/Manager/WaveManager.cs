using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private List<GameObject> enemyPrefabs; // The pool of enemies
    [SerializeField] private float spawnDistance = 12f;     // Radius around player
    [SerializeField] private float spawnStagger = 0.5f;     // Time between individual spawns

    private int enemiesAlive = 0;
    private Transform playerTransform;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }


    public void TriggerWave(int waveNumber, float difficultyMultiplier)
    {
        
        // Logic: Wave 1 = 3 enemies. Wave 10 = 30 enemies? 
        // Or Wave 1 = 3 enemies. Wave 2 = 4 enemies (based on 1.1x multiplier)
        GameDirector.Instance.NotifyWaveFinished();
        int enemyCount = Mathf.CeilToInt(3 * difficultyMultiplier);
        
        Debug.Log($"<color=red>SPAWNING WAVE {waveNumber}</color> (Count: {enemyCount}, Diff: {difficultyMultiplier:F1}x)");

        StartCoroutine(SpawnRoutine(enemyCount, difficultyMultiplier));
    }

    private IEnumerator SpawnRoutine(int count, float difficulty)
    {
        for (int i = 0; i < count; i++)
        {
            // Stop spawning if GameDirector says we are paused (Safety check)
            if (GameDirector.Instance.IsPaused) yield break;

            SpawnEnemy(difficulty);
            yield return new WaitForSeconds(spawnStagger);
        }
    }

    private void SpawnEnemy(float difficulty)
    {
        if (playerTransform == null || enemyPrefabs.Count == 0) return;

        // 1. Calculate Position (Random point on circle edge)
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = playerTransform.position + (Vector3)(randomDir * spawnDistance);

        // 2. Select Enemy
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        // 3. Instantiate
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 4. Apply Difficulty Stats
        EnemyBase enemyScript = enemyObj.GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
            // You need to add this method to EnemyBase to scale HP/Damage
            // enemyScript.ApplyDifficultyScaling(difficulty);
        }
    }
}