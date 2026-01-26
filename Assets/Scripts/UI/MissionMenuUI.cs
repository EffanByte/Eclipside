using UnityEngine;
using System.Collections.Generic;

public class MissionMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform listParent; // Vertical Layout Group

    private bool showDaily = true; // Tab State

    private void OnEnable()
    {
        RefreshList();
    }

    public void SwitchTab(bool daily)
    {
        showDaily = daily;
        RefreshList();
    }

    public void RefreshList()
    {
        // 1. Clear old
        foreach(Transform child in listParent) Destroy(child.gameObject);

        // 2. Get Data
        var tracker = SaveManager.Profile.daily_tracker;
        List<ActiveMissionEntry> list = showDaily ? tracker.active_daily_missions : tracker.active_weekly_missions;

        // 3. Spawn
        foreach (var entry in list)
        {
            // We need to find the Static Data (Title, Desc) using the ID
            MissionData data = MissionManager.Instance.GetMissionDataByID(entry.mission_id);
            
            if (data != null)
            {
                GameObject obj = Instantiate(slotPrefab, listParent);
                obj.GetComponent<MissionSlotUI>().Setup(entry, data);
            }
        }
    }
}