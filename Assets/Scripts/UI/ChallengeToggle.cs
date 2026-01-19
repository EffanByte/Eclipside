using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class ChallengeToggle : MonoBehaviour
{
    [Header("Settings")]
    public ChallengeType challengeType;
    public TextMeshProUGUI label; // Optional: Drag the label here to auto-name it

    private void Start()
    {
        Toggle toggle = GetComponent<Toggle>();
        
        label = label ?? GetComponentInChildren<TextMeshProUGUI>();
        // Auto-name the label for convenience
        if(label != null) label.text = challengeType.ToString();

        // 1. Load previous state (optional)
        toggle.isOn = ChallengeManager.Instance.activeChallenges.Contains(challengeType);

        // 2. Listen for clicks
        toggle.onValueChanged.AddListener((isOn) => 
        {
            ChallengeManager.Instance.ToggleChallenge(challengeType, isOn);
        });
    }
}