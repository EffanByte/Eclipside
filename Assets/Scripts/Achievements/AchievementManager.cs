using UnityEngine;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private List<AchievementData> allAchievements;

    // Track which ones are already claimed
    private HashSet<string> unlockedAchievements = new HashSet<string>();

    private void Start()
    {
        if (Instance == null) Instance = this;
        
        // Listen to the Stats Manager
        StatisticsManager.Instance.OnStatUpdated += CheckProgress;
        
        LoadProgress();
    }

    private void CheckProgress(string key, int currentValue)
    {
        foreach (var ach in allAchievements)
        {
            // 1. Skip if already unlocked
            if (unlockedAchievements.Contains(ach.id)) continue;

            // 2. Check if this achievement cares about this stat
            if (ach.statKey == key)
            {
                // 3. Check condition
                if (currentValue >= ach.targetValue)
                {
                    UnlockAchievement(ach);
                }
            }
        }
    }

    private void UnlockAchievement(AchievementData ach)
    {
        unlockedAchievements.Add(ach.id);
        
        // UI Notification
        Debug.Log($"<color=yellow>ACHIEVEMENT UNLOCKED: {ach.title}</color>");
        
        // Grant Reward (Apply Difficulty Multiplier here if needed)
        GrantReward(ach);
        
        SaveProgress();
    }

    private void GrantReward(AchievementData ach)
    {
        float difficultyMult = GameDirector.Instance.CurrentDifficulty; 
        // Logic to give Gold/Orbs/Items based on ach.rewardType
        // e.g. PlayerController.Instance.AddCurrency(...)
    }

    
    private void SaveProgress() { /* Save unlocked list */ }
    private void LoadProgress() { /* Load unlocked list */ }
}