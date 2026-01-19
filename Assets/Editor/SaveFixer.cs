#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SaveFixer : MonoBehaviour
{
    [MenuItem("Tools/Fix Save Data")]
    public static void ResetAndFix()
    {
        // 1. Create a clean profile
        SaveFile_Profile cleanProfile = new SaveFile_Profile();
        
        // 2. Setup Default Inventory (The Correct List Format)
        cleanProfile.consumables.stash = new List<InventoryItemEntry>
        {
            new InventoryItemEntry { item_id = "glass_orb", count = 2 },
            new InventoryItemEntry { item_id = "large_potion", count = 1 }
        };

        // 3. Setup Default User
        cleanProfile.user_profile.gold = 1000;
        cleanProfile.user_profile.orbs = 100;
        
        // 4. Save to the REAL path
        SaveManager.Save("Save_Profile", cleanProfile);
        
        Debug.Log($"<color=green>FIXED!</color> Valid JSON written to: {Application.persistentDataPath}");
        
        // Open the folder so you can see it
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif