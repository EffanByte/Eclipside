using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))] // Ensure it has a trigger
public class TimedChest : MonoBehaviour, IInteractable
{
    [Header("Loot Configuration")]
    [SerializeField] private List<WeaponData> weaponPool;
    [SerializeField] private List<ItemData> consumablePool; // nneed to chhange this to retrieve for consumable DB automatically
    [SerializeField] private ItemData keyItem; // Reference to your Key "CurrencyItem"

    [Header("Reward Spawning")]
    [SerializeField] private GameObject lootPedestalPrefab; // The prefab with LootPedestal script
    private int keyCount = 1;
    private static int globalKeyCount;


    [Header("Settings")]
    [SerializeField] private float disappearTime = 20f;
    [SerializeField] private Sprite openVisual; // Sprite/Object to show when opened
    [SerializeField] private Sprite closedVisual;

    private bool isOpened = false;

    public void Setup(int keyCount)
    {
        SetKeyCount(keyCount);
    }

    
    private void Start()
    {
        // Start the countdown immediately upon spawning
        StartCoroutine(DespawnRoutine());
        if (keyCount == 0)
        {
            keyCount = globalKeyCount;
        }
    }

    public void Interact(PlayerController player)
    {
        if (isOpened) return;
        Debug.Log(keyCount);
        if (PlayerController.Instance.DeductCurrency(CurrencyType.Key, keyCount))
        {
            OpenChest();
        }
    }

    public string GetInteractionPrompt()
    {
        return isOpened ? "" : "Open Chest";
    }

    public void OpenChest()
    {
        isOpened = true;
        
        float roll = Random.value;
        ItemData reward = null;

        if (roll < 0.6f && weaponPool.Count > 0)
            reward = weaponPool[Random.Range(0, weaponPool.Count)];
        else if (roll < 0.9f && consumablePool.Count > 0)
            reward = consumablePool[Random.Range(0, consumablePool.Count)];
        else if (keyItem != null)
            reward = keyItem;
        // 2. Spawn Visual Loot
        if (reward != null && lootPedestalPrefab != null)
        {
            // Spawn slightly above chest
            Vector3 spawnPos = transform.position + Vector3.up * 0.2f;
            
            GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
            LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
            if (pedestal != null)
            {
                pedestal.Setup(reward);
                StatisticsManager.Instance.IncrementStat("CHESTS_OPENED");
            }
        }

        gameObject.GetComponent<SpriteRenderer>().sprite = openVisual;
        StatisticsManager.Instance.IncrementStat("CHESTS_OPENED");
        StopAllCoroutines(); // Stop disappearance timer
    }
    private IEnumerator DespawnRoutine()
    {
        // Optional: Flash sprite when time is running low (at 5 seconds left)
        yield return new WaitForSeconds(disappearTime);

        if (!isOpened)
        {
            Debug.Log("Chest disappeared!");
            // Optional: Poof particle effect
            Destroy(gameObject);
        }
    }

    private void SetKeyCount(int count)
    {
        keyCount = count;
        Debug.Log("Key count set to " + keyCount);
    }
    public static void SetGloalKeyCount(int count)
    {
        globalKeyCount = count;
    }
}