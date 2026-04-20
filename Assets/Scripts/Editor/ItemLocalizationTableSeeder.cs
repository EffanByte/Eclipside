using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Eclipside.Editor
{
    [InitializeOnLoad]
    public static class ItemLocalizationTableSeeder
    {
        private const string TableName = "Items";
        private const string AssetDirectory = "Assets/Languages";
        private const string SessionSeedKey = "Eclipside.ItemLocalizationTableSeeder.Ran";

        static ItemLocalizationTableSeeder()
        {
            EditorApplication.delayCall += SeedMissingEntriesOnLoad;
        }

        [MenuItem("Tools/Eclipside/Localization/Seed Item Table")]
        public static void SeedTableFromMenu()
        {
            SeedTable(logSummary: true);
        }

        private static void SeedMissingEntriesOnLoad()
        {
            if (SessionState.GetBool(SessionSeedKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionSeedKey, true);
            SeedTable(logSummary: false);
        }

        private static void SeedTable(bool logSummary)
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

            ItemData[] assets = LoadAssets();
            if (assets.Length == 0)
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

                foreach (ItemData asset in assets)
                {
                    if (asset == null)
                    {
                        continue;
                    }

                    addedEntries += SeedEntry(table, asset.GetNameLocalizationKey(), asset.itemName, ref filledValues);
                    addedEntries += SeedEntry(table, asset.GetDescriptionLocalizationKey(), asset.description, ref filledValues);
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

        private static ItemData[] LoadAssets()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Objects/Items" });
            List<ItemData> assets = new List<ItemData>(assetGuids.Length);

            for (int i = 0; i < assetGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                ItemData asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (asset != null && !(asset is WeaponData))
                {
                    assets.Add(asset);
                }
            }

            return assets.ToArray();
        }
    }
}
