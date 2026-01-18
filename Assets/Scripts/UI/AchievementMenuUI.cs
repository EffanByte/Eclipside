using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AchievementMenuUI : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform contentContainer;

    [Header("Data Source")]
    // We populate this list automatically via the Context Menu below
    [SerializeField] private List<AchievementData> achievementsList;

    private void OnEnable()
    {
        RenderAchievements();
    }

    private void RenderAchievements()
    {
        // 1. Clear existing items to prevent duplicates
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Loop through data
        foreach (AchievementData achievement in achievementsList)
        {
            // 3. Fetch live progress from StatisticsManager
            int currentVal = StatisticsManager.Instance.GetStat(achievement.statKey);

            // 4. Spawn & Setup
            GameObject obj = Instantiate(slotPrefab, contentContainer);
            AchievementSlotUI slot = obj.GetComponent<AchievementSlotUI>();
            
            if (slot != null)
            {
                slot.Initialize(achievement, currentVal);
            }
        }
    }

    // ---------------------------------------------------------
    // EDITOR AUTOMATION
    // ---------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("Load Achievements From Folder")]
    private void LoadFromFolder()
    {
        achievementsList.Clear();
        
        // Searches the project for AchievementData assets
        string[] guids = AssetDatabase.FindAssets("t:AchievementData", new[] { "Assets/Objects/Achievements" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AchievementData asset = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
            if (asset != null)
            {
                achievementsList.Add(asset);
            }
        }
        
        Debug.Log($"Loaded {achievementsList.Count} achievements from Assets/Objects/Achievements");
        
        // Mark object as dirty to save the list
        EditorUtility.SetDirty(this);
    }
#endif
}