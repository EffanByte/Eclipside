using UnityEngine;

public static class CurrencyManager
{
    private const string PROFILE_FILE = "Save_Profile";

    // ---------------------------------------------------------
    // ADD CURRENCY (Rewards, Purchases)
    // ---------------------------------------------------------
public static void AddCurrency(CurrencyType type, int amount)
    {
        // 1. USE CACHED PROFILE
        var profile = SaveManager.Profile; 

        // 2. Modify
        switch (type)
        {
            case CurrencyType.Gold:
                profile.user_profile.gold += amount;
                break;
            case CurrencyType.Orb:
                profile.user_profile.orbs += amount;
                break;
        }

        // 3. Save via Cache helper
        SaveManager.SaveProfile();
        Debug.Log($"[Economy] Added {amount} {type}. Total: {(type == CurrencyType.Gold ? profile.user_profile.gold : profile.user_profile.orbs)}");
    }

    public static bool TrySpendCurrency(CurrencyType type, int cost)
    {
        // 1. USE CACHED PROFILE
        var profile = SaveManager.Profile;
        bool transactionSuccess = false;

        // 2. Check & Deduct
        switch (type)
        {
            case CurrencyType.Gold:
                if (profile.user_profile.gold >= cost)
                {
                    profile.user_profile.gold -= cost;
                    transactionSuccess = true;
                }
                break;

            case CurrencyType.Orb:
                if (profile.user_profile.orbs >= cost)
                {
                    profile.user_profile.orbs -= cost;
                    transactionSuccess = true;
                }
                break;
        }

        // 3. Save
        if (transactionSuccess)
        {
            SaveManager.SaveProfile();
            Debug.Log($"[Economy] Spent {cost} {type}. Remaining: {(type == CurrencyType.Gold ? profile.user_profile.gold : profile.user_profile.orbs)}");
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
        var profile = SaveManager.Profile;
        switch (type)
        {
            case CurrencyType.Gold: return profile.user_profile.gold;
            case CurrencyType.Orb: return profile.user_profile.orbs;
            default: return 0;
        }
    }
}