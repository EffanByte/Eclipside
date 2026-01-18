using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class ChallengeUI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject togglePrefab; // A UI Toggle Prefab
    [SerializeField] private Transform container; // The Content of your ScrollView

    private void Start()
    {
        GenerateToggles();
    }

    private void GenerateToggles()
    {
        // Clear existing
        foreach (Transform child in container) Destroy(child.gameObject);

        // Loop through Enum
        foreach (ChallengeType type in Enum.GetValues(typeof(ChallengeType)))
        {
            GameObject obj = Instantiate(togglePrefab, container);
            
            // Setup Text
            TextMeshProUGUI label = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = FormatName(type.ToString());

            // Setup Toggle
            Toggle toggle = obj.GetComponent<Toggle>();
            
            // Set initial state based on Manager
            toggle.isOn = ChallengeManager.Instance.IsChallengeActive(type);

            // Add Listener
            toggle.onValueChanged.AddListener((isOn) => 
            {
                ChallengeManager.Instance.ToggleChallenge(type, isOn);
            });
        }
    }

    // Helper to make "FragileCrystal" look like "Fragile Crystal"
    private string FormatName(string enumName)
    {
        return System.Text.RegularExpressions.Regex.Replace(enumName, "(\\B[A-Z])", " $1");
    }
}
