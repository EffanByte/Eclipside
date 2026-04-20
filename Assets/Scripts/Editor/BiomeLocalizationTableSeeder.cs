using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Eclipside.Editor
{
    [InitializeOnLoad]
    public static class BiomeLocalizationTableSeeder
    {
        private const string TableName = "Biomes";
        private const string AssetDirectory = "Assets/Languages";
        private const string SessionSeedKey = "Eclipside.BiomeLocalizationTableSeeder.Ran";

        static BiomeLocalizationTableSeeder()
        {
            EditorApplication.delayCall += SeedMissingBiomeEntriesOnLoad;
        }

        [MenuItem("Tools/Eclipside/Localization/Seed Biome Table")]
        public static void SeedBiomeTableFromMenu()
        {
            SeedBiomeTable(logSummary: true);
        }

        private static void SeedMissingBiomeEntriesOnLoad()
        {
            if (SessionState.GetBool(SessionSeedKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionSeedKey, true);
            SeedBiomeTable(logSummary: false);
        }

        private static void SeedBiomeTable(bool logSummary)
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

            BiomeData[] biomeAssets = LoadBiomeAssets();
            if (biomeAssets.Length == 0)
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

                foreach (BiomeData biome in biomeAssets)
                {
                    if (biome == null || string.IsNullOrWhiteSpace(biome.biomeName))
                    {
                        continue;
                    }

                    string key = biome.GetResolvedLocalizationKey();
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    StringTableEntry entry = table.GetEntry(key);
                    if (entry == null)
                    {
                        table.AddEntry(key, biome.biomeName);
                        addedEntries++;
                    }
                    else if (string.IsNullOrWhiteSpace(entry.Value))
                    {
                        entry.Value = biome.biomeName;
                        filledValues++;
                    }
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

        private static BiomeData[] LoadBiomeAssets()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:BiomeData", new[] { "Assets/Objects/Biomes" });
            List<BiomeData> biomes = new List<BiomeData>(assetGuids.Length);

            for (int i = 0; i < assetGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                BiomeData biome = AssetDatabase.LoadAssetAtPath<BiomeData>(path);
                if (biome != null)
                {
                    biomes.Add(biome);
                }
            }

            return biomes.ToArray();
        }
    }
}
