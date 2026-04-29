using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for ToList()
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private List<AchievementData> allAchievements;

    // Track which ones are already claimed
    private HashSet<string> unlockedAchievements = new HashSet<string>();
    private const string FILE_NAME = "Save_Stats";

    public IReadOnlyList<AchievementData> GetAllAchievements()
    {
        return allAchievements;
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject); // add if statement later, idr exact condition
        if (Instance == null) Instance = this;
        
        // Listen to the Stats Manager
        StatisticsManager.Instance.OnStatChanged += CheckProgress;
        
        LoadProgress();
    }

    private void CheckProgress(string key, int currentValue, int delta)
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
    #if UNITY_EDITOR
    [ContextMenu("Load Achievements From Folder")]
    private void LoadFromFolder()
    {
        allAchievements.Clear();
        
        // Searches the project for AchievementData assets
        string[] guids = AssetDatabase.FindAssets("t:AchievementData", new[] { "Assets/Objects/Achievements" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AchievementData asset = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
            if (asset != null)
            {
                allAchievements.Add(asset);
            }
        }
        
        Debug.Log($"Loaded {allAchievements.Count} achievements from Assets/Objects/Achievements");
        
        // Mark object as dirty to save the list
        EditorUtility.SetDirty(this);
    }
#endif
}
