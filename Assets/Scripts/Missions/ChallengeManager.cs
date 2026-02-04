using UnityEngine;
using System.Collections;
using System.Collections.Generic;


    public enum ChallengeType
    {
        FragileCrystal,
        ThePurge,
        EndlessGreed,
        TheGladiator,
        BloodForPower,
        Crossfire,
        TotalConfusion,
        RainOfFire, // Mentioned in your JSON but missing in code
        TheUnlucky,
        LastBreath
    }

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance { get; private set; }

    // Stores selected challenges
    public HashSet<ChallengeType> activeChallenges = new HashSet<ChallengeType>();

    // Global Flags for specific logic checks
    public static bool theGladiator = false;

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
        theGladiator = false;

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
                FragileCrystal();
                break;

            case ChallengeType.ThePurge:
                ThePurge();
                break;

            case ChallengeType.EndlessGreed:
                EndlessGreed();
                break;

            case ChallengeType.TheGladiator:
                TheGladiator();
                break;

            case ChallengeType.BloodForPower:
                BloodForPower();
                break;

            case ChallengeType.TotalConfusion:
                TotalConfusion();
                break;

            case ChallengeType.TheUnlucky:
                TheUnlucky();
                break;

            case ChallengeType.LastBreath:
                LastBreath();
                break;

            case ChallengeType.Crossfire:
                Crossfire();
                break;
        }
    }

    public static  void FragileCrystal()
    {
        Debug.Log(PlayerController.Instance.GetMaxHealth());
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, PlayerController.Instance.GetMaxHealth()/2);
        Debug.Log(PlayerController.Instance.GetMaxHealth());    
    }

    public static  void ThePurge()
    {
        Debug.Log("Removing Shop Zones for The Purge Challenge");
        ZoneSpawner.Instance.zonePrefabs.RemoveAt(0);
    }

    public static void EndlessGreed()
    {
        TimedChest.SetGloalKeyCount(0);
        GameDirector.Instance.SetMaxWaveCount(GameDirector.Instance.GetMaxWaveCount() + 1);
    }
    public void TheGladiator()
    {
        theGladiator = true;
    }

    public void BloodForPower()
    {
        PlayerController.Instance.OnLevelUp += BFPModify;
    }

    private void BFPModify()
    {
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, -1);
    }

    public void TotalConfusion()
    {
        StartCoroutine(ConfusionRoutine());
    }

    private IEnumerator ConfusionRoutine()
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.TryAddStatus(StatusType.Confusion);
        yield return new WaitForSeconds(Random.Range(20,60));
    }

    public void TheUnlucky()
    {
        PlayerController.Instance.LockLuck();   
    }
    public void LastBreath()
    {
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, 10);
    }
    public void Crossfire()
    {
        // complete later when do projectile logic for player
    }
}