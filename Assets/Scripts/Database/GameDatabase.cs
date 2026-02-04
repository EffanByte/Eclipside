using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;

[CreateAssetMenu(menuName = "Eclipside/System/Game Database")]
public class GameDatabase : ScriptableObject
{
    // --- SINGLETON ACCESS ---
    private static GameDatabase _instance;
    public static GameDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Loads from Assets/Resources/GameDatabase
                _instance = Resources.Load<GameDatabase>("GameDatabase");
                if (_instance == null) Debug.LogError("GameDatabase not found in Resources folder!");
                else _instance.Initialize();
            }
            return _instance;
        }
    }

    [Header("Content Registry")]
    public List<WeaponData> allWeapons;
    public List<ConsumableItem> allConsumables;
    public List<CharacterData> allCharacters;

    // --- DICTIONARIES FOR FAST LOOKUP ---
    private Dictionary<string, WeaponData> weaponMap = new Dictionary<string, WeaponData>();
    private Dictionary<string, ConsumableItem> consumableMap = new Dictionary<string, ConsumableItem>();
    private Dictionary<string, CharacterData> characterMap = new Dictionary<string, CharacterData>();

    public void Initialize()
    {
        weaponMap.Clear();
        consumableMap.Clear();
        characterMap.Clear();

        // Map Weapons (Key: Asset Name or ID)
        foreach (var w in allWeapons)
        {
            if (w != null && !weaponMap.ContainsKey(w.name)) 
                weaponMap.Add(w.name, w);
        }

        // Map Consumables
        foreach (var c in allConsumables)
        {
            if (c != null && !consumableMap.ContainsKey(c.name)) 
                consumableMap.Add(c.name, c);
        }

        // Map Characters
        foreach (var c in allCharacters)
        {
            // Assuming CharacterData has a field 'characterID'
            if (c != null && !characterMap.ContainsKey(c.characterID)) 
                characterMap.Add(c.characterID, c);
        }

        Debug.Log($"Database Initialized: {allWeapons.Count} Weapons, {allConsumables.Count} Consumables.");
    }

    // ---------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------

    public WeaponData GetWeaponByID(string id)
    {
        if (weaponMap.TryGetValue(id, out WeaponData weapon)) return weapon;
        Debug.LogWarning($"Weapon ID not found: {id}");
        return null;
    }

    public ConsumableItem GetConsumableByID(string id)
    {
        if (consumableMap.TryGetValue(id, out ConsumableItem item)) return item;
        Debug.LogWarning($"Consumable ID not found: {id}");
        return null;
    }

    public ConsumableItem GetRandomConsumable()
    {
        var subset = allConsumables.ToList();
        if (subset.Count == 0) return null;
        return subset[Random.Range(0, subset.Count)];
    }


    // Helper: Get Random Item by Rarity
    public WeaponData GetRandomWeapon(ItemRarity rarity)
    {
        var subset = allWeapons.Where(w => w.rarity == rarity).ToList();
        if (subset.Count == 0) return null;
        return subset[Random.Range(0, subset.Count)];
    }

        public WeaponData GetRandomWeapon()
    {
        var subset = allWeapons.ToList();
        if (subset.Count == 0) return null;
        return subset[Random.Range(0, subset.Count)];
    }
    public ItemData GetRandomItem()
    {
        var subset = allWeapons.Cast<ItemData>().ToList();
        subset.AddRange(allConsumables);
        foreach(ItemData item in subset)
        {
            Debug.Log(item.itemName);
        }
        return subset[Random.Range(0, subset.Count)];
    }
        public ItemData GetRandomItem(ItemRarity rarity)
    {
         var subset = allWeapons.Cast<ItemData>().Where(w => w.rarity == rarity).ToList();
        subset.AddRange(allConsumables.Where(c => c.rarity == rarity));
        return subset[Random.Range(0, subset.Count)];
    }
}