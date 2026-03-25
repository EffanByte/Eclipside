using UnityEngine;

public class SanctuaryOfFortune : EventObject
{
    private int price;
    [SerializeField] private GameObject lootPedestalPrefab; 
    
    void Start()
    {
        //price = Random.Range(25, 51); 
        price = 5;
    }

    protected override void PerformEvent(PlayerController player)
    {
        if (PlayerController.Instance.DeductCurrency(CurrencyType.Rupee, price))
        {
            float playerLuck = PlayerController.Instance != null ? PlayerController.Instance.GetLuckValue() : 0f;
            ItemData item = GameDatabase.Instance.GetRandomItem(playerLuck);
            if (item == null)
            {
                Debug.LogWarning("Sanctuary of Fortune could not find a reward.");
                return;
            }

            Vector3 spawnPos = transform.position + Vector3.up * 0.2f;
            GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
            LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
            if (pedestal != null)
            {
                pedestal.Setup(item);
            }
            Debug.Log($"Sanctuary rewarded a {item.itemName} at {playerLuck:0.##} luck!");
        }
        else
        {
            Debug.Log("Not enough rupees to use the Sanctuary of Fortune.");
        }
    }
}
