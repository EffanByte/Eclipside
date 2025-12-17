using System.Collections.Generic;
using UnityEngine;

public class WaveSpawnManager : MonoBehaviour
{
    [Header("Enemy Prefab")]
    [SerializeField] private EnemyBase enemyPrefab;

    [Header("Spawn Bounds")]
    [SerializeField] private float minX = -2.25f;
    [SerializeField] private float maxX =  2.5f;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float maxY =  3f;

    [Header("Wave Settings")]
    [SerializeField] private int startEnemies = 1;
    [SerializeField] private int enemiesPerWaveIncrease = 1;
    [SerializeField] private float timeBetweenSpawns = 0.1f;

    private readonly List<EnemyBase> aliveEnemies = new();
    private int currentWave = 0;
    private bool spawningWave = false;

    private void OnEnable()
    {
        EnemyBase.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void Start()
    {
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (spawningWave) return;

        currentWave++;
        int toSpawn = startEnemies + (currentWave - 1) * enemiesPerWaveIncrease;
        StartCoroutine(SpawnWaveCoroutine(toSpawn));
    }

    private System.Collections.IEnumerator SpawnWaveCoroutine(int count)
    {
        spawningWave = true;

        for (int i = 0; i < count; i++)
        {
            SpawnOne();
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        spawningWave = false;
    }

    private void SpawnOne()
    {
        Vector2 pos = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );

        EnemyBase enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        aliveEnemies.Add(enemy);
    }

    private void HandleEnemyKilled(EnemyBase enemy)
    {
        // Remove dead one
        aliveEnemies.Remove(enemy);

        // Clean up any nulls (in case something got destroyed without calling Kill)
        aliveEnemies.RemoveAll(e => e == null);

        // If all are dead, start next wave
        if (!spawningWave && aliveEnemies.Count == 0)
        {
            StartNextWave();
        }
    }

    // Optional: visualize spawn area in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size   = new Vector3((maxX - minX), (maxY - minY), 0.01f);
        Gizmos.DrawWireCube(center, size);
    }
}
