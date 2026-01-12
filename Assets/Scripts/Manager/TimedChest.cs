using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))] // Ensure it has a trigger
public class TimedChest : MonoBehaviour, IInteractable
{
    [Header("Loot Configuration")]
    [SerializeField] private List<WeaponData> weaponPool;
    [SerializeField] private List<ItemData> consumablePool;
    [SerializeField] private ItemData keyItem; // Reference to your Key "CurrencyItem"

    [Header("Reward Spawning")]
    [SerializeField] private GameObject lootPedestalPrefab; // The prefab with LootPedestal script


    [Header("Settings")]
    [SerializeField] private float disappearTime = 20f;
    [SerializeField] private Sprite openVisual; // Sprite/Object to show when opened
    [SerializeField] private Sprite closedVisual;

    private bool isOpened = false;

    private void Start()
    {
        // Start the countdown immediately upon spawning
        StartCoroutine(DespawnRoutine());
    }

    public void Interact(PlayerController player)
    {
        if (isOpened) return;

        // Check if player has a key (if you want these chests to require keys)
        // For "Wave Reward" chests, they are usually free. 
        // If you want a cost: if (player.keys <= 0) return false; player.keys--;

        OpenChest(player);
        return;
    }

    public string GetInteractionPrompt()
    {
        return isOpened ? "" : "Open Chest";
    }

     private void OpenChest(PlayerController player)
    {
        isOpened = true;
        
        // 1. Determine Loot (Same Logic as before)
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
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            
            GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
            
            LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
            if (pedestal != null)
            {
                pedestal.Setup(reward);
            }
        }

        // 3. Cleanup Chest
        if (openVisual) gameObject.GetComponent<SpriteRenderer>().sprite = openVisual;
        if (closedVisual) gameObject.GetComponent<SpriteRenderer>().sprite = closedVisual;
        
        StopAllCoroutines(); // Stop disappearance timer
        Destroy(gameObject, 0.5f); // Fade out chest quickly
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
}