using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WaveDefinition
{
    [Header("Wave Config")]
    public string waveName = "Wave 1";
    public List<EnemyGroup> groups; // e.g. 3 Spiders, 1 Golem
}

[System.Serializable]
public class EnemyGroup
{
    public GameObject enemyPrefab; // Drag your Spider/BarkGuardian prefab here
    public int count; // How many to spawn
}