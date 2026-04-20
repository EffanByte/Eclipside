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

    private void Awake()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimClicked);
            claimButton.onClick.AddListener(OnClaimClicked);
        }
    }

    private void OnDestroy()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimClicked);
        }
    }

    public void Setup(ActiveMissionEntry entry, MissionData staticData)
    {
        currentEntry = entry;

        progressText.text = $"{entry.current_progress} / {entry.target_value}";
        descriptionText.text = !string.IsNullOrWhiteSpace(staticData.description) ? staticData.description : entry.description;

        float pct = entry.target_value > 0 ? (float)entry.current_progress / entry.target_value : 0f;
        progressBar.value = pct;

        rewardAmountText.text = CurrencyUiUtility.FormatAmount(staticData.rewardAmount);
        ApplyRewardIcon(staticData);

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
            claimButton.gameObject.SetActive(true);
            claimButton.interactable = false;
            completedCheckmark.SetActive(false);
        }
    }

    private void ApplyRewardIcon(MissionData staticData)
    {
        if (rewardIcon == null || staticData == null)
        {
            return;
        }

        switch (staticData.rewardType)
        {
            case MissionRewardType.Gold:
                CurrencyUiUtility.ApplyCurrencySprite(rewardIcon, CurrencyType.Gold);
                break;
            case MissionRewardType.Orbs:
                CurrencyUiUtility.ApplyCurrencySprite(rewardIcon, CurrencyType.Orb);
                break;
            default:
                rewardIcon.gameObject.SetActive(false);
                break;
        }
    }

    public void OnClaimClicked()
    {
        if (claimButton != null)
        {
            claimButton.interactable = false;
        }

        MissionManager.Instance.ClaimMission(currentEntry);
    }
}
