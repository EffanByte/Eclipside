using UnityEngine;

public static class CurrencyManager
{
    private const string PROFILE_FILE = "Save_Profile";

    // ---------------------------------------------------------
    // ADD CURRENCY (Rewards, Purchases)
    // ---------------------------------------------------------
    public static void AddCurrency(CurrencyType type, int amount)
    {
        // 1. Load Data
        var profile = SaveManager.Load<SaveFile_Profile>(PROFILE_FILE);

        // 2. Modify
        switch (type)
        {
            case CurrencyType.Gold:
                profile.user_profile.gold += amount;
                Debug.Log($"[Economy] Added {amount} Gold. Total: {profile.user_profile.gold}");
                break;

            case CurrencyType.Orb:
                profile.user_profile.orbs += amount;
                Debug.Log($"[Economy] Added {amount} Orbs. Total: {profile.user_profile.orbs}");
                break;
                
        }

        // 3. Save Immediately
        SaveManager.Save(PROFILE_FILE, profile);
        Debug.Log("[Economy] Profile saved after currency addition.");
    }

    // ---------------------------------------------------------
    // CHARGE CURRENCY (Spending)
    // Returns TRUE if transaction succeeded, FALSE if insufficient funds
    // ---------------------------------------------------------
    public static bool TrySpendCurrency(CurrencyType type, int cost)
    {
        // 1. Load Data
        var profile = SaveManager.Load<SaveFile_Profile>(PROFILE_FILE);
        bool transactionSuccess = false;

        // 2. Check & Deduct
        switch (type)
        {
            case CurrencyType.Gold:
                if (profile.user_profile.gold >= cost)
                {
                    profile.user_profile.gold -= cost;
                    transactionSuccess = true;
                    Debug.Log($"[Economy] Spent {cost} Gold. Remaining: {profile.user_profile.gold}");
                }
                break;

            case CurrencyType.Orb:
                if (profile.user_profile.orbs >= cost)
                {
                    profile.user_profile.orbs -= cost;
                    transactionSuccess = true;
                    Debug.Log($"[Economy] Spent {cost} Orbs. Remaining: {profile.user_profile.orbs}");
                }
                break;
        }

        // 3. Save ONLY if data changed
        if (transactionSuccess)
        {
            SaveManager.Save(PROFILE_FILE, profile);
        }
        else
        {
            Debug.LogWarning($"[Economy] Transaction Failed: Not enough {type}");
        }

        return transactionSuccess;
    }

    // ---------------------------------------------------------
    // HELPER: CHECK BALANCE (Without spending)
    // ---------------------------------------------------------
    public static int GetBalance(CurrencyType type)
    {
        var profile = SaveManager.Load<SaveFile_Profile>(PROFILE_FILE);
        switch (type)
        {
            case CurrencyType.Gold: return profile.user_profile.gold;
            case CurrencyType.Orb: return profile.user_profile.orbs;
            default: return 0;
        }
    }
}