using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private int pityThreshold = 50;
    [SerializeField] private int epicSoftPity = 10;
    [SerializeField] private TextMeshPro debugOutput;
    [SerializeField] private bool useBackend = true;

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

    public bool CanAfford(MeteoriteBanner banner, bool isTenPull)
    {
        var profile = SaveManager.Profile;
        int cost = isTenPull ? banner.tenPullCost : banner.singlePullCost;

        if (banner.currencyType == CurrencyType.Gold) return profile.user_profile.gold >= cost;
        if (banner.currencyType == CurrencyType.Orb) return profile.user_profile.orbs >= cost;

        return false;
    }

    public void PerformPull(MeteoriteBanner banner, bool isTenPull)
    {
        if (banner == null)
        {
            return;
        }

        if (useBackend)
        {
            StartCoroutine(PerformPullBackendRoutine(banner, isTenPull));
            return;
        }

        PerformPullLocal(banner, isTenPull);
    }

    private IEnumerator PerformPullBackendRoutine(MeteoriteBanner banner, bool isTenPull)
    {
        if (!CanAfford(banner, isTenPull))
        {
            SetDebugOutput("Pull failed: insufficient currency.");
            yield break;
        }

        yield return BackendApiClient.InitializePlayer(
            response => { },
            error => Debug.LogWarning($"Backend player init failed before gacha pull. {error}"));

        BackendGachaPullResponse backendResponse = null;
        string backendError = null;

        yield return BackendApiClient.GachaPull(
            banner.GetBackendBannerId(),
            isTenPull ? 10 : 1,
            response => backendResponse = response,
            error => backendError = error);

        if (backendResponse == null || !backendResponse.ok)
        {
            SetDebugOutput(string.IsNullOrWhiteSpace(backendError) ? "Pull failed." : backendError);
            Debug.LogWarning($"Gacha pull failed. {backendError}");
            yield break;
        }

        BackendApiClient.ApplyWalletToProfile(backendResponse.wallet);
        BackendApiClient.ApplyGachaToProfile(backendResponse.gacha);
        ApplyServerRewards(backendResponse.results);
        BackendApiClient.MarkProfileDirty();
        SaveManager.SaveProfile();

        SetDebugOutput(BuildPullSummary(backendResponse.results));
    }

    private void ApplyServerRewards(BackendGachaPullResult[] results)
    {
        if (results == null)
        {
            return;
        }

        var profile = SaveManager.Profile;
        foreach (var result in results)
        {
            if (result == null || result.reward == null)
            {
                continue;
            }

            var reward = result.reward;
            switch (reward.type)
            {
                case "Weapon":
                    if (!profile.weapons.unlocked_weapon_ids.Contains(reward.id))
                    {
                        profile.weapons.unlocked_weapon_ids.Add(reward.id);
                    }
                    break;

                case "Character":
                    if (!profile.characters.owned_character_ids.Contains(reward.id))
                    {
                        profile.characters.owned_character_ids.Add(reward.id);
                    }
                    break;

                case "Consumable":
                    AddConsumableToProfile(profile, reward.id, Mathf.Max(1, reward.amount));
                    break;
            }
        }
    }

    private void AddConsumableToProfile(SaveFile_Profile profile, string itemId, int amount)
    {
        for (int i = 0; i < profile.consumables.stash.Count; i++)
        {
            if (profile.consumables.stash[i].item_id == itemId)
            {
                InventoryItemEntry updated = profile.consumables.stash[i];
                updated.count += amount;
                profile.consumables.stash[i] = updated;
                return;
            }
        }

        profile.consumables.stash.Add(new InventoryItemEntry
        {
            item_id = itemId,
            count = amount
        });
    }

    private string BuildPullSummary(BackendGachaPullResult[] results)
    {
        if (results == null || results.Length == 0)
        {
            return "No rewards returned.";
        }

        List<string> lines = new List<string>();
        foreach (var result in results)
        {
            if (result == null || result.reward == null)
            {
                continue;
            }

            string rewardName = !string.IsNullOrWhiteSpace(result.reward.id)
                ? result.reward.id
                : $"{result.reward.type} x{result.reward.amount}";
            lines.Add($"Pulled: {rewardName} ({result.rarity})");
        }

        return string.Join("\n", lines);
    }

    private void SetDebugOutput(string text)
    {
        if (debugOutput != null)
        {
            debugOutput.text = text;
        }
    }

    private void PerformPullLocal(MeteoriteBanner banner, bool isTenPull)
    {
        int cost = isTenPull ? banner.tenPullCost : banner.singlePullCost;
        if (!CurrencyManager.TrySpendCurrency(banner.currencyType, cost))
        {
            Debug.LogError("Pull Failed: Insufficient Currency");
            SetDebugOutput("Pull failed: insufficient currency.");
            return;
        }

        var profile = SaveManager.Profile;
        List<PullResult> results = new List<PullResult>();
        int iterations = isTenPull ? 10 : 1;

        for (int i = 0; i < iterations; i++)
        {
            profile.gacha_state.total_pulls_lifetime++;
            profile.gacha_state.current_pity_counter++;
            profile.gacha_state.consecutive_pulls_no_epic++;

            GachaRarity selectedRarity = DetermineRarity(banner, profile.gacha_state);
            if (selectedRarity == GachaRarity.Mythical)
            {
                profile.gacha_state.current_pity_counter = 0;
            }
            if (selectedRarity == GachaRarity.Epic || selectedRarity == GachaRarity.Mythical)
            {
                profile.gacha_state.consecutive_pulls_no_epic = 0;
            }

            GachaRewardEntry item = PickItemFromPool(banner, selectedRarity);
            Debug.Log($"Pulled: {item.idName} ({selectedRarity})");
            results.Add(ProcessReward(item, profile));
        }

        SaveManager.SaveProfile();
        BackendApiClient.MarkProfileDirty();
        SetDebugOutput(BuildLocalPullSummary(results));
    }

    private string BuildLocalPullSummary(List<PullResult> results)
    {
        if (results == null || results.Count == 0)
        {
            return "No rewards returned.";
        }

        List<string> lines = new List<string>();
        foreach (var result in results)
        {
            if (result.reward != null)
            {
                lines.Add($"Pulled: {result.reward.idName}");
            }
        }
        return string.Join("\n", lines);
    }

    private GachaRarity DetermineRarity(MeteoriteBanner banner, GachaState state)
    {
        bool hardPityActive = state.current_pity_counter >= pityThreshold;
        bool epicPityActive = state.consecutive_pulls_no_epic >= epicSoftPity;

        float roll = Random.Range(0f, 100f);
        float mythicChance = hardPityActive ? 50f : banner.probMythic;
        if (roll < mythicChance) return GachaRarity.Mythical;

        float currentRoll = roll - mythicChance;
        if (epicPityActive) return GachaRarity.Epic;
        if (currentRoll < banner.probEpic) return GachaRarity.Epic;

        currentRoll -= banner.probEpic;
        if (currentRoll < banner.probRare) return GachaRarity.Rare;

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

        if (item.type == RewardType.Gold)
        {
            CurrencyManager.AddCurrency(CurrencyType.Gold, item.amount);
        }
        if (item.type == RewardType.Orb)
        {
            CurrencyManager.AddCurrency(CurrencyType.Orb, item.amount);
        }
        else if (item.type == RewardType.Consumable)
        {
            profile.consumables.stash.Add(new InventoryItemEntry
            {
                item_id = item.itemReference.itemName,
                count = item.amount
            });
        }
        else if (item.type == RewardType.Weapon)
        {
            string weaponID = item.itemReference.name;
            if (profile.weapons.unlocked_weapon_ids.Contains(weaponID))
            {
                result.isDuplicate = true;
                result.convertedAmount = item.duplicateConversionAmount;
                AddConversionCurrency(profile, item.duplicateConversionType, item.duplicateConversionAmount);
            }
            else
            {
                profile.weapons.unlocked_weapon_ids.Add(weaponID);
            }
        }
        else if (item.type == RewardType.Character)
        {
            string charID = item.characterReference.characterID;
            if (profile.characters.owned_character_ids.Contains(charID))
            {
                result.isDuplicate = true;
                result.convertedAmount = item.duplicateConversionAmount;
                AddConversionCurrency(profile, item.duplicateConversionType, item.duplicateConversionAmount);
            }
            else
            {
                profile.characters.owned_character_ids.Add(charID);
            }
        }

        return result;
    }

    private void AddConversionCurrency(SaveFile_Profile profile, CurrencyType type, int amount)
    {
        if (type == CurrencyType.Gold) CurrencyManager.AddCurrency(CurrencyType.Gold, amount);
        else if (type == CurrencyType.Orb) CurrencyManager.AddCurrency(CurrencyType.Orb, amount);
    }
}
