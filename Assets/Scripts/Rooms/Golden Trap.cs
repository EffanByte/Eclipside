using UnityEngine;

public class GoldenTrap : EventObject
{
    [SerializeField] private Sprite openVisual;
    [SerializeField] private GameObject lootPedestalPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    protected override void PerformEvent(PlayerController player)
    {
        ItemData item = GameDatabase.Instance.GetRandomItem(ItemRarity.Epic);
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
        for (int i = 0; i < Random.Range(5,10); i++)
        {
            FindAnyObjectByType<WaveManager>().SpawnEnemy(GameDirector.Instance.currentDifficulty, 5f);
        }
    }
}
