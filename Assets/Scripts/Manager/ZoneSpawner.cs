using UnityEngine;
using System.Collections.Generic;

public class ZoneSpawner : MonoBehaviour
{
    public static ZoneSpawner Instance { get; private set; }
    
    [SerializeField] private Transform zoneSpawnPointContainer;
    public List<GameObject> zonePrefabs;

    void Awake()
    {
        Instance = this;
    }

    public static void SpawnZones()
    {
        if (Instance == null || Instance.zoneSpawnPointContainer == null || Instance.zonePrefabs.Count == 0) 
            return;

        foreach (Transform spawnPoint in Instance.zoneSpawnPointContainer)
        {
            int randomIndex = Random.Range(0, Instance.zonePrefabs.Count);
            GameObject zonePrefab = Instance.zonePrefabs[randomIndex];
            
            // Instantiating as a child of the spawnPoint makes cleanup easier later
            Instantiate(zonePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        }
    }
    
    // Call this when changing biomes if you want to reroll the shops/altars
    public static void ClearZones()
    {
        if (Instance == null || Instance.zoneSpawnPointContainer == null) return;
        
        foreach (Transform spawnPoint in Instance.zoneSpawnPointContainer)
        {
            foreach(Transform child in spawnPoint)
            {
                Destroy(child.gameObject);
            }
        }
    }
}