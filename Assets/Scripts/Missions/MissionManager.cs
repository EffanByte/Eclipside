using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum MissionDifficulty { Easy, Medium, Hard, Weekly }
public enum MissionRewardType { Gold, Orbs, Ticket }

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Mission Pools")]
    [SerializeField] private List<MissionData> easyPool;
    [SerializeField] private List<MissionData> mediumPool;
    [SerializeField] private List<MissionData> hardPool;
    [SerializeField] private List<MissionData> weeklyPool;

    [Header("Bonus Config")]
    [SerializeField] private int dailyBonusOrbs = 15;
    [SerializeField] private int weeklyBonusOrbs = 75;

    // Runtime Lookup for Data
    private Dictionary<string, MissionData> missionDatabase = new Dictionary<string, MissionData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Build Database for fast lookup
        AddToDatabase(easyPool);
        AddToDatabase(mediumPool);
        AddToDatabase(hardPool);
        AddToDatabase(weeklyPool);
    }

    private void Start()
    {
        CheckResets();
        StatisticsManager.Instance.OnStatChanged += OnStatUpdated;
    }

    private void OnDestroy()
    {
        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.OnStatChanged -= OnStatUpdated;
    }

    // ---------------------------------------------------------
    // 1. RESET LOGIC (UTC TIME)
    // ---------------------------------------------------------
    private void CheckResets()
    {
        var tracker = SaveManager.Profile.daily_tracker;
        DateTime now = DateTime.UtcNow;
        string todayStr = now.ToString("yyyy-MM-dd");

        // --- DAILY RESET ---
        Debug.Log("Trying daily reset");
        // commenting out if to test normally
        //if (tracker.last_daily_reset_date != todayStr)
        {
            Debug.Log("Performing Daily Mission Reset...");
            GenerateDailyMissions(tracker);
            tracker.last_daily_reset_date = todayStr;
            tracker.daily_reroll_used = false;
            tracker.daily_bonus_claimed = false;
            SaveManager.SaveProfile();
        }
        foreach (ActiveMissionEntry entry in tracker.active_daily_missions)
        {
            Debug.Log($"Daily Mission: {entry.description}, Progress: {entry.current_progress}/{entry.target_value}, Completed: {entry.is_completed}, Claimed: {entry.is_claimed}");
        }
        // --- WEEKLY RESET (Monday) ---
        // Calculate the date of the most recent Monday
        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime lastMonday = now.AddDays(-1 * diff).Date;
        string weekStr = lastMonday.ToString("yyyy-MM-dd");

        if (tracker.last_weekly_reset_date != weekStr)
        {
            Debug.Log("Performing Weekly Mission Reset...");
            GenerateWeeklyMissions(tracker);
            tracker.last_weekly_reset_date = weekStr;
            tracker.weekly_bonus_claimed = false;
            SaveManager.SaveProfile();
        }
    }

    // ---------------------------------------------------------
    // 2. GENERATION
    // ---------------------------------------------------------
    private void GenerateDailyMissions(DailyTracker tracker)
    {
        tracker.active_daily_missions.Clear();

        // Generate 1 Easy, 1 Medium, 1 Hard
        AddMissionToTracker(tracker.active_daily_missions, GetRandomMission(easyPool));
        AddMissionToTracker(tracker.active_daily_missions, GetRandomMission(mediumPool));
        AddMissionToTracker(tracker.active_daily_missions, GetRandomMission(hardPool));
    }

    private void GenerateWeeklyMissions(DailyTracker tracker)
    {
        tracker.active_weekly_missions.Clear();
        
        // Generate 1 Weekly (or 5 if you want a full list)
        // Ensure unique picks if picking multiple
        AddMissionToTracker(tracker.active_weekly_missions, GetRandomMission(weeklyPool));
    }

    private MissionData GetRandomMission(List<MissionData> pool)
    {
        if (pool == null || pool.Count == 0) return null;
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    private void AddMissionToTracker(List<ActiveMissionEntry> list, MissionData data)
    {
        if (data == null) return;
        
        // Check if user already has partial progress in Stats (Optional, usually start at 0)
        // Ideally, missions track *delta* progress since generation.
        // For simplicity, we reset the progress to 0.
        
        ActiveMissionEntry entry = new ActiveMissionEntry
        {
            mission_id = data.id,
            description = data.description,
            target_value = data.targetValue,
            current_progress = 0,
            is_completed = false,
            is_claimed = false
        };
        list.Add(entry);
    }

    // ---------------------------------------------------------
    // 3. PROGRESS TRACKING
    // ---------------------------------------------------------
    // ---------------------------------------------------------
    // 3. PROGRESS TRACKING (Fixed)
    // ---------------------------------------------------------
    private void OnStatUpdated(string statKey, int totalValue, int amountAdded)
    {
        // 1. Get the lists
        var tracker = SaveManager.Profile.daily_tracker;
        List<ActiveMissionEntry> activeDaily = tracker.active_daily_missions;
        List<ActiveMissionEntry> activeWeekly = tracker.active_weekly_missions;

        // 2. Update Standard Increments (e.g. Kill 10 Enemies)
        // We pass the amountAdded (Delta) because Missions track progress per session
        UpdateMissionList(activeDaily, statKey, amountAdded);
        UpdateMissionList(activeWeekly, statKey, amountAdded);

        // 3. Handle Special Conditional Logic ("Without Dying")
        // Case A: "Complete Run Without Dying"
        if (statKey == "RUNS_COMPLETED")
        {
            if (!StatisticsManager.Instance.HasDiedThisRun())
            {
                // Trigger the special mission ID (e.g. DAILY_H_NODEATH) linked to "RUN_NO_DEATH" key
                UpdateMissionList(activeDaily, "RUN_NO_DEATH", 1);
            }
        }

        // Case B: "Defeat Miniboss Without Dying"
        if (statKey == "KILLS_MINIBOSS")
        {
            if (!StatisticsManager.Instance.HasDiedThisRun())
            {
                UpdateMissionList(activeDaily, "MINIBOSS_NO_DEATH", 1);
            }
        }
    }

    private void UpdateMissionList(List<ActiveMissionEntry> missions, string key, int amountToAdd)
    {
        bool changed = false;
        foreach (var entry in missions)
        {
            if (entry.is_completed) continue;

            if (missionDatabase.TryGetValue(entry.mission_id, out MissionData data))
            {
                if (data.statKey == key)
                {
                    entry.current_progress += amountToAdd;
                    if (entry.current_progress >= entry.target_value)
                    {
                        entry.current_progress = entry.target_value;
                        entry.is_completed = true;
                        // Show Notification UI: "Mission Complete!"
                    }
                    changed = true;
                }
            }
        }
        
        if(changed) SaveManager.SaveProfile();
    }

    // ---------------------------------------------------------
    // 4. CLAIMING & REROLL
    // ---------------------------------------------------------
    
    // Call via UI
    public void ClaimMission(ActiveMissionEntry entry)
    {
        if (!entry.is_completed || entry.is_claimed) return;

        // Anti-Tamper Check (Placeholder)
        if (!IsServerTimeSynced()) 
        {
            Debug.LogError("Cannot claim: Time not synced!");
            return;
        }

        if (missionDatabase.TryGetValue(entry.mission_id, out MissionData data))
        {
            entry.is_claimed = true;
            
            // Give Reward
            if (data.rewardType == MissionRewardType.Gold) 
                CurrencyManager.AddCurrency(CurrencyType.Gold, data.rewardAmount);
            else if (data.rewardType == MissionRewardType.Orbs) 
                CurrencyManager.AddCurrency(CurrencyType.Orb, data.rewardAmount);
            // Handle Ticket logic here
            
            CheckDailyBonus();
            SaveManager.SaveProfile();
        }
    }

    public void RerollDailyMission(ActiveMissionEntry entryToSwap)
    {
        var tracker = SaveManager.Profile.daily_tracker;
        if (tracker.daily_reroll_used) return;

        // Find the difficulty of the mission we are swapping
        if (missionDatabase.TryGetValue(entryToSwap.mission_id, out MissionData oldData))
        {
            List<MissionData> pool = null;
            switch(oldData.difficulty)
            {
                case MissionDifficulty.Easy: pool = easyPool; break;
                case MissionDifficulty.Medium: pool = mediumPool; break;
                case MissionDifficulty.Hard: pool = hardPool; break;
            }

            // Pick new one (try to ensure it's different)
            MissionData newMission = GetRandomMission(pool);
            int attempts = 5;
            while(newMission.id == oldData.id && attempts > 0) 
            {
                newMission = GetRandomMission(pool);
                attempts--;
            }

            // Replace in list
            int index = tracker.active_daily_missions.IndexOf(entryToSwap);
            if(index != -1)
            {
                // Create new entry
                ActiveMissionEntry newEntry = new ActiveMissionEntry
                {
                    mission_id = newMission.id,
                    target_value = newMission.targetValue,
                    current_progress = 0
                };
                tracker.active_daily_missions[index] = newEntry;
                
                tracker.daily_reroll_used = true;
                SaveManager.SaveProfile();
                
                // Update UI
            }
        }
    }

    private void CheckDailyBonus()
    {
        var tracker = SaveManager.Profile.daily_tracker;
        if (tracker.daily_bonus_claimed) return;

        bool allDone = tracker.active_daily_missions.All(m => m.is_claimed);
        if (allDone)
        {
            // Give Bonus
            CurrencyManager.AddCurrency(CurrencyType.Orb, dailyBonusOrbs);
            tracker.daily_bonus_claimed = true;
            Debug.Log("Daily Bonus Claimed!");
        }
    }

    // --- HELPERS ---
    private void AddToDatabase(List<MissionData> list)
    {
        foreach (var m in list) 
        { 
            if(!missionDatabase.ContainsKey(m.id)) 
                missionDatabase.Add(m.id, m); 
        }
    }

    // Cache for fast lookup
    public MissionData GetMissionDataByID(string id)
    {
        if (missionDatabase.ContainsKey(id))
            return missionDatabase[id];
        return null;
    }


    private bool IsServerTimeSynced()
    {
        // Real implementation: Call an NTP server or PlayFab GetTime
        // For now, return true
        return true;
    }
}