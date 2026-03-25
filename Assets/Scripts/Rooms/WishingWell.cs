using UnityEngine;

public class WishingWell : EventObject
{

    private enum RupeeRange
    {
        Low = 10,
        Medium = 25,
        High = 50
    }

    private readonly float[] weightsLow = { 80f, 18f, 2f };   // 10 Rupees
    private readonly float[] weightsMed = { 50f, 40f, 10f };  // 25 Rupees
    private readonly float[] weightsHigh = { 20f, 50f, 30f }; // 50 Rupees

    [SerializeField] private GameObject lootPedestalPrefab;
    private int rupeeInvestment;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RandomizeInvestmentRange();
    }

    protected override void PerformEvent(PlayerController player)
    {
        Debug.Log($"Player threw {rupeeInvestment} Rupees into the well.");
        ThrowRupees(rupeeInvestment);
    }

    public void ThrowRupees(int amount)
    {
        if (PlayerController.Instance.DeductCurrency(CurrencyType.Rupee, amount))
        {
            float nothingRoll = Random.value; // Returns 0.0 to 1.0
            Debug.Log($"Nothing Roll: {nothingRoll}");
            
            if (nothingRoll <= 0.30f)
            {
                HandleNothingResult();
                return;
            }

            // 3. Determine Rarity Weights based on Investment
            float[] currentWeights = new float[3];

            switch (amount)
            {
                case 10: currentWeights = weightsLow; break;
                case 25: currentWeights = weightsMed; break;
                case 50: currentWeights = weightsHigh; break;
                default: 
                    Debug.LogWarning("Invalid Rupee Amount"); 
                    currentWeights = weightsLow; 
                    break;
            }
            // 4. Select Rarity and Spawn Item
            ItemRarity selectedRarity = GetRandomRarity(currentWeights);
            ItemData item = GameDatabase.Instance.GetRandomItem(selectedRarity);
            Debug.Log($"Selected Rarity: {selectedRarity}, Item: {item?.itemName}");
            if (item != null)
            {
                // Spawn slightly above chest
                Vector3 spawnPos = transform.position + Vector3.up * 0.2f;
                GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
                LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
                if (pedestal != null)
                {
                    pedestal.Setup(item);
                }
            }
            RandomizeInvestmentRange(); // Change the range for the next throw
    }
    else
        {
            Debug.Log("Not enough Rupees!");
        }
    }

    
        private void HandleNothingResult()
    {
        RandomizeInvestmentRange();
        Debug.Log("The well is silent...");
    }

    private ItemRarity GetRandomRarity(float[] weights)
    {
        float playerLuck = PlayerController.Instance != null ? PlayerController.Instance.GetLuckValue() : 0f;
        return LuckUtility.RollRarity(weights, playerLuck, ItemRarity.Common);
    }

    private void RandomizeInvestmentRange()
    {
                rupeeInvestment = Random.Range(0, 3) switch
        {
            0 => (int)RupeeRange.Low,
            1 => (int)RupeeRange.Medium,
            2 => (int)RupeeRange.High,
            _ => (int)RupeeRange.Low
        };
    }
}
