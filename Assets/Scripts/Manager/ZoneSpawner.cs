using UnityEngine;
using System.Collections.Generic;

public class ZoneSpawner : MonoBehaviour
{
    public static ZoneSpawner Instance { get; private set; }
    [SerializeField] private Transform zoneSpawnPointContainer;
    public List<GameObject> zonePrefabs;


    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        Instance = this;
    }
    public static void SpawnZones()
    {
        foreach (Transform spawnPoint in Instance.zoneSpawnPointContainer)
        {
            int randomIndex = Random.Range(0, Instance.zonePrefabs.Count);
            GameObject zonePrefab = Instance.zonePrefabs[randomIndex];
            Instantiate(zonePrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

}
