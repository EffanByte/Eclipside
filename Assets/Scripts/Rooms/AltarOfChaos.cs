using UnityEngine;

public class AltarOfChaos : EventObject
{   
    private float currentDamageIncrease = 0f;
    private bool isBuffActive = false;
    [SerializeField] private GameObject lootPedestalPrefab;
    [SerializeField] private ItemData reward;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameDirector.Instance.OnWaveAdvanced += RemoveBuff; 
    }


    void OnDisable()
    {
        GameDirector.Instance.OnWaveAdvanced -= RemoveBuff;
    }
    
    protected override void PerformEvent(PlayerController player)
    {
        AlterOfChaos();
    }

    private void AlterOfChaos()
    {
        float chance = Random.Range(0f, 1f);
        if (chance < 0.5f)
        {
            DamageInfo info = new DamageInfo(
            amount: 10f,
            element: DamageElement.True,
            style: AttackStyle.Environment,     
            sourcePosition: transform.position,
            knockbackForce: 0f
            );
            PlayerController.Instance.ReceiveDamage(info);
            currentDamageIncrease = PlayerController.Instance.GetBaseDamage() * 0.3f;
            // need to change this logic later to reflect a proper % buff rather than a flat numeric increase
            PlayerController.Instance.ApplyPermanentBuff(StatType.BaseDamage, currentDamageIncrease);
            Debug.Log("Player took damage but gained damage buff!");
            isBuffActive = true;
        }
        else
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.2f;
            GameObject lootObj = Instantiate(lootPedestalPrefab, spawnPos, Quaternion.identity);
            LootPedestal pedestal = lootObj.GetComponent<LootPedestal>();
            if (pedestal != null)
            {
                pedestal.Setup(reward);
            }
        }
        }
    private void RemoveBuff(int waveAdvanced)
    {
        if (isBuffActive)
        {
            PlayerController.Instance.ApplyPermanentBuff(StatType.BaseDamage, -currentDamageIncrease); 
            isBuffActive = false;
        }
    }
}
