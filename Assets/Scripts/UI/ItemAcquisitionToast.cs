using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemAcquisitionToast : MonoBehaviour
{
    private static ItemAcquisitionToast instance;

    private TMP_FontAsset fallbackTmpFont;
    private CanvasGroup canvasGroup;
    private Image iconImage;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI descriptionText;
    private Coroutine hideRoutine;
    private ItemData currentItem;

    public static void Show(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        ItemAcquisitionToast toast = EnsureInstance();
        if (toast == null)
        {
            Debug.LogWarning($"[UI] Could not show pickup toast for {item.name} because no active Canvas was found.");
            return;
        }

        toast.ShowInternal(item);
    }

    private static ItemAcquisitionToast EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        Canvas canvas = FindTargetCanvas();
        if (canvas == null)
        {
            return null;
        }

        GameObject root = new GameObject("ItemAcquisitionToast", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        instance = root.AddComponent<ItemAcquisitionToast>();
        return instance;
    }

    private static Canvas FindTargetCanvas()
    {
        PauseMenu pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (pauseMenu != null)
        {
            Canvas pauseCanvas = pauseMenu.GetComponentInParent<Canvas>();
            if (pauseCanvas != null)
            {
                return pauseCanvas;
            }
        }

        return FindFirstObjectByType<Canvas>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        fallbackTmpFont = LocalizedFontResolver.ResolveTmpFont(TMP_Settings.defaultFontAsset);
        BuildUi();
        SetVisible(false);
    }

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += RefreshTexts;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshTexts;
    }

    private void ShowInternal(ItemData item)
    {
        currentItem = item;
        RefreshTexts();
        SetVisible(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(4f);
        SetVisible(false);
    }

    private void BuildUi()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.sizeDelta = new Vector2(460f, 170f);
        rootRect.anchoredPosition = new Vector2(-26f, 28f);

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image panelImage = gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.09f, 0.12f, 0.16f, 0.96f);

        Outline outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.92f, 0.75f, 0.30f, 0.20f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject iconRoot = CreateUiObject("IconRoot", transform);
        Image iconRootImage = iconRoot.AddComponent<Image>();
        iconRootImage.color = new Color(0.15f, 0.18f, 0.22f, 1f);
        RectTransform iconRootRect = iconRoot.GetComponent<RectTransform>();
        iconRootRect.anchorMin = new Vector2(0f, 0.5f);
        iconRootRect.anchorMax = new Vector2(0f, 0.5f);
        iconRootRect.pivot = new Vector2(0f, 0.5f);
        iconRootRect.sizeDelta = new Vector2(96f, 96f);
        iconRootRect.anchoredPosition = new Vector2(18f, 0f);

        GameObject iconObject = CreateUiObject("Icon", iconRoot.transform);
        iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(74f, 74f);
        iconRect.anchoredPosition = Vector2.zero;

        headerText = CreateText("Header", transform, string.Empty, 18f, FontStyles.Bold, new Color(0.92f, 0.75f, 0.30f, 1f));
        RectTransform headerRect = headerText.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0f, 1f);
        headerRect.sizeDelta = new Vector2(-144f, 28f);
        headerRect.anchoredPosition = new Vector2(132f, -18f);

        nameText = CreateText("Name", transform, string.Empty, 26f, FontStyles.Bold, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.sizeDelta = new Vector2(-144f, 42f);
        nameRect.anchoredPosition = new Vector2(132f, -44f);

        descriptionText = CreateText("Description", transform, string.Empty, 17f, FontStyles.Normal, new Color(0.75f, 0.82f, 0.85f, 1f));
        descriptionText.enableWordWrapping = true;
        RectTransform descriptionRect = descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.offsetMin = new Vector2(132f, 18f);
        descriptionRect.offsetMax = new Vector2(-18f, -84f);
    }

    private void RefreshTexts()
    {
        if (currentItem == null)
        {
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = currentItem.icon;
            iconImage.enabled = currentItem.icon != null;
        }

        if (headerText != null)
        {
            headerText.text = currentItem is WeaponData
                ? L("pickup.weapon", "Weapon Acquired")
                : L("pickup.item", "Item Acquired");
        }

        if (nameText != null)
        {
            nameText.text = currentItem.GetDisplayName();
        }

        if (descriptionText != null)
        {
            string localizedDescription = currentItem.GetLocalizedDescription();
            descriptionText.text = string.IsNullOrWhiteSpace(localizedDescription)
                ? L("pickup.no_description", "No description is available for this item yet.")
                : localizedDescription;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string message, float fontSize, FontStyles fontStyle, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
        text.text = message;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created;
    }

    private string L(string key, string fallback, params object[] args)
    {
        return LocalizationManager.GetString(LocalizationManager.DefaultTable, key, fallback, args);
    }
}
