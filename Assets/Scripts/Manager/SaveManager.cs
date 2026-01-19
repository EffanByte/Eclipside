using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

// ============================================================================
// FILE 1: PROFILE DATA (Save_Profile.json)
// Stores: Assets, Currency, Progress, Unlocks. (Crucial Data)
// ============================================================================
[Serializable]
public class SaveFile_Profile
{
    public UserProfile user_profile = new UserProfile();
    public MonthlyPass monthly_pass = new MonthlyPass();
    public DailyTracker daily_tracker = new DailyTracker();
    public SaveCharacterData characters = new SaveCharacterData();
    public WeaponInventory weapons = new WeaponInventory();
    public ConsumableInventory consumables = new ConsumableInventory();
    public GachaState gacha_state = new GachaState();
    public ProgressionData progression = new ProgressionData();
}

[Serializable]
public class UserProfile
{
    public string user_id;
    public string username;
    public long date_created;           // timestamp
    public long last_login_timestamp;   // timestamp
    public int gold;
    public int orbs;
    public int arena_tickets;
    public bool is_starter_pack_purchased;
}

[Serializable]
public class MonthlyPass
{
    public bool is_active;
    public long expiration_date;        // timestamp
    public int current_exp_progress;
}

[Serializable]
public class DailyTracker
{
    public string last_reset_date;      // "YYYY-MM-DD"
    public int ads_watched_count;
    public int arena_ads_watched;
    public bool daily_login_claimed;
}

[Serializable]
public class SaveCharacterData
{
    public List<string> owned_character_ids = new List<string>();
    public string equipped_character_id;
    public List<string> skins = new List<string>();
    
    // Dictionary workaround: List of structs
    public List<CharProgressEntry> character_progress = new List<CharProgressEntry>();
}

[Serializable]
public struct CharProgressEntry
{
    public string character_id; // Key
    public bool completed_easy;
    public bool completed_normal;
    public bool completed_hard;
}

[Serializable]
public class WeaponInventory
{
    public List<string> unlocked_weapon_ids = new List<string>();
    public List<string> weapon_skins = new List<string>();
}

[Serializable]
public class ConsumableInventory
{
    // Dictionary workaround: List of structs for dynamic items
    public List<InventoryItemEntry> stash = new List<InventoryItemEntry>();
}

[Serializable]
public struct InventoryItemEntry
{
    public string item_id; // e.g., "glass_orb"
    public int count;      // e.g., 2
}

[Serializable]
public class GachaState
{
    public int total_pulls_lifetime;
    public int current_pity_counter;
    public int consecutive_pulls_no_epic;
    public int whale_pity_tracker;
}

[Serializable]
public class ProgressionData
{
    public ProgressionFlags flags = new ProgressionFlags();
    public TutorialSteps tutorial_steps = new TutorialSteps();
}

[Serializable]
public class ProgressionFlags
{
    public bool has_watched_intro_cutscene;
    public bool has_completed_tutorial;
    public bool has_defeated_final_boss;
    public bool is_hard_mode_unlocked;
    public bool is_arena_unlocked;
}

[Serializable]
public class TutorialSteps
{
    public bool initial_selection_done;
    public bool movement_done;
    public bool attack_done;
    public bool upgrade_done;
    public bool special_attack_done;
    public bool shop_purchase_done;
}

// ============================================================================
// FILE 2: STATS DATA (Save_Stats.json)
// Stores: History, Kill Counts, Achievements. (Log Data)
// ============================================================================
[Serializable]
public class SaveFile_Stats
{
    public LifetimeStats stats = new LifetimeStats();
    public AchievementData_Save achievements = new AchievementData_Save();
    public ActiveChallenges active_challenges = new ActiveChallenges();
}

[Serializable]
public class LifetimeStats
{
    public int total_runs_started;
    public int total_runs_completed;
    public int consecutive_days_played;
    public int total_days_played;
    
    // Nested Stat Groups
    public KillStats enemies_killed = new KillStats();
    public EconomyStats economy = new EconomyStats();
    public GameplayStats gameplay = new GameplayStats();
}

[Serializable]
public class KillStats
{
    public int regular;
    public int mini_bosses;
    public int bosses;
}

[Serializable]
public class EconomyStats
{
    public int chests_opened;
    public int gold_spent_in_shops;
}

[Serializable]
public class GameplayStats
{
    public int arena_entries;
    public int highest_arena_wave;
    public int synergy_kills;
}

[Serializable]
public class AchievementData_Save
{
    public List<string> completed_achievement_ids = new List<string>();
    public List<string> rewards_claimed_ids = new List<string>();
}

[Serializable]
public class ActiveChallenges
{
    public bool fragile_crystal_completed;
    public bool the_purge_completed;
    public bool endless_greed_completed;
    public bool the_gladiator_completed;
    public bool blood_for_power_completed;
    public bool crossfire_completed;
    public bool total_confusion_completed;
    public bool rain_of_fire_completed;
    public bool the_unlucky_completed;
    public bool last_breath_completed;
}

// ============================================================================
// FILE 3: SETTINGS DATA (Save_Settings.json)
// Stores: Local config. (Not synced to cloud ideally)
// ============================================================================
[Serializable]
public class SaveFile_Settings
{
    public AudioSettings audio = new AudioSettings();
    public ControlSettings controls = new ControlSettings();
    public GeneralSettings general = new GeneralSettings();
}

[Serializable]
public class AudioSettings
{
    public float music_volume; // 0.0 to 1.0
    public float sfx_volume;
    public bool is_muted;
}

[Serializable]
public class ControlSettings
{
    public bool auto_aim_enabled;
    public float joystick_size;
    public Vector2Serializable joystick_position;
}

[Serializable]
public class GeneralSettings
{
    public string language; // "en", "es", etc.
}

// Unity Vector2 is not serializable by default in all JSON libraries, 
// using a wrapper ensures safety.
[Serializable]
public struct Vector2Serializable
{
    public float x;
    public float y;

    public Vector2Serializable(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    // Implicit conversion for ease of use
    public static implicit operator Vector2(Vector2Serializable v) => new Vector2(v.x, v.y);
    public static implicit operator Vector2Serializable(Vector2 v) => new Vector2Serializable(v.x, v.y);
}
public static class SaveManager
{
    private static string BasePath => Application.persistentDataPath;

    // --- NEW: THE CACHE ---
    private static SaveFile_Profile _cachedProfile;

    // Accessor: Always returns the live object in memory
    public static SaveFile_Profile Profile
    {
        get
        {
            if (_cachedProfile == null)
            {
                // Load from disk if we haven't yet
                _cachedProfile = Load<SaveFile_Profile>("Save_Profile");
            }
            return _cachedProfile;
        }
    }

    // Call this to write the cache to disk
    public static void SaveProfile()
    {
        if (_cachedProfile != null)
        {
            Save("Save_Profile", _cachedProfile);
        }
    }

    // --- Existing Methods (Keep these) ---
    public static void Save<T>(string filename, T data)
    {
        string path = Path.Combine(BasePath, filename + ".json");
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        
        // Debug.Log($"Saved {filename}"); // Comment out to reduce spam
    }

    public static T Load<T>(string filename) where T : new()
    {
        string path = Path.Combine(BasePath, filename + ".json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
        return new T();
    }
    
    // Helper to force reload if needed (e.g. after wiping data)
    public static void RefreshCache()
    {
        _cachedProfile = null;
    }
}