using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GachaAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private Transform impactPoint;
    [SerializeField] private Animator templeAnimator; // For floor crack
    [SerializeField] private Transform rewardsContainer; // Where icons appear

    [Header("Trail Colors")]
    [SerializeField] private Color colCommon = Color.white;
    [SerializeField] private Color colRare = Color.blue;
    [SerializeField] private Color colEpic = Color.magenta;
    [SerializeField] private Color colMythic = Color.red;

    public void PlayPullSequence(List<GachaManager.PullResult> results)
    {
        StartCoroutine(SequenceRoutine(results));
    }

    private IEnumerator SequenceRoutine(List<GachaManager.PullResult> results)
    {
        // 1. Determine highest rarity for the Big Meteor
        GachaRarity highest = GachaRarity.Common;
        foreach (var res in results)
        {
            if (res.reward.rarity > highest) highest = res.reward.rarity;
        }

        // 2. Spawn Main Meteor
        SpawnMeteor(highest, isBig: true);

        // If 10-pull, spawn small ones
        if (results.Count > 1)
        {
            for (int i = 0; i < 9; i++)
            {
                SpawnMeteor(GachaRarity.Common, isBig: false); // Visual filler
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(1.5f); // Wait for impact

        // 3. Reveal Logic
        if (highest == GachaRarity.Mythical)
        {
            // Dramatic floor crack
            templeAnimator.SetTrigger("CrackFloor");
            yield return new WaitForSeconds(1.0f);
        }

        // 4. Show Rewards One by One
        foreach (var res in results)
        {
            ShowRewardUI(res);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SpawnMeteor(GachaRarity rarity, bool isBig)
    {
        GameObject obj = Instantiate(meteorPrefab, impactPoint.position + Vector3.up * 10, Quaternion.identity);
        float scale = isBig ? 2f : 1f;
        obj.transform.localScale = Vector3.one * scale;

        // Set Trail Color
        TrailRenderer trail = obj.GetComponentInChildren<TrailRenderer>();
        if (trail != null)
        {
            Color c = GetColor(rarity);
            trail.startColor = c;
            trail.endColor = new Color(c.r, c.g, c.b, 0);
        }
        
        // Simple fall animation
        // Rigidbody or Tween handles falling down
    }

    private Color GetColor(GachaRarity r)
    {
        switch (r)
        {
            case GachaRarity.Common: return colCommon;
            case GachaRarity.Rare: return colRare;
            case GachaRarity.Epic: return colEpic;
            case GachaRarity.Mythical: return colMythic;
            default: return Color.white;
        }
    }

    private void ShowRewardUI(GachaManager.PullResult result)
    {
        // Instantiate a UI Card prefab
        Debug.Log($"Revealed: {result.reward.idName} ({result.reward.rarity})");
        if (result.isDuplicate) Debug.Log($"Duplicate! Converted to {result.convertedAmount}");
    }
}