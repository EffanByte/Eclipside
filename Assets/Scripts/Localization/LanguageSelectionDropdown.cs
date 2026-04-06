using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LanguageSelectionDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown tmpDropdown;
    [SerializeField] private Dropdown legacyDropdown;

    private readonly List<string> languageCodes = new List<string>();
    private bool suppressCallbacks;

    private void Awake()
    {
        if (tmpDropdown == null)
        {
            tmpDropdown = GetComponent<TMP_Dropdown>();
        }

        if (legacyDropdown == null)
        {
            legacyDropdown = GetComponent<Dropdown>();
        }
    }

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        BuildOptions();
        RefreshSelectedValue();

        if (tmpDropdown != null)
        {
            tmpDropdown.onValueChanged.AddListener(OnTmpDropdownChanged);
        }

        if (legacyDropdown != null)
        {
            legacyDropdown.onValueChanged.AddListener(OnLegacyDropdownChanged);
        }

        LocalizationManager.LanguageChanged += RefreshSelectedValue;
    }

    private void OnDisable()
    {
        if (tmpDropdown != null)
        {
            tmpDropdown.onValueChanged.RemoveListener(OnTmpDropdownChanged);
        }

        if (legacyDropdown != null)
        {
            legacyDropdown.onValueChanged.RemoveListener(OnLegacyDropdownChanged);
        }

        LocalizationManager.LanguageChanged -= RefreshSelectedValue;
    }

    private void BuildOptions()
    {
        languageCodes.Clear();
        IReadOnlyList<string> supportedCodes = LocalizationManager.GetSupportedLanguageCodes();

        List<string> labels = new List<string>(supportedCodes.Count);
        for (int i = 0; i < supportedCodes.Count; i++)
        {
            string code = supportedCodes[i];
            languageCodes.Add(code);
            labels.Add(LocalizationManager.GetDisplayNameForCode(code));
        }

        if (tmpDropdown != null)
        {
            tmpDropdown.ClearOptions();
            tmpDropdown.AddOptions(labels);
        }

        if (legacyDropdown != null)
        {
            legacyDropdown.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(labels.Count);
            for (int i = 0; i < labels.Count; i++)
            {
                options.Add(new Dropdown.OptionData(labels[i]));
            }

            legacyDropdown.AddOptions(options);
        }
    }

    private void RefreshSelectedValue()
    {
        string currentCode = LocalizationManager.GetCurrentLanguageCode();
        int index = languageCodes.IndexOf(currentCode);
        if (index < 0)
        {
            index = 0;
        }

        suppressCallbacks = true;
        if (tmpDropdown != null)
        {
            tmpDropdown.SetValueWithoutNotify(index);
        }

        if (legacyDropdown != null)
        {
            legacyDropdown.SetValueWithoutNotify(index);
        }
        suppressCallbacks = false;
    }

    private void OnTmpDropdownChanged(int index)
    {
        OnSelectionChanged(index);
    }

    private void OnLegacyDropdownChanged(int index)
    {
        OnSelectionChanged(index);
    }

    private void OnSelectionChanged(int index)
    {
        if (suppressCallbacks || index < 0 || index >= languageCodes.Count)
        {
            return;
        }

        LocalizationManager.SetLanguage(languageCodes[index]);
    }
}
