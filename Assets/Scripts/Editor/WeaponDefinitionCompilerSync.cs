using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WeaponDefinitionCompilerSync
{
    private const string SyncMenuPath = "Tools/Eclipside/Sync Weapon Definitions";
    private static bool syncQueued;

    static WeaponDefinitionCompilerSync()
    {
        QueueAutoSync();
    }

    [MenuItem(SyncMenuPath)]
    public static void SyncFromMenu()
    {
        SyncAllWeapons(logSummary: true);
    }

    private static void QueueAutoSync()
    {
        if (syncQueued)
        {
            return;
        }

        syncQueued = true;
        EditorApplication.delayCall += RunQueuedSync;
    }

    private static void RunQueuedSync()
    {
        syncQueued = false;

        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            QueueAutoSync();
            return;
        }

        SyncAllWeapons(logSummary: false);
    }

    private static void SyncAllWeapons(bool logSummary)
    {
        WeaponData[] weapons = LoadWeaponsFromDatabase();
        if (weapons == null || weapons.Length == 0)
        {
            if (logSummary)
            {
                Debug.LogWarning("[Weapons] No weapons found to sync.");
            }
            return;
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (WeaponData weapon in weapons)
            {
                SyncWeaponAsset(weapon);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (logSummary)
        {
            Debug.Log($"[Weapons] Synced {weapons.Length} weapon definitions into assets.");
        }
    }

    private static WeaponData[] LoadWeaponsFromDatabase()
    {
        string[] databaseGuids = AssetDatabase.FindAssets("t:GameDatabase");
        foreach (string guid in databaseGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(path);
            if (database != null && database.allWeapons != null && database.allWeapons.Count > 0)
            {
                return database.allWeapons.ToArray();
            }
        }

        List<WeaponData> weapons = new List<WeaponData>();
        string[] weaponGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Objects/Weapons" });
        foreach (string guid in weaponGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weapon != null)
            {
                weapons.Add(weapon);
            }
        }

        return weapons.ToArray();
    }

    private static void SyncWeaponAsset(WeaponData weapon)
    {
        if (weapon == null)
        {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(weapon);
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return;
        }

        RemoveEmbeddedEffects(assetPath, weapon);
        WeaponDefinitionLibrary.NormalizeWeapon(weapon);
        AttachEmbeddedEffects(assetPath, weapon);
        EditorUtility.SetDirty(weapon);
    }

    private static void RemoveEmbeddedEffects(string assetPath, WeaponData weapon)
    {
        Object[] assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in assetsAtPath)
        {
            if (asset == null || asset == weapon || !(asset is WeaponEffect))
            {
                continue;
            }

            Object.DestroyImmediate(asset, true);
        }
    }

    private static void AttachEmbeddedEffects(string assetPath, WeaponData weapon)
    {
        if (weapon.effects == null)
        {
            return;
        }

        foreach (WeaponEffect effect in weapon.effects)
        {
            if (effect == null)
            {
                continue;
            }

            effect.hideFlags = HideFlags.None;
            if (string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(effect)))
            {
                AssetDatabase.AddObjectToAsset(effect, weapon);
            }

            EditorUtility.SetDirty(effect);
        }
    }
}
