using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))] // Set to Is Trigger = True
public class ToxicCloud : AreaEffectPool
{
    private float cloudDuration;
    [SerializeField] private ItemEffect ToxicCloudEffect;

    public void Setup(bool isElite)
    {
        // Base: 3s duration, 3 tiles radius. Elite: 3.5s duration, 3.5 tiles radius.
        cloudDuration = isElite ? 3.5f : 3.0f;

        Initialize(cloudDuration, 1f, new List<ItemEffect> { ToxicCloudEffect}, null);
    }
}