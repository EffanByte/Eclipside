using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawnManager : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private EnemyBase normalEnemyPrefab;
    [SerializeField] private EnemyBase spiderEnemyPrefab;

    [Header("Spawn Bounds")]
    [SerializeField] private float minX = -2.25f;
    [SerializeField] private float maxX =  2.5f;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float maxY =  3f;

    [Header("Wave Settings")]
    [SerializeField] private int startEnemies = 1;
    [SerializeField] private int enemiesPerWaveIncrease = 1;
    [SerializeField] private float timeBetweenSpawns = 0.1f;

    [Header("Spider Settings")]
    [SerializeField] private int spiderStartWave = 3;
    [SerializeField] private int spidersPerWave = 1;

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

        int totalEnemies = startEnemies + (currentWave - 1) * enemiesPerWaveIncrease;
        int spiderCount = currentWave >= spiderStartWave
            ? Mathf.Min(spidersPerWave * (currentWave - spiderStartWave + 1), totalEnemies)
            : 0;

        int normalCount = totalEnemies - spiderCount;

        StartCoroutine(SpawnWaveCoroutine(normalCount, spiderCount));
    }

    private IEnumerator SpawnWaveCoroutine(int normalCount, int spiderCount)
    {
        spawningWave = true;

        for (int i = 0; i < normalCount; i++)
        {
            SpawnOne(normalEnemyPrefab);
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        for (int i = 0; i < spiderCount; i++)
        {
            SpawnOne(spiderEnemyPrefab);
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        spawningWave = false;
    }

    private void SpawnOne(EnemyBase prefab)
    {
        Vector2 pos = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );

        EnemyBase enemy = Instantiate(prefab, pos, Quaternion.identity);
        aliveEnemies.Add(enemy);
    }

    private void HandleEnemyKilled(EnemyBase enemy)
    {
        aliveEnemies.Remove(enemy);
        aliveEnemies.RemoveAll(e => e == null);

        if (!spawningWave && aliveEnemies.Count == 0)
        {
            StartNextWave();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size   = new Vector3((maxX - minX), (maxY - minY), 0.01f);
        Gizmos.DrawWireCube(center, size);
    }
}
