using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for ToList()


public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private List<AchievementData> allAchievements;

    // Track which ones are already claimed
    private HashSet<string> unlockedAchievements = new HashSet<string>();
    private const string FILE_NAME = "Save_Stats";

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
        // Ensure Player exists (in case unlocked in menu)
        if (PlayerController.Instance == null) return;

        switch (ach.rewardType)
        {
            case RewardType.Gold:
                PlayerController.Instance.AddCurrency(CurrencyType.Rupee, ach.rewardAmount); // Note: Assuming Gold = Rupee logic or add Gold type
                break;
                
            case RewardType.Orb:
                 PlayerController.Instance.AddCurrency(CurrencyType.Orb, ach.rewardAmount); 
                break;

            case RewardType.Consumable:
                if (ach.rewardItem != null && ach.rewardItem is ConsumableItem con)
                {
                    InventoryManager.Instance.AddItem(con); // think about this because it'd be one time use but idk when to add
                }
                break;
        }
    }

    // ---------------------------------------------------------
    // SAVE / LOAD LOGIC
    // ---------------------------------------------------------

    private void SaveProgress()
    {
        // 1. Load existing file (to avoid overwriting other stats)
        SaveFile_Stats data = SaveManager.Load<SaveFile_Stats>(FILE_NAME);

        // 2. Update the list
        data.achievements.completed_achievement_ids = unlockedAchievements.ToList();

        // 3. Write back to disk
        SaveManager.Save(FILE_NAME, data);
    }

    private void LoadProgress()
    {
        // 1. Load file
        SaveFile_Stats data = SaveManager.Load<SaveFile_Stats>(FILE_NAME);

        // 2. Populate runtime HashSet
        unlockedAchievements.Clear();
        if (data.achievements != null && data.achievements.completed_achievement_ids != null)
        {
            foreach (string id in data.achievements.completed_achievement_ids)
            {
                unlockedAchievements.Add(id);
            }
        }
    }
}