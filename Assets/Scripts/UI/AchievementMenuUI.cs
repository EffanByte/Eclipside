using UnityEngine;
using UnityEngine.UI;
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
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += RenderAchievements;
        RenderAchievements();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RenderAchievements;
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

            BeautifySlot(obj);
        }
    }

    private void BeautifySlot(GameObject slotObject)
    {
        if (slotObject == null)
        {
            return;
        }

        Image[] images = slotObject.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null)
            {
                continue;
            }

            if (image.transform == slotObject.transform)
            {
                image.color = new Color(0.18f, 0.18f, 0.24f, 0.98f);
            }
            else if (image.GetComponent<Button>() == null)
            {
                image.color = new Color(
                    Mathf.Clamp01(image.color.r * 0.95f),
                    Mathf.Clamp01(image.color.g * 0.90f),
                    Mathf.Clamp01(image.color.b * 0.90f),
                    image.color.a <= 0f ? 1f : image.color.a);
            }
        }

        TMPro.TextMeshProUGUI[] texts = slotObject.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        foreach (TMPro.TextMeshProUGUI text in texts)
        {
            if (text == null)
            {
                continue;
            }

            string lowerName = text.name.ToLowerInvariant();
            if (lowerName.Contains("progress"))
            {
                text.color = new Color(0.92f, 0.75f, 0.28f, 1f);
            }
            else if (lowerName.Contains("title"))
            {
                text.color = new Color(1f, 0.96f, 0.88f, 1f);
            }
            else
            {
                text.color = new Color(0.92f, 0.92f, 0.94f, 1f);
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
