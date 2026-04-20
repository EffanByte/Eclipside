using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Eclipside.Editor
{
    [InitializeOnLoad]
    public static class MissionLocalizationTableSeeder
    {
        private const string TableName = "Missions";
        private const string AssetDirectory = "Assets/Languages";
        private const string SessionSeedKey = "Eclipside.MissionLocalizationTableSeeder.Ran";

        static MissionLocalizationTableSeeder()
        {
            EditorApplication.delayCall += SeedMissingMissionEntriesOnLoad;
        }

        [MenuItem("Tools/Eclipside/Localization/Seed Mission Table")]
        public static void SeedMissionTableFromMenu()
        {
            SeedMissionTable(logSummary: true);
        }

        private static void SeedMissingMissionEntriesOnLoad()
        {
            if (SessionState.GetBool(SessionSeedKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionSeedKey, true);
            SeedMissionTable(logSummary: false);
        }

        private static void SeedMissionTable(bool logSummary)
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

            MissionData[] missionAssets = LoadMissionAssets();
            if (missionAssets.Length == 0)
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

                foreach (MissionData mission in missionAssets)
                {
                    if (mission == null)
                    {
                        continue;
                    }

                    addedEntries += SeedEntry(table, mission.GetTitleLocalizationKey(), mission.title, ref filledValues);
                    addedEntries += SeedEntry(table, mission.GetDescriptionLocalizationKey(), mission.description, ref filledValues);
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

        private static MissionData[] LoadMissionAssets()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:MissionData", new[] { "Assets/Objects/Missions" });
            List<MissionData> missions = new List<MissionData>(assetGuids.Length);

            for (int i = 0; i < assetGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                MissionData mission = AssetDatabase.LoadAssetAtPath<MissionData>(path);
                if (mission != null)
                {
                    missions.Add(mission);
                }
            }

            return missions.ToArray();
        }
    }
}
