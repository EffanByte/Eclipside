using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementSlotUI : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText; // The "50/100" text
    
    [Header("Styling")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
    [SerializeField] private Color unlockedColor = new Color(1f, 1f, 1f, 1f);     // Normal

    public void Initialize(AchievementData data, int currentProgress)
    {
        // 1. Text & Icon
        titleText.text = data.title;
        descriptionText.text = data.description;
        
        if (data.rewardItem != null && data.rewardItem.icon != null)
        {
            // Use reward icon, or add a specific icon field to AchievementData if preferred
            iconImage.sprite = data.rewardItem.icon; 
        }
        
        // 2. Calculate Progress
        // Clamp so we don't show "105/100"
        int displayProgress = Mathf.Clamp(currentProgress, 0, data.targetValue);
        bool isComplete = currentProgress >= data.targetValue;

        // 3. Update Progress Text
        // Format: "50 / 100"
        progressText.text = $"{displayProgress} / {data.targetValue}";

        // 4. Styling (Terraria Style: Gray if locked, Bright if unlocked)
        if (backgroundImage != null)
        {
            backgroundImage.color = isComplete ? unlockedColor : lockedColor;
        }
        
        // Optional: Strike-through text or green color for completion
        if (isComplete)
        {
            progressText.color = Color.green;
        }
    }
}