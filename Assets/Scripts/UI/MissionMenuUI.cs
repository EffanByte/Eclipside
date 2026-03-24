using UnityEngine;
using System.Collections.Generic;

public class MissionMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform listParent;

    private bool showDaily = true;

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStateChanged += RefreshList;
        }

        RefreshList();
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStateChanged -= RefreshList;
        }
    }

    public void SwitchTab(bool daily)
    {
        showDaily = daily;
        RefreshList();
    }

    public void RefreshList()
    {
        if (listParent == null)
        {
            return;
        }

        foreach (Transform child in listParent)
        {
            Destroy(child.gameObject);
        }

        var tracker = SaveManager.Profile.daily_tracker;
        List<ActiveMissionEntry> list = showDaily ? tracker.active_daily_missions : tracker.active_weekly_missions;

        foreach (var entry in list)
        {
            MissionData data = MissionManager.Instance != null ? MissionManager.Instance.GetMissionDataByID(entry.mission_id) : null;
            if (data == null)
            {
                continue;
            }

            GameObject obj = Instantiate(slotPrefab, listParent);
            obj.GetComponent<MissionSlotUI>().Setup(entry, data);
        }
    }
}
