using UnityEditor.Rendering;
using UnityEngine;

public class Challenges : MonoBehaviour
{
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
    
}
