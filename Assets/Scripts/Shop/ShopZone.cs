using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))] // The Trigger for the Safe Zone
public class ShopZone : MonoBehaviour
{
    [Header("Generation Configuration")]
    [SerializeField] private GameObject pedestalPrefab;
    [SerializeField] private GameObject refreshStatuePrefab;
    
    [Header("Spawn Locations")]
    [Tooltip("Drag 5 Empty GameObjects here representing where items spawn")]
    [SerializeField] private List<Transform> pedestalSpawnPoints; 
    [SerializeField] private Transform statueSpawnPoint;

    [Header("Barriers")]
    [Tooltip("The physical wall collider. Ensure this is NOT the same object as this script.")]
    
    [SerializeField] private GameObject visualBarrier;

    private Collider2D physicalBarrier; 

    // --- Internal State ---
    private List<ShopPedestal> spawnedPedestals = new List<ShopPedestal>();
    private ShopRefreshStatue refreshStatue;

    private void Start()
    {
        // 1. SPAWN THE PHYSICAL OBJECTS
        SpawnShopArchitecture();

        // 2. GENERATE DATA (If needed)
        // If this is the first time entering a shop, roll the dice.
        if (ShopManager.Instance.currentStock == null || ShopManager.Instance.currentStock.Length == 0)
        {
            ShopManager.Instance.GenerateShop();
        }

        // 3. UPDATE VISUALS (Apply Data to New Objects)
        UpdateVisuals();

        // 4. LISTEN FOR EVENTS
        ShopManager.Instance.OnShopUpdated += UpdateVisuals;

        // 5. SETUP BARRIERS
        if (GameDirector.Instance != null)
        {
            GameDirector.Instance.OnCombatStateChanged += ToggleBarrier;
            ToggleBarrier(GameDirector.Instance.IsWaveActive);
        }
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance != null) ShopManager.Instance.OnShopUpdated -= UpdateVisuals;
        if (GameDirector.Instance != null) GameDirector.Instance.OnCombatStateChanged -= ToggleBarrier;
    }

    // ---------------------------------------------------------
    // INSTANTIATION LOGIC (The Fix)
    // ---------------------------------------------------------
    private void SpawnShopArchitecture()
    {
        // A. Spawn Statue
        if (refreshStatuePrefab != null && statueSpawnPoint != null)
        {
            GameObject obj = Instantiate(refreshStatuePrefab, statueSpawnPoint.position, Quaternion.identity, transform);
        }

        // B. Spawn Pedestals
        spawnedPedestals.Clear(); // Safety clear
        if (pedestalPrefab != null && pedestalSpawnPoints.Count > 0)
        {
            foreach (Transform spawnPoint in pedestalSpawnPoints)
            {
                GameObject obj = Instantiate(pedestalPrefab, spawnPoint.position, Quaternion.identity, transform);
                ShopPedestal pedestalScript = obj.GetComponent<ShopPedestal>();
                if (pedestalScript != null)
                {
                    spawnedPedestals.Add(pedestalScript);
                }
            }
        }
    }

    // ---------------------------------------------------------
    // VISUAL LOGIC
    // ---------------------------------------------------------
    private void UpdateVisuals()
    {
        ItemData[] stock = ShopManager.Instance.currentStock;
        bool[] soldOut = ShopManager.Instance.isSoldOut;

        // Loop through our NEWLY SPAWNED pedestals list
        for (int i = 0; i < spawnedPedestals.Count; i++)
        {
            if (i < stock.Length)
            {
                spawnedPedestals[i].Setup(stock[i], i);
            }
        }

        if (refreshStatue != null)
        {
            refreshStatue.UpdateCost();
        }
    }

    // ---------------------------------------------------------
    // BARRIER LOGIC
    // ---------------------------------------------------------
    private void ToggleBarrier(bool isCombatActive)
    {
        if (physicalBarrier != null) physicalBarrier.enabled = isCombatActive;
        if (visualBarrier != null) visualBarrier.SetActive(isCombatActive);
    }

    // ---------------------------------------------------------
    // SAFE ZONE LOGIC
    // ---------------------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Entered Shop: Waves Paused.");
            GameDirector.Instance.SetSafeZoneState(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Left Shop: Waves Resumed.");
            GameDirector.Instance.SetSafeZoneState(false);
        }
    }
}