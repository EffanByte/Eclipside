using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LocalizedTextLabel : MonoBehaviour
{
    [SerializeField] private string tableCollection = LocalizationManager.DefaultTable;
    [SerializeField] private string entryKey;
    [SerializeField] private string fallbackText;
    [SerializeField] private bool uppercase;
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private Text legacyText;

    private void Reset()
    {
        CacheTargets();
    }

    private void Awake()
    {
        CacheTargets();
    }

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        string text = LocalizationManager.GetString(tableCollection, entryKey, fallbackText);
        if (uppercase)
        {
            text = text.ToUpperInvariant();
        }

        if (tmpText != null)
        {
            tmpText.text = text;
        }

        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }

    public void SetEntry(string newTableCollection, string newEntryKey, string newFallbackText = null)
    {
        tableCollection = string.IsNullOrWhiteSpace(newTableCollection) ? LocalizationManager.DefaultTable : newTableCollection;
        entryKey = newEntryKey;
        fallbackText = newFallbackText ?? fallbackText;
        Refresh();
    }

    private void CacheTargets()
    {
        if (tmpText == null)
        {
            tmpText = GetComponent<TMP_Text>();
        }

        if (legacyText == null)
        {
            legacyText = GetComponent<Text>();
        }
    }
}
