using UnityEngine;

public class CursedChest : EventObject
{    
    [SerializeField] private Sprite openVisual;
    [SerializeField] private GameObject lootPedestalPrefab;
    protected override void PerformEvent(PlayerController player)
    {
        float chance = Random.Range(0f, 1f);
        if (chance < 0.5f){
        ItemData item = GameDatabase.Instance.GetRandomItem(ItemRarity.Mythical);
        // 2. Spawn Visual Loot
        if (item != null)
        {
            // Spawn slightly above chest
            Vector3 spawnPos = transform.position + Vector3.up * 0.2f;
            GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
            LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
            if (pedestal != null)
            {
                pedestal.Setup(item);
                StatisticsManager.Instance.IncrementStat("CHESTS_OPENED");
            }
            GetComponent<SpriteRenderer>().sprite = openVisual;
        }
        }
        else
        {
            // Spawn miniboss here later
        }
    }
}
