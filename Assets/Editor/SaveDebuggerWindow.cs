#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SaveDebuggerWindow : EditorWindow
{
    private int cheatGoldAmount = 1000;
    private string achievementIdToUnlock = "ACH_SURVIVOR_1";

    // Define filenames to match your SaveManager logic
    private const string FILE_PROFILE = "Save_Profile";
    private const string FILE_STATS = "Save_Stats";
    private const string FILE_SETTINGS = "Save_Settings";

    [MenuItem("Tools/Save Debugger")]
    public static void ShowWindow()
    {
        GetWindow<SaveDebuggerWindow>("Save Data Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Player Profile", EditorStyles.boldLabel);

        if (GUILayout.Button("Load & Log Gold"))
        {
            var profile = SaveManager.Load<SaveFile_Profile>(FILE_PROFILE);
            Debug.Log($"Current Gold: {profile.user_profile.gold}");
        }

        GUILayout.BeginHorizontal();
        cheatGoldAmount = EditorGUILayout.IntField("Gold Amount", cheatGoldAmount);
        if (GUILayout.Button("Add Gold"))
        {
            var profile = SaveManager.Load<SaveFile_Profile>(FILE_PROFILE);
            profile.user_profile.gold += cheatGoldAmount;
            SaveManager.Save(FILE_PROFILE, profile);
            Debug.Log($"Added {cheatGoldAmount} Gold. New Total: {profile.user_profile.gold}");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
        GUILayout.Label("Progression Flags", EditorStyles.boldLabel);

        if (GUILayout.Button("Reset Tutorial Flags"))
        {
            var profile = SaveManager.Load<SaveFile_Profile>(FILE_PROFILE);
            
            // Reset Flags
            profile.progression.flags.has_completed_tutorial = false;
            // Reset Steps
            profile.progression.tutorial_steps = new TutorialSteps(); 
            
            SaveManager.Save(FILE_PROFILE, profile);
            Debug.Log("Tutorial Reset!");
        }

        if (GUILayout.Button("Unlock All Hard Modes"))
        {
            var profile = SaveManager.Load<SaveFile_Profile>(FILE_PROFILE);
            profile.progression.flags.is_hard_mode_unlocked = true;
            profile.progression.flags.is_arena_unlocked = true;
            SaveManager.Save(FILE_PROFILE, profile);
            Debug.Log("Hard Modes Unlocked!");
        }

        GUILayout.Space(20);
        GUILayout.Label("Achievements", EditorStyles.boldLabel);
        
        achievementIdToUnlock = EditorGUILayout.TextField("Achievement ID", achievementIdToUnlock);
        if (GUILayout.Button("Force Unlock Achievement"))
        {
            var statsFile = SaveManager.Load<SaveFile_Stats>(FILE_STATS);
            
            if (!statsFile.achievements.completed_achievement_ids.Contains(achievementIdToUnlock))
            {
                statsFile.achievements.completed_achievement_ids.Add(achievementIdToUnlock);
                SaveManager.Save(FILE_STATS, statsFile);
                Debug.Log($"Unlocked {achievementIdToUnlock}");
            }
            else
            {
                Debug.LogWarning("Achievement already unlocked.");
            }
        }

        GUILayout.Space(20);
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("WIPE ALL SAVE DATA"))
        {
            if (EditorUtility.DisplayDialog("Wipe Data?", "Are you sure? This cannot be undone.", "Yes, Wipe it", "Cancel"))
            {
                SaveManager.Delete(FILE_PROFILE);
                SaveManager.Delete(FILE_STATS);
                SaveManager.Delete(FILE_SETTINGS);
                Debug.LogWarning("All Save Data Deleted.");
            }
        }
    }
}
#endif