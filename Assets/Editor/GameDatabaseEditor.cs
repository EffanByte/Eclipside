#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameDatabase))]
public class GameDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameDatabase db = (GameDatabase)target;

        GUILayout.Space(20);
        if (GUILayout.Button("Auto-Find All Items", GUILayout.Height(40)))
        {
            FindAllItems(db);
        }
    }

    private void FindAllItems(GameDatabase db)
    {
        // 1. Find Weapons
        db.allWeapons = FindAssetsByType<WeaponData>();
        
        // 2. Find Consumables
        db.allConsumables = FindAssetsByType<ConsumableItem>();

        EditorUtility.SetDirty(db); // Mark as changed to save
        Debug.Log("Database Updated Successfully!");
    }

    private System.Collections.Generic.List<T> FindAssetsByType<T>() where T : ItemData
    {
        System.Collections.Generic.List<T> assets = new System.Collections.Generic.List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) assets.Add(asset);
        }
        return assets;
    }
}
#endif