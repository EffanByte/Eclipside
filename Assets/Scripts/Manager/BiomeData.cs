using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Eclipside/Environment/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Header("Identity")]
    public string biomeName;
    [Tooltip("How many waves before moving to the next biome")]
    public int wavesToClear = 10; 

    [Header("Spawning Pools")]
    [Tooltip("Normal enemies for this biome")]
    public List<GameObject> commonEnemies;

    [Header("Boss Settings")]
    public List<GameObject> bossPool;    
}