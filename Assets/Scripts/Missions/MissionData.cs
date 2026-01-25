using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Identity")]
    public string id; // Unique ID (e.g., "DAILY_E_KILL_120")
    public string title;
    [TextArea] public string description;
    
    [Header("Configuration")]
    public MissionDifficulty difficulty;
    public string statKey; // Must match StatisticsManager keys (e.g. "KILLS_REGULAR")
    public int targetValue; // e.g. 120

    [Header("Rewards")]
    public MissionRewardType rewardType;
    public int rewardAmount;
}
