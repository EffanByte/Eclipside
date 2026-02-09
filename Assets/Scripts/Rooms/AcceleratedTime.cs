using UnityEngine;

public class AcceleratedTime : EventObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    protected override void PerformEvent(PlayerController player)
    {
        
        GameDirector.Instance.OnLevelCompleted += GoldPayout;
        FindAnyObjectByType<WaveManager>().spawnStagger /= 2;
    }

    private void GoldPayout()
    {
        PlayerController.Instance.AddCurrency(CurrencyType.Rupee, PlayerController.Instance.rupees);
    }

    private void OnDisable()
    {
        GameDirector.Instance.OnLevelCompleted -= GoldPayout;
    }
}
