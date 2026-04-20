using UnityEngine;
using System.Collections.Generic;

public class MissionMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform listParent;

    private bool showDaily = true;

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += RefreshList;
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionStateChanged += RefreshList;
        }

        RefreshList();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshList;
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
            BeautifySlot(obj);
        }
    }

    private void BeautifySlot(GameObject slotObject)
    {
        if (slotObject == null)
        {
            return;
        }

        UnityEngine.UI.Image[] images = slotObject.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (UnityEngine.UI.Image image in images)
        {
            if (image == null)
            {
                continue;
            }

            if (image.transform == slotObject.transform)
            {
                image.color = new Color(0.18f, 0.18f, 0.24f, 0.98f);
            }
            else if (image.GetComponent<UnityEngine.UI.Button>() == null)
            {
                image.color = new Color(
                    Mathf.Clamp01(image.color.r * 0.95f),
                    Mathf.Clamp01(image.color.g * 0.90f),
                    Mathf.Clamp01(image.color.b * 0.90f),
                    image.color.a <= 0f ? 1f : image.color.a);
            }
        }

        TMPro.TextMeshProUGUI[] texts = slotObject.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        foreach (TMPro.TextMeshProUGUI text in texts)
        {
            if (text == null)
            {
                continue;
            }

            string lowerName = text.name.ToLowerInvariant();
            if (lowerName.Contains("reward") || lowerName.Contains("progress"))
            {
                text.color = new Color(0.92f, 0.75f, 0.28f, 1f);
            }
            else
            {
                text.color = new Color(0.92f, 0.92f, 0.94f, 1f);
            }
        }

        UnityEngine.UI.Slider slider = slotObject.GetComponentInChildren<UnityEngine.UI.Slider>(true);
        if (slider != null)
        {
            UnityEngine.UI.Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<UnityEngine.UI.Image>() : null;
            if (fill != null)
            {
                fill.color = new Color(0.92f, 0.75f, 0.28f, 1f);
            }
        }
    }
}
