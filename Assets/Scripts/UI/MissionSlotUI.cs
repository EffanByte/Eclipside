using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionSlotUI : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TextMeshProUGUI rewardAmountText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Header("Controls")]
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject completedCheckmark;
    [SerializeField] private Slider progressBar;

    private ActiveMissionEntry currentEntry;

    public void Setup(ActiveMissionEntry entry, MissionData staticData)
    {
        currentEntry = entry;

        // Text
        progressText.text = $"{entry.current_progress} / {entry.target_value}";
        descriptionText.text = entry.description;
        // Progress Bar
        float pct = (float)entry.current_progress / entry.target_value;
        progressBar.value = pct;

        // Rewards
        rewardAmountText.text = staticData.rewardAmount.ToString();
        // rewardIcon.sprite = ... logic to pick Gold vs Orb sprite

        // State Logic
        bool isComplete = entry.current_progress >= entry.target_value;
        bool isClaimed = entry.is_claimed;

        if (isClaimed)
        {
            claimButton.gameObject.SetActive(false);
            completedCheckmark.SetActive(true);
        }
        else if (isComplete)
        {
            claimButton.gameObject.SetActive(true);
            claimButton.interactable = true;
            completedCheckmark.SetActive(false);
        }
        else
        {
            // In Progress
            claimButton.gameObject.SetActive(true);
            claimButton.interactable = false; // Greyed out
            completedCheckmark.SetActive(false);
        }
    }

    public void OnClaimClicked()
    {
        MissionManager.Instance.ClaimMission(currentEntry);
        // Refresh UI logic (usually handled by parent re-rendering)
    }
}