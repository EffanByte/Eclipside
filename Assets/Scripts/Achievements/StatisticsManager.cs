using UnityEngine;
using System.Collections.Generic;
using System;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    // Key = "TOTAL_KILLS", Value = 154
    private Dictionary<string, int> stats = new Dictionary<string, int>();

    // Event for the AchievementManager to listen to
    public event Action<string, int> OnStatUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        LoadStats();
    }

    public void IncrementStat(string key, int amount = 1)
    {
        if (!stats.ContainsKey(key)) stats[key] = 0;
        
        stats[key] += amount;
        
        // Notify listeners
        OnStatUpdated?.Invoke(key, stats[key]);
        
        // Save (Ideally do this on level complete, not every kill)
        // PlayerPrefs.SetInt(key, stats[key]); 
    }

    public void SetStat(string key, int value)
    {
        stats[key] = value;
        OnStatUpdated?.Invoke(key, stats[key]);
    }

    public int GetStat(string key)
    {
        return stats.ContainsKey(key) ? stats[key] : 0;
    }

    private void LoadStats()
    {
        // Load from Save File logic here
    }
}