using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Eclipside.Editor
{
    [InitializeOnLoad]
    public static class AchievementLocalizationTableSeeder
    {
        private const string TableName = "Achievements";
        private const string AssetDirectory = "Assets/Languages";
        private const string SessionSeedKey = "Eclipside.AchievementLocalizationTableSeeder.Ran";

        static AchievementLocalizationTableSeeder()
        {
            EditorApplication.delayCall += SeedMissingAchievementEntriesOnLoad;
        }

        [MenuItem("Tools/Eclipside/Localization/Seed Achievement Table")]
        public static void SeedAchievementTableFromMenu()
        {
            SeedAchievementTable(logSummary: true);
        }

        private static void SeedMissingAchievementEntriesOnLoad()
        {
            if (SessionState.GetBool(SessionSeedKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionSeedKey, true);
            SeedAchievementTable(logSummary: false);
        }

        private static void SeedAchievementTable(bool logSummary)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(TableName, AssetDirectory);
                if (collection == null)
                {
                    Debug.LogError($"[Localization] Failed to create the {TableName} string table collection.");
                    return;
                }
            }

            AchievementData[] achievementAssets = LoadAchievementAssets();
            if (achievementAssets.Length == 0)
            {
                return;
            }

            int addedEntries = 0;
            int filledValues = 0;

            foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            {
                StringTable table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    string tablePath = $"{AssetDirectory}/{TableName}_{locale.Identifier.Code}.asset";
                    table = collection.AddNewTable(locale.Identifier, tablePath) as StringTable;
                }

                if (table == null)
                {
                    continue;
                }

                foreach (AchievementData achievement in achievementAssets)
                {
                    if (achievement == null)
                    {
                        continue;
                    }

                    addedEntries += SeedEntry(table, achievement.GetTitleLocalizationKey(), achievement.title, ref filledValues);
                    addedEntries += SeedEntry(table, achievement.GetDescriptionLocalizationKey(), achievement.description, ref filledValues);
                }

                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();

            if (logSummary)
            {
                Debug.Log($"[Localization] Seeded {TableName} table. Added {addedEntries} entries and filled {filledValues} empty values.");
            }
        }

        private static int SeedEntry(StringTable table, string key, string fallback, ref int filledValues)
        {
            if (table == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(fallback))
            {
                return 0;
            }

            StringTableEntry entry = table.GetEntry(key);
            if (entry == null)
            {
                table.AddEntry(key, fallback);
                return 1;
            }

            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                entry.Value = fallback;
                filledValues++;
            }

            return 0;
        }

        private static AchievementData[] LoadAchievementAssets()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:AchievementData", new[] { "Assets/Objects/Achievements" });
            List<AchievementData> achievements = new List<AchievementData>(assetGuids.Length);

            for (int i = 0; i < assetGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                AchievementData achievement = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
                if (achievement != null)
                {
                    achievements.Add(achievement);
                }
            }

            return achievements.ToArray();
        }
    }
}
