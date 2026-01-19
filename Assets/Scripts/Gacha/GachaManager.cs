using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private int pityThreshold = 50; // 50 pulls
    [SerializeField] private int epicSoftPity = 10;  // 10 pulls
    [SerializeField] private TextMeshPro debugOutput;

    // Internal results structure
    public struct PullResult
    {
        public GachaRewardEntry reward;
        public bool isDuplicate;
        public int convertedAmount;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ---------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------

    public bool CanAfford(MeteoriteBanner banner, bool isTenPull)
    {
        var profile = SaveManager.Profile; 
        int cost = isTenPull ? banner.tenPullCost : banner.singlePullCost;
        
        if (banner.currencyType == CurrencyType.Gold) return profile.user_profile.gold >= cost;
        if (banner.currencyType == CurrencyType.Orb) return profile.user_profile.orbs >= cost;
        
        return false;
    }

    public List<PullResult> PerformPull(MeteoriteBanner banner, bool isTenPull)
    {
       int cost = isTenPull ? banner.tenPullCost : banner.singlePullCost;

        // --- THE FIX ---
        // Attempt to charge. If it fails, stop immediately.
        // This handles loading, checking balance, deducting, and saving internally.
        if (!CurrencyManager.TrySpendCurrency(banner.currencyType, cost))
        {
            Debug.LogError("Pull Failed: Insufficient Currency");
            return null; 
        }

        var profile = SaveManager.Profile; 

        // 3. Calculate Results
        List<PullResult> results = new List<PullResult>();
        int iterations = isTenPull ? 10 : 1;

        for (int i = 0; i < iterations; i++)
        {
            // Update Trackers
            profile.gacha_state.total_pulls_lifetime++;
            profile.gacha_state.current_pity_counter++;
            profile.gacha_state.consecutive_pulls_no_epic++;

            // Determine Rarity
            GachaRarity selectedRarity = DetermineRarity(banner, profile.gacha_state);

            // Reset counters based on result
            if (selectedRarity == GachaRarity.Mythical)
            {
                // Soft reset: If fail, pity set to 25. If Success, reset to 0.
                profile.gacha_state.current_pity_counter = 0;
            }
            if (selectedRarity == GachaRarity.Epic || selectedRarity == GachaRarity.Mythical)
            {
                profile.gacha_state.consecutive_pulls_no_epic = 0;
            }

            // Pick Item
            GachaRewardEntry item = PickItemFromPool(banner, selectedRarity);
            Debug.Log($"Pulled: {item.idName} ({selectedRarity})");
            debugOutput.text = $"\nPulled: {item.idName} ({selectedRarity})";
            // Process Item (Add to inventory / Handle Duplicate)
            PullResult result = ProcessReward(item, profile);
            results.Add(result);
        }

        // 4. Save Data
        SaveManager.SaveProfile();

        // 5. Return for Animation
        return results;
    }

    // ---------------------------------------------------------
    // INTERNAL LOGIC
    // ---------------------------------------------------------

    private GachaRarity DetermineRarity(MeteoriteBanner banner, GachaState state)
    {
        // Rule: After 50 pulls, Mythic chance boosted to 50%
        bool hardPityActive = state.current_pity_counter >= pityThreshold;
        
        // Rule: Every 10 continuous pulls guarantees Epic (Soft Pity)
        // Note: The logic implies if we are at 9, the 10th must be Epic or better.
        bool epicPityActive = state.consecutive_pulls_no_epic >= epicSoftPity;

        float roll = Random.Range(0f, 100f);

        // 1. Check Mythic
        float mythicChance = hardPityActive ? 50f : banner.probMythic;
        if (roll < mythicChance) return GachaRarity.Mythical;

        // Hard Pity Fail Logic: "If fail, pity counter set to 25"
        // If we were at 50+, rolled for 50%, and FAILED (landed here), we reset to 25 later?
        // Actually, if hard pity active and we failed mythic, 
        // we essentially wasted the "Boosted" roll. We don't reset counter unless we get it?
        // acc maybe "Soft reset: If fail, pity counter set to 25 (half)."
        // This implies if we hit 50, didn't get mythic, we drop back to 25.
        // Handled in the loop logic if needed, but usually pity guarantees eventually.

        // 2. Check Epic
        float currentRoll = roll - mythicChance; // Shift window
        
        // If Epic Pity is active, we force Epic (unless we got Mythic above)
        if (epicPityActive) return GachaRarity.Epic;

        if (currentRoll < banner.probEpic) return GachaRarity.Epic;

        // 3. Check Rare
        currentRoll -= banner.probEpic;
        if (currentRoll < banner.probRare) return GachaRarity.Rare;

        // 4. Common
        return GachaRarity.Common;
    }

    private GachaRewardEntry PickItemFromPool(MeteoriteBanner banner, GachaRarity rarity)
    {
        List<GachaRewardEntry> pool = null;
        switch (rarity)
        {
            case GachaRarity.Common: pool = banner.commonPool; break;
            case GachaRarity.Rare: pool = banner.rarePool; break;
            case GachaRarity.Epic: pool = banner.epicPool; break;
            case GachaRarity.Mythical: pool = banner.mythicPool; break;
        }

        if (pool == null || pool.Count == 0)
        {
            Debug.LogError($"Pool for {rarity} in {banner.name} is empty!");
            return new GachaRewardEntry { idName = "Fallback Gold", type = RewardType.Currency, amount = 100 };
        }

        return pool[Random.Range(0, pool.Count)];
    }

    private PullResult ProcessReward(GachaRewardEntry item, SaveFile_Profile profile)
    {
        PullResult result = new PullResult { reward = item, isDuplicate = false, convertedAmount = 0 };

        // 1. CURRENCY
        if (item.type == RewardType.Gold)
        {
            CurrencyManager.AddCurrency(CurrencyType.Gold, item.amount);
        }
                if (item.type == RewardType.Orb)
        {
            CurrencyManager.AddCurrency(CurrencyType.Orb, item.amount);
        }
        // 2. CONSUMABLE
        else if (item.type == RewardType.Consumable)
        {
            profile.consumables.stash.Add(new InventoryItemEntry { 
                item_id = item.itemReference.itemName, 
                count = item.amount 
            });
        }
        // 3. WEAPON
        else if (item.type == RewardType.Weapon)
        {
            string weaponID = item.itemReference.name; // Uses WeaponData asset name
            if (profile.weapons.unlocked_weapon_ids.Contains(weaponID))
            {
                result.isDuplicate = true;
                result.convertedAmount = item.duplicateConversionAmount;
                Debug.Log($"Duplicate Weapon: {weaponID}. Converted to {result.convertedAmount}");
                AddConversionCurrency(profile, item.duplicateConversionType, item.duplicateConversionAmount);
            }
            else
            {
                profile.weapons.unlocked_weapon_ids.Add(weaponID);
            }
        }
        // 4. CHARACTER (NEW LOGIC)
        else if (item.type == RewardType.Character)
        {
            // Use the ID from CharacterData (e.g. "char_knight")
            string charID = item.characterReference.characterID; 

            // Check Save File for ownership
            if (profile.characters.owned_character_ids.Contains(charID))
            {
                // DUPLICATE: Convert to currency
                result.isDuplicate = true;
                result.convertedAmount = item.duplicateConversionAmount;
                AddConversionCurrency(profile, item.duplicateConversionType, item.duplicateConversionAmount);
                
                Debug.Log($"Duplicate Character: {charID}. Converted to {result.convertedAmount}");
            }
            else
            {
                // NEW: Unlock it
                profile.characters.owned_character_ids.Add(charID);
                Debug.Log($"Unlocked New Character: {charID}");
            }
        }

        return result;
    }

    // Helper to keep code clean
    private void AddConversionCurrency(SaveFile_Profile profile, CurrencyType type, int amount)
    {
        if (type == CurrencyType.Gold) CurrencyManager.AddCurrency(CurrencyType.Gold, amount);
        else if (type == CurrencyType.Orb) CurrencyManager.AddCurrency(CurrencyType.Orb, amount);
    }
}