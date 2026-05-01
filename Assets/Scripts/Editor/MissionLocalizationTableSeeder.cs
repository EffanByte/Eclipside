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
        private static readonly Dictionary<string, Dictionary<string, string>> LocalizedMissionEntries = new Dictionary<string, Dictionary<string, string>>
        {
            ["es"] = new Dictionary<string, string>
            {
                ["mission.arena_wave_20.description"] = "Alcanza la oleada 20 en la arena",
                ["mission.chests_opened_3.description"] = "Abre 3 cofres",
                ["mission.chests_opened_25.description"] = "Abre 25 cofres",
                ["mission.chests_opened_1.description"] = "Abre 1 cofre",
                ["mission.daily_e_run_1.description"] = "Completa una partida",
                ["mission.defeat_enemy_120.description"] = "Derrota a 120 enemigos",
                ["mission.kills_status_25.description"] = "Derrota a 25 enemigos afectados por un efecto de estado.",
                ["mission.kills_miniboss_1.description"] = "Derrota a 1 minijefe de portal",
                ["mission.kills_miniboss_2.description"] = "Derrota a 2 minijefes de portal.",
                ["mission.kills_miniboss_12.description"] = "Derrota a 12 minijefes de portal.",
                ["mission.boss_nodeath.description"] = "Derrota a un minijefe sin morir",
                ["mission.daily_h_nodeath.description"] = "Completa una partida sin morir",
                ["mission.portals_opened_1.description"] = "Activa 1 portal.",
                ["mission.items_purchased_3.description"] = "Compra 3 objetos en la tienda",
                ["mission.rupee_spent_250.description"] = "Gasta 250 de oro en total en la tienda.",
                ["mission.rupee_spent_2500.description"] = "Gasta 2500 rupias en total en la tienda.",
            },
            ["ja"] = new Dictionary<string, string>
            {
                ["mission.arena_wave_20.description"] = "アリーナでウェーブ20に到達する",
                ["mission.chests_opened_3.description"] = "宝箱を3個開ける",
                ["mission.chests_opened_25.description"] = "宝箱を25個開ける",
                ["mission.chests_opened_1.description"] = "宝箱を1個開ける",
                ["mission.daily_e_run_1.description"] = "1回ランを完了する",
                ["mission.defeat_enemy_120.description"] = "敵を120体倒す",
                ["mission.kills_status_25.description"] = "状態異常の敵を25体倒す",
                ["mission.kills_miniboss_1.description"] = "ポータルのミニボスを1体倒す",
                ["mission.kills_miniboss_2.description"] = "ポータルのミニボスを2体倒す",
                ["mission.kills_miniboss_12.description"] = "ポータルのミニボスを12体倒す",
                ["mission.boss_nodeath.description"] = "死なずにミニボスを倒す",
                ["mission.daily_h_nodeath.description"] = "死なずに1回ランを完了する",
                ["mission.portals_opened_1.description"] = "ポータルを1つ起動する",
                ["mission.items_purchased_3.description"] = "ショップでアイテムを3個購入する",
                ["mission.rupee_spent_250.description"] = "ショップで合計250ゴールド使う",
                ["mission.rupee_spent_2500.description"] = "ショップで合計2500ルピー使う",
            },
            ["ru"] = new Dictionary<string, string>
            {
                ["mission.arena_wave_20.description"] = "Достигните 20-й волны на арене",
                ["mission.chests_opened_3.description"] = "Откройте 3 сундука",
                ["mission.chests_opened_25.description"] = "Откройте 25 сундуков",
                ["mission.chests_opened_1.description"] = "Откройте 1 сундук",
                ["mission.daily_e_run_1.description"] = "Завершите 1 забег",
                ["mission.defeat_enemy_120.description"] = "Победите 120 врагов",
                ["mission.kills_status_25.description"] = "Победите 25 врагов под эффектом состояния.",
                ["mission.kills_miniboss_1.description"] = "Победите 1 портального мини-босса",
                ["mission.kills_miniboss_2.description"] = "Победите 2 портальных мини-боссов.",
                ["mission.kills_miniboss_12.description"] = "Победите 12 портальных мини-боссов.",
                ["mission.boss_nodeath.description"] = "Победите мини-босса, не умирая",
                ["mission.daily_h_nodeath.description"] = "Завершите забег, не умирая",
                ["mission.portals_opened_1.description"] = "Активируйте 1 портал.",
                ["mission.items_purchased_3.description"] = "Купите 3 предмета в магазине",
                ["mission.rupee_spent_250.description"] = "Потратьте в магазине 250 золота.",
                ["mission.rupee_spent_2500.description"] = "Потратьте в магазине 2500 рупий.",
            },
        };

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

                    addedEntries += SeedEntry(table, locale.Identifier.Code, mission.GetTitleLocalizationKey(), mission.title, ref filledValues);
                    addedEntries += SeedEntry(table, locale.Identifier.Code, mission.GetDescriptionLocalizationKey(), mission.description, ref filledValues);
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

        private static int SeedEntry(StringTable table, string localeCode, string key, string fallback, ref int filledValues)
        {
            if (table == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(fallback))
            {
                return 0;
            }

            string localizedValue = GetLocalizedMissionValue(localeCode, key, fallback);

            StringTableEntry entry = table.GetEntry(key);
            if (entry == null)
            {
                table.AddEntry(key, localizedValue);
                return 1;
            }

            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                entry.Value = localizedValue;
                filledValues++;
            }

            return 0;
        }

        private static string GetLocalizedMissionValue(string localeCode, string key, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(localeCode)
                && LocalizedMissionEntries.TryGetValue(localeCode, out Dictionary<string, string> localeEntries)
                && localeEntries.TryGetValue(key, out string localizedValue)
                && !string.IsNullOrWhiteSpace(localizedValue))
            {
                return localizedValue;
            }

            return fallback;
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
