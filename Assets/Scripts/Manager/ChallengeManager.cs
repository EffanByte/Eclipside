using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance { get; private set; }

    // Stores selected challenges
    public HashSet<ChallengeType> activeChallenges = new HashSet<ChallengeType>();

    // Global Flags for specific logic checks
    public bool IsGladiatorActive { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object when loading Game Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- UI INTERACTION ---

    public void ToggleChallenge(ChallengeType type, bool isOn)
    {
        if (isOn) activeChallenges.Add(type);
        else activeChallenges.Remove(type);
    }

    public bool IsChallengeActive(ChallengeType type)
    {
        return activeChallenges.Contains(type);
    }

    // --- EXECUTION (Called by GameDirector at Start of Run) ---

    public void ApplyActiveChallenges()
    {
        // Reset Flags
        IsGladiatorActive = false;

        Debug.Log($"Applying {activeChallenges.Count} Challenges...");

        foreach (var challenge in activeChallenges)
        {
            ApplyLogic(challenge);
        }
    }

    private void ApplyLogic(ChallengeType type)
    {
        switch (type)
        {
            case ChallengeType.FragileCrystal:
                // HP / 2
                float currentMax = PlayerController.Instance.GetMaxHealth();
                PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, -(currentMax / 2));
                break;

            case ChallengeType.ThePurge:
                // Remove Shop from generation list
                // Note: We use RemoveAll with a predicate to find the shop prefab by name or component
                if (GameDirector.Instance != null)
                {
                    GameDirector.Instance.zonePrefabs.RemoveAll(x => x.name.Contains("Shop") || x.GetComponent<ShopZone>() != null);
                }
                break;

            case ChallengeType.EndlessGreed:
                // Add wave count
                // Assuming GameDirector has a SetMaxWaves method, or modify variable directly
                // GameDirector.Instance.IncreaseMaxWaves(1);
                // TimedChest.GlobalKeyCost = 0; // Custom logic
                break;

            case ChallengeType.TheGladiator:
                IsGladiatorActive = true; 
                // Logic needs to be checked in WaveManager (e.g. "If Gladiator, spawn harder enemies")
                break;

            case ChallengeType.BloodForPower:
                PlayerController.Instance.OnLevelUp += BloodForPower_Callback;
                break;

            case ChallengeType.TotalConfusion:
                StartCoroutine(TotalConfusionRoutine());
                break;

            case ChallengeType.TheUnlucky:
                // Assuming you made the LockLuck function
                PlayerController.Instance.ToggleLuck(false); // Force false
                // PlayerController.Instance.LockLuck(); // Force Lock
                break;

            case ChallengeType.LastBreath:
                // Set HP to very low (e.g., 10%) but high max? Or just low max?
                // Your code said "ModifyMaxHealth(10)", assuming that sets it to 10.
                PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, 10f); // Example logic
                break;

            case ChallengeType.Crossfire:
                // Logic implemented in PlayerController projectile code checking ChallengeManager.Instance.IsChallengeActive(Crossfire)
                break;
        }
    }

    // --- LOGIC HELPERS ---

    private void BloodForPower_Callback()
    {
        // -1 Max HP on Level Up
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, -1);
        Debug.Log("Blood For Power: Lost 1 Max HP on Level Up.");
    }

    private IEnumerator TotalConfusionRoutine()
    {
        // Loop forever during the run
        while (PlayerController.Instance != null)
        {
            float waitTime = Random.Range(20f, 60f);
            yield return new WaitForSeconds(waitTime);

            // Apply Confusion via StatusManager
            // (Assumes PlayerController has access to StatusManager)
            PlayerController.Instance.GetComponent<StatusManager>().TryAddStatus(StatusType.Confusion);
            Debug.Log("Total Confusion Challenge: Triggered Confusion!");
        }
    }
}