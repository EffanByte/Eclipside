using UnityEngine;
using System;
using System.Collections;
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

    [Header("Backend")]
    [SerializeField] private bool useBackend = true;
    [SerializeField] private float progressFlushDelay = 0.5f;

    private readonly Dictionary<string, MissionData> missionDatabase = new Dictionary<string, MissionData>();
    private readonly Dictionary<string, int> pendingProgressDeltas = new Dictionary<string, int>();

    private Coroutine progressFlushCoroutine;
    private bool backendReady;

    public event Action OnMissionStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        AddToDatabase(easyPool);
        AddToDatabase(mediumPool);
        AddToDatabase(hardPool);
        AddToDatabase(weeklyPool);
    }

    private void Start()
    {
        if (!BackendRuntimeSettings.IsEnabled)
        {
            useBackend = false;
        }

        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.OnStatChanged += OnStatUpdated;
        }

        if (useBackend)
        {
            StartCoroutine(InitializeFromBackend());
            return;
        }

        CheckResets();
        NotifyMissionStateChanged();
    }

    private void OnDestroy()
    {
        if (StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.OnStatChanged -= OnStatUpdated;
        }
    }

    public bool IsUsingBackend() => useBackend;

    private IEnumerator InitializeFromBackend()
    {
        bool initSucceeded = false;

        yield return BackendApiClient.InitializePlayer(
            response =>
            {
                initSucceeded = true;
                if (response != null)
                {
                    BackendApiClient.ApplyWalletToProfile(response.wallet);
                    SaveManager.SaveProfile();
                }
            },
            error =>
            {
                Debug.LogWarning($"Backend init failed, falling back to local missions. {error}");
            });

        if (!initSucceeded)
        {
            useBackend = false;
            CheckResets();
            NotifyMissionStateChanged();
            yield break;
        }

        yield return RefreshMissionStateFromBackend();
    }

    private IEnumerator RefreshMissionStateFromBackend()
    {
        bool requestSucceeded = false;

        yield return BackendApiClient.RequestMissionsState(
            response =>
            {
                requestSucceeded = true;
                backendReady = true;
                ApplyBackendMissionState(response);
            },
            error =>
            {
                Debug.LogWarning($"Mission state sync failed. {error}");
            });

        if (!requestSucceeded)
        {
            backendReady = false;
        }
    }

    private void ApplyBackendMissionState(BackendMissionsStateResponse response)
    {
        if (response == null)
        {
            return;
        }

        var tracker = SaveManager.Profile.daily_tracker;
        tracker.daily_reroll_used = response.dailyRerollUsed;
        tracker.daily_bonus_claimed = response.dailyBonusClaimed;
        tracker.weekly_bonus_claimed = response.weeklyBonusClaimed;
        tracker.last_daily_reset_date = response.dayIndex.ToString();
        tracker.last_weekly_reset_date = response.weekIndex.ToString();
        RegisterBackendMissionData(response.dailyMissions, MissionDifficulty.Easy);
        RegisterBackendMissionData(response.weeklyMissions, MissionDifficulty.Weekly);
        tracker.active_daily_missions = ConvertMissionEntries(response.dailyMissions);
        tracker.active_weekly_missions = ConvertMissionEntries(response.weeklyMissions);

        BackendApiClient.ApplyWalletToProfile(response.wallet);
        SaveManager.SaveProfile();
        NotifyMissionStateChanged();
    }

    private List<ActiveMissionEntry> ConvertMissionEntries(BackendMissionEntryDto[] entries)
    {
        var converted = new List<ActiveMissionEntry>();
        if (entries == null)
        {
            return converted;
        }

        foreach (var entry in entries)
        {
            string description = entry.title;
            MissionData staticData = GetMissionDataByID(entry.missionId);
            if (staticData != null && !string.IsNullOrWhiteSpace(staticData.description))
            {
                description = staticData.description;
            }

            converted.Add(new ActiveMissionEntry
            {
                mission_id = entry.missionId,
                description = description,
                current_progress = entry.currentProgress,
                target_value = entry.targetValue,
                is_completed = entry.isCompleted,
                is_claimed = entry.isClaimed
            });
        }

        return converted;
    }

    private void RegisterBackendMissionData(BackendMissionEntryDto[] entries, MissionDifficulty fallbackDifficulty)
    {
        if (entries == null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.missionId))
            {
                continue;
            }

            if (missionDatabase.TryGetValue(entry.missionId, out MissionData existing))
            {
                if (!string.IsNullOrWhiteSpace(entry.title))
                {
                    existing.title = entry.title;
                }

                if (string.IsNullOrWhiteSpace(existing.description))
                {
                    existing.description = entry.title;
                }

                existing.targetValue = entry.targetValue;
                ApplyBackendRewardToMission(existing, entry.reward);
                continue;
            }

            MissionData runtimeMission = ScriptableObject.CreateInstance<MissionData>();
            runtimeMission.id = entry.missionId;
            runtimeMission.title = string.IsNullOrWhiteSpace(entry.title) ? entry.missionId : entry.title;
            runtimeMission.description = runtimeMission.title;
            runtimeMission.statKey = entry.statKey;
            runtimeMission.targetValue = entry.targetValue;
            runtimeMission.difficulty = GuessDifficulty(entry.missionId, fallbackDifficulty);
            ApplyBackendRewardToMission(runtimeMission, entry.reward);
            missionDatabase[entry.missionId] = runtimeMission;
        }
    }

    private MissionDifficulty GuessDifficulty(string missionId, MissionDifficulty fallbackDifficulty)
    {
        if (string.IsNullOrWhiteSpace(missionId))
        {
            return fallbackDifficulty;
        }

        string normalized = missionId.ToLowerInvariant();
        if (normalized.Contains("weekly")) return MissionDifficulty.Weekly;
        if (normalized.Contains("hard")) return MissionDifficulty.Hard;
        if (normalized.Contains("medium")) return MissionDifficulty.Medium;
        if (normalized.Contains("easy")) return MissionDifficulty.Easy;
        return fallbackDifficulty;
    }

    private void ApplyBackendRewardToMission(MissionData mission, BackendReward reward)
    {
        if (mission == null || reward == null)
        {
            return;
        }

        mission.rewardAmount = reward.amount;
        switch (reward.type)
        {
            case "Orbs":
                mission.rewardType = MissionRewardType.Orbs;
                break;
            case "Ticket":
                mission.rewardType = MissionRewardType.Ticket;
                break;
            default:
                mission.rewardType = MissionRewardType.Gold;
                break;
        }
    }
    private void QueueMissionProgress(string statKey, int amount)
    {
        if (string.IsNullOrWhiteSpace(statKey) || amount == 0)
        {
            return;
        }

        if (!pendingProgressDeltas.ContainsKey(statKey))
        {
            pendingProgressDeltas[statKey] = 0;
        }

        pendingProgressDeltas[statKey] += amount;

        if (progressFlushCoroutine == null)
        {
            progressFlushCoroutine = StartCoroutine(FlushMissionProgressRoutine());
        }
    }

    private IEnumerator FlushMissionProgressRoutine()
    {
        yield return new WaitForSeconds(progressFlushDelay);

        float waitTime = 0f;
        while (useBackend && !backendReady && waitTime < 5f)
        {
            yield return new WaitForSeconds(0.25f);
            waitTime += 0.25f;
        }

        if (useBackend && !backendReady)
        {
            Debug.LogWarning("Skipping backend mission progress flush because the backend is not ready.");
            progressFlushCoroutine = null;
            yield break;
        }

        var batch = new List<KeyValuePair<string, int>>(pendingProgressDeltas);
        pendingProgressDeltas.Clear();

        foreach (var delta in batch)
        {
            yield return BackendApiClient.PostMissionProgress(
                delta.Key,
                delta.Value,
                response => { },
                error => Debug.LogWarning($"Mission progress sync failed for {delta.Key}. {error}"));
        }

        yield return RefreshMissionStateFromBackend();
        progressFlushCoroutine = null;

        if (pendingProgressDeltas.Count > 0)
        {
            progressFlushCoroutine = StartCoroutine(FlushMissionProgressRoutine());
        }
    }

    private void NotifyMissionStateChanged()
    {
        OnMissionStateChanged?.Invoke();
    }

    private void CheckResets()
    {
        var tracker = SaveManager.Profile.daily_tracker;
        DateTime now = DateTime.UtcNow;
        string todayStr = now.ToString("yyyy-MM-dd");

        if (tracker.last_daily_reset_date != todayStr)
        {
            GenerateDailyMissions(tracker);
            tracker.last_daily_reset_date = todayStr;
            tracker.daily_reroll_used = false;
            tracker.daily_bonus_claimed = false;
            SaveManager.SaveProfile();
        }

        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime lastMonday = now.AddDays(-1 * diff).Date;
        string weekStr = lastMonday.ToString("yyyy-MM-dd");

        if (tracker.last_weekly_reset_date != weekStr)
        {
            GenerateWeeklyMissions(tracker);
            tracker.last_weekly_reset_date = weekStr;
            tracker.weekly_bonus_claimed = false;
            SaveManager.SaveProfile();
        }
    }

    private void GenerateDailyMissions(DailyTracker tracker)
    {
        tracker.active_daily_missions.Clear();
        AddMissionToTracker(tracker.active_daily_missions, GetRandomMission(easyPool));
        AddMissionToTracker(tracker.active_daily_missions, GetRandomMission(mediumPool));
        AddMissionToTracker(tracker.active_daily_missions, GetRandomMission(hardPool));
    }

    private void GenerateWeeklyMissions(DailyTracker tracker)
    {
        tracker.active_weekly_missions.Clear();
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

        list.Add(new ActiveMissionEntry
        {
            mission_id = data.id,
            description = data.description,
            target_value = data.targetValue,
            current_progress = 0,
            is_completed = false,
            is_claimed = false
        });
    }

    private void OnStatUpdated(string statKey, int totalValue, int amountAdded)
    {
        if (useBackend)
        {
            QueueMissionProgress(statKey, amountAdded);

            if (statKey == "RUNS_COMPLETED" && StatisticsManager.Instance != null && !StatisticsManager.Instance.HasDiedThisRun())
            {
                QueueMissionProgress("RUN_NO_DEATH", 1);
            }

            if (statKey == "KILLS_MINIBOSS" && StatisticsManager.Instance != null && !StatisticsManager.Instance.HasDiedThisRun())
            {
                QueueMissionProgress("MINIBOSS_NO_DEATH", 1);
            }
            return;
        }

        var tracker = SaveManager.Profile.daily_tracker;
        UpdateMissionList(tracker.active_daily_missions, statKey, amountAdded);
        UpdateMissionList(tracker.active_weekly_missions, statKey, amountAdded);

        if (statKey == "RUNS_COMPLETED" && StatisticsManager.Instance != null && !StatisticsManager.Instance.HasDiedThisRun())
        {
            UpdateMissionList(tracker.active_daily_missions, "RUN_NO_DEATH", 1);
        }

        if (statKey == "KILLS_MINIBOSS" && StatisticsManager.Instance != null && !StatisticsManager.Instance.HasDiedThisRun())
        {
            UpdateMissionList(tracker.active_daily_missions, "MINIBOSS_NO_DEATH", 1);
        }
    }

    private void UpdateMissionList(List<ActiveMissionEntry> missions, string key, int amountToAdd)
    {
        bool changed = false;
        foreach (var entry in missions)
        {
            if (entry.is_completed) continue;

            if (missionDatabase.TryGetValue(entry.mission_id, out MissionData data) && data.statKey == key)
            {
                entry.current_progress += amountToAdd;
                if (entry.current_progress >= entry.target_value)
                {
                    entry.current_progress = entry.target_value;
                    entry.is_completed = true;
                }
                changed = true;
            }
        }

        if (changed)
        {
            SaveManager.SaveProfile();
            NotifyMissionStateChanged();
        }
    }

    public void ClaimMission(ActiveMissionEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (useBackend)
        {
            StartCoroutine(ClaimMissionBackendRoutine(entry));
            return;
        }

        if (!entry.is_completed || entry.is_claimed) return;

        if (missionDatabase.TryGetValue(entry.mission_id, out MissionData data))
        {
            entry.is_claimed = true;
            if (data.rewardType == MissionRewardType.Gold)
                CurrencyManager.AddCurrency(CurrencyType.Gold, data.rewardAmount);
            else if (data.rewardType == MissionRewardType.Orbs)
                CurrencyManager.AddCurrency(CurrencyType.Orb, data.rewardAmount);

            CheckDailyBonus();
            SaveManager.SaveProfile();
            NotifyMissionStateChanged();
        }
    }

    private IEnumerator ClaimMissionBackendRoutine(ActiveMissionEntry entry)
    {
        bool claimSucceeded = false;

        yield return BackendApiClient.ClaimMission(
            entry.mission_id,
            response => claimSucceeded = response != null && response.ok,
            error => Debug.LogWarning($"Mission claim failed. {error}"));

        if (claimSucceeded)
        {
            yield return RefreshMissionStateFromBackend();
        }
    }

    public void RerollDailyMission(ActiveMissionEntry entryToSwap)
    {
        if (entryToSwap == null)
        {
            return;
        }

        if (useBackend)
        {
            StartCoroutine(RerollMissionBackendRoutine(entryToSwap));
            return;
        }

        var tracker = SaveManager.Profile.daily_tracker;
        if (tracker.daily_reroll_used) return;

        if (missionDatabase.TryGetValue(entryToSwap.mission_id, out MissionData oldData))
        {
            List<MissionData> pool = null;
            switch (oldData.difficulty)
            {
                case MissionDifficulty.Easy: pool = easyPool; break;
                case MissionDifficulty.Medium: pool = mediumPool; break;
                case MissionDifficulty.Hard: pool = hardPool; break;
            }

            MissionData newMission = GetRandomMission(pool);
            int attempts = 5;
            while (newMission != null && newMission.id == oldData.id && attempts > 0)
            {
                newMission = GetRandomMission(pool);
                attempts--;
            }

            int index = tracker.active_daily_missions.IndexOf(entryToSwap);
            if (index != -1 && newMission != null)
            {
                tracker.active_daily_missions[index] = new ActiveMissionEntry
                {
                    mission_id = newMission.id,
                    description = newMission.description,
                    target_value = newMission.targetValue,
                    current_progress = 0,
                    is_completed = false,
                    is_claimed = false
                };

                tracker.daily_reroll_used = true;
                SaveManager.SaveProfile();
                NotifyMissionStateChanged();
            }
        }
    }

    private IEnumerator RerollMissionBackendRoutine(ActiveMissionEntry entryToSwap)
    {
        bool rerollSucceeded = false;

        yield return BackendApiClient.RerollMission(
            entryToSwap.mission_id,
            response => rerollSucceeded = response != null && response.ok,
            error => Debug.LogWarning($"Mission reroll failed. {error}"));

        if (rerollSucceeded)
        {
            yield return RefreshMissionStateFromBackend();
        }
    }

    private void CheckDailyBonus()
    {
        var tracker = SaveManager.Profile.daily_tracker;
        if (tracker.daily_bonus_claimed) return;

        bool allDone = tracker.active_daily_missions.All(m => m.is_claimed);
        if (allDone)
        {
            CurrencyManager.AddCurrency(CurrencyType.Orb, dailyBonusOrbs);
            tracker.daily_bonus_claimed = true;
        }
    }

    private void AddToDatabase(List<MissionData> list)
    {
        if (list == null)
        {
            return;
        }

        foreach (var mission in list)
        {
            if (mission != null && !missionDatabase.ContainsKey(mission.id))
            {
                missionDatabase.Add(mission.id, mission);
            }
        }
    }

    public MissionData GetMissionDataByID(string id)
    {
        if (missionDatabase.ContainsKey(id))
            return missionDatabase[id];
        return null;
    }
}


