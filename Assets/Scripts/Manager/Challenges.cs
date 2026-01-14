
using System.Collections;
using UnityEngine;

public class Challenges : MonoBehaviour
{

    public static bool theGladiator = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FragileCrystal()
    {
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, PlayerController.Instance.GetMaxHealth()/2);
    }

    public void ThePurge()
    {
        if (GameDirector.Instance.zonePrefabs.Contains(GameObject.FindWithTag("Shop")))
            GameDirector.Instance.zonePrefabs.Remove(GameObject.FindWithTag("Shop"));
    }

    public void EndlessGreed()
    {
        TimedChest.SetGloalKeyCount(0);
        GameDirector.Instance.SetMaxWaveCount(GameDirector.Instance.GetMaxWaveCount() + 1);
    }
    public void TheGladiator()
    {
        theGladiator = true;
    }

    public void BloodForPower()
    {
        PlayerController.Instance.OnLevelUp += BFPModify;
    }

    private void BFPModify()
    {
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, -1);
    }

    public void TotalConfusion()
    {
        StartCoroutine(ConfusionRoutine());
    }

    private IEnumerator ConfusionRoutine()
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.TryAddStatus(StatusType.Confusion);
        yield return new WaitForSeconds(Random.Range(20,60));
    }

    public void TheUnlucky()
    {
        PlayerController.Instance.LockLuck();   
    }
    public void LastBreath()
    {
        PlayerController.Instance.ModifyPlayerStat(StatType.MaxHealth, 10);
    }
    public void Crossfire()
    {
        // complete later when do projectile logic for player
    }
}
