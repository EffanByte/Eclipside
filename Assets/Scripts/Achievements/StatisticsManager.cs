using UnityEngine;
using System.Collections.Generic;
using System;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    // Runtime storage (Fast lookup for achievements)
    private Dictionary<string, int> stats = new Dictionary<string, int>();

    public event Action<string, int> OnStatUpdated;
    private const string FILE_NAME = "Save_Stats";

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadStats();
    }

    public void IncrementStat(string key, int amount = 1)
    {
        if (!stats.ContainsKey(key)) stats[key] = 0;
        stats[key] += amount;
        
        OnStatUpdated?.Invoke(key, stats[key]);
        
        // Optional: Save on every update? 
        // Better: Save only on Level Complete or specific milestones to avoid lag.
        // For now, having auto save to ensure progress isn't lost.
        SaveStats(); 
    }

    // ---------------------------------------------------------
    // MAPPING LOGIC (Save File <-> Dictionary)
    // ---------------------------------------------------------

    private void LoadStats()
    {
        SaveFile_Stats data = SaveManager.Load<SaveFile_Stats>(FILE_NAME);

        
        stats["TOTAL_RUNS"] = data.stats.total_runs_started;
        stats["RUNS_COMPLETED"] = data.stats.total_runs_completed;
        
        // Combat
        stats["KILLS_REGULAR"] = data.stats.enemies_killed.regular;
        stats["KILLS_BOSS"] = data.stats.enemies_killed.bosses;
        stats["KILLS_MINIBOSS"] = data.stats.enemies_killed.mini_bosses;
        stats["KILLS_SYNERGY"] = data.stats.gameplay.synergy_kills;

        // Economy
        stats["CHESTS_OPENED"] = data.stats.economy.chests_opened;
        stats["GOLD_SPENT"] = data.stats.economy.gold_spent_in_shops;

        // Gameplay
        stats["ARENA_WAVE"] = data.stats.gameplay.highest_arena_wave;
        stats["PERFECT_WAVES"] = data.stats.gameplay.perfect_waves;
    }

    private void SaveStats()
    {
        // 1. Load current state (to preserve achievements/challenges)
        SaveFile_Stats data = SaveManager.Load<SaveFile_Stats>(FILE_NAME);

        // 2. Write Dictionary values back to specific fields
        if (stats.ContainsKey("TOTAL_RUNS")) data.stats.total_runs_started = stats["TOTAL_RUNS"];
        if (stats.ContainsKey("RUNS_COMPLETED")) data.stats.total_runs_completed = stats["RUNS_COMPLETED"];
        
        if (stats.ContainsKey("KILLS_REGULAR")) data.stats.enemies_killed.regular = stats["KILLS_REGULAR"];
        if (stats.ContainsKey("KILLS_BOSS")) data.stats.enemies_killed.bosses = stats["KILLS_BOSS"];
        if (stats.ContainsKey("KILLS_MINIBOSS")) data.stats.enemies_killed.mini_bosses = stats["KILLS_MINIBOSS"];
        
        if (stats.ContainsKey("CHESTS_OPENED")) data.stats.economy.chests_opened = stats["CHESTS_OPENED"];
        if (stats.ContainsKey("GOLD_SPENT")) data.stats.economy.gold_spent_in_shops = stats["GOLD_SPENT"];
        if (stats.ContainsKey("PERFECT_WAVES")) data.stats.gameplay.perfect_waves = stats["PERFECT_WAVES"];
        // 3. Save
        SaveManager.Save(FILE_NAME, data);
    }

    public int GetStat(string key)
    {
        if (stats.ContainsKey(key))
            return stats[key];
        return 0;
    }
}