using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        List<Transform> spawnPoints = new List<Transform>();
        foreach (Transform spawnPoint in Instance.zoneSpawnPointContainer)
        {
            if (spawnPoint != null)
            {
                spawnPoints.Add(spawnPoint);
            }
        }

        if (spawnPoints.Count == 0)
        {
            return;
        }

        Vector3 playerPosition = PlayerController.Instance != null
            ? PlayerController.Instance.transform.position
            : Instance.zoneSpawnPointContainer.position;

        spawnPoints.Sort((a, b) =>
            Vector3.SqrMagnitude(a.position - playerPosition).CompareTo(
            Vector3.SqrMagnitude(b.position - playerPosition)));

        List<GameObject> shopPrefabs = Instance.zonePrefabs.Where(Instance.IsShopZonePrefab).ToList();
        List<GameObject> nonShopPrefabs = Instance.zonePrefabs.Where(prefab => prefab != null && !Instance.IsShopZonePrefab(prefab)).ToList();
        int guaranteedShopSpawnIndex = shopPrefabs.Count > 0
            ? Random.Range(0, Mathf.Min(3, spawnPoints.Count))
            : -1;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            GameObject zonePrefab = null;

            if (i == guaranteedShopSpawnIndex)
            {
                zonePrefab = shopPrefabs[Random.Range(0, shopPrefabs.Count)];
            }
            else if (nonShopPrefabs.Count > 0)
            {
                zonePrefab = nonShopPrefabs[Random.Range(0, nonShopPrefabs.Count)];
            }
            else if (Instance.zonePrefabs.Count > 0)
            {
                zonePrefab = Instance.zonePrefabs[Random.Range(0, Instance.zonePrefabs.Count)];
            }

            if (zonePrefab == null)
            {
                continue;
            }

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

    private bool IsShopZonePrefab(GameObject prefab)
    {
        return prefab != null && (prefab.GetComponent<ShopZone>() != null || prefab.CompareTag("Shop"));
    }
}
