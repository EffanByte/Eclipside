using UnityEngine;

public enum AchievementType
{
    Incremental,    // Count up to X (Kills, Gold Spent)
    State,          // Boolean check (Complete run < 1 heart)
    DateCheck       // Daily logins
}

[CreateAssetMenu(menuName = "Eclipside/Achievement")]
public class AchievementData : ScriptableObject
{
    [Header("Identity")]
    public string id; // Unique Key e.g., "ACH_KILL_100"
    public string title;
    [TextArea] public string description;

    [Header("Logic")]
    public AchievementType type;
    public string statKey; // The variable we are watching (e.g., "TOTAL_KILLS")
    public int targetValue; // 100, 1000, 5000

    [Header("Reward")]
    public RewardType rewardType;
    public int rewardAmount; // For Gold/Orbs
    public ItemData rewardItem; // For Consumables
}