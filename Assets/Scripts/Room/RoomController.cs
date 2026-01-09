using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public enum RoomType
{
    Combat,     // Locks doors, spawns enemies
    Shop,       // Safe
    Chest,      // Safe
    Boss,       // Locks doors, spawns boss
    Start       // Safe
}

public enum RoomState
{
    Inactive,
    Active,     // Player inside, Doors Locked (if combat)
    Cleared     // Finished
}
[RequireComponent(typeof(BoxCollider2D))] // Trigger to detect player entry
public class RoomController : MonoBehaviour
{
    [Header("Configuration")]
    public RoomType type;
    
    [Header("References")]
    [SerializeField] private List<RoomDoor> doors;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private GameObject rewardChestPrefab; 

    [Header("Combat Settings")]
    // If list is empty, room is considered "Safe"
    [SerializeField] private List<WaveDefinition> waves; 

    // --- State ---
    private RoomState state = RoomState.Inactive;
    private int currentWaveIndex = 0;
    private int enemiesAlive = 0;

    private void Start()
    {
        // Start with doors open so player can enter
        UnlockDoors();
    }

// ... inside RoomController class ...

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 2. Initialize Logic (Combat/Shop/etc)
            if (state == RoomState.Inactive)
            {
                InitializeRoom();
            }
        }
    }

    private void InitializeRoom()
    {
        // STEP 1: Check Logic based on Room Type / Wave Count
        bool isCombatRoom = (waves.Count > 0);

        if (isCombatRoom)
        {
            // Combat Logic: Lock player in, start fighting
            state = RoomState.Active;
            LockDoors();
            StartCoroutine(CombatRoutine());
        }
        else
        {
            // Safe Logic: Keep doors open, mark as cleared immediately
            state = RoomState.Cleared;
            Debug.Log($"Entered Safe Room: {type}");
        }
    }

    // --- COMBAT LOOP ---
    private IEnumerator CombatRoutine()
    {
        EnemyBase.OnEnemyKilled += HandleEnemyDeath;

        // Iterate through all waves
        while (currentWaveIndex < waves.Count)
        {
            SpawnWave(waves[currentWaveIndex]);

            // Wait until wave is dead
            yield return new WaitUntil(() => enemiesAlive <= 0);

            currentWaveIndex++;
            yield return new WaitForSeconds(1f); // Brief pause betweeen waves
        }

        EnemyBase.OnEnemyKilled -= HandleEnemyDeath;
        RoomFinished();
    }

    private void SpawnWave(WaveDefinition wave)
    {
        Debug.Log($"Spawning {wave.waveName}");
        
        foreach (var group in wave.groups)
        {
            for (int i = 0; i < group.count; i++)
            {
                Transform spot = spawnPoints[Random.Range(0, spawnPoints.Count)];
                Instantiate(group.enemyPrefab, spot.position, Quaternion.identity);
                enemiesAlive++;
            }
        }
    }

    private void HandleEnemyDeath(EnemyBase enemy)
    {
        if (state == RoomState.Active)
        {
            enemiesAlive--;
        }
    }

    private void RoomFinished()
    {
        Debug.Log("Room Cleared!");
        state = RoomState.Cleared;
        UnlockDoors();

        if (rewardChestPrefab != null)
        {
            Instantiate(rewardChestPrefab, transform.position, Quaternion.identity);
        }
    }

    // --- HELPERS ---
    private void LockDoors()
    {
        foreach (var door in doors) door.SetLocked(true);
    }

    private void UnlockDoors()
    {
        foreach (var door in doors) door.SetLocked(false);
    }
}