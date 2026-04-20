using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private TMP_FontAsset fallbackTmpFont;
    private CanvasGroup canvasGroup;
    private GameObject cardRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI controlsTitleText;
    private TextMeshProUGUI controlsSubtitleText;
    private Button resumeButton;
    private Button retryButton;
    private Button mainMenuButton;
    private Button quitButton;
    private readonly System.Collections.Generic.List<ControlRow> controlRows = new System.Collections.Generic.List<ControlRow>();

    private sealed class ControlRow
    {
        public string ActionKey;
        public string ActionFallback;
        public string BindingText;
        public TextMeshProUGUI ActionText;
        public TextMeshProUGUI BindingTextComponent;
    }

    private void Start()
    {
        fallbackTmpFont = TMP_Settings.defaultFontAsset;
        BuildPauseMenuUi();
        SetMenuVisible(false);
    }

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += RefreshTexts;
        RefreshTexts();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshTexts;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsMenuVisible())
            {
                OnResumeButtonPressed();
            }
            else
            {
                OnPauseButtonPressed();
            }
        }
    }

    public void OnPauseButtonPressed()
    {
        Time.timeScale = 0f;
        SetMenuVisible(true);
        RefreshTexts();
    }

    public void OnResumeButtonPressed()
    {
        Time.timeScale = 1f;
        SetMenuVisible(false);
    }

    public void OnReturnToMainMenuButtonPressed()
    {
        Time.timeScale = 1f;
        RunSceneTransitionState.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnRetryButtonPressed()
    {
        Time.timeScale = 1f;
        if (GameDirector.Instance != null)
        {
            RunSceneTransitionState.SetBiomeState(
                GameDirector.Instance.CurrentBiomeIndex,
                GameDirector.Instance.CurrentDifficultyValue,
                GameDirector.Instance.GetTimer());
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitGameButtonPressed()
    {
        SaveManager.SaveProfile();
        SaveManager.SaveSettings();
        Application.Quit();
    }

    private void BuildPauseMenuUi()
    {
        HideLegacyChildren();

        Image overlay = gameObject.GetComponent<Image>();
        if (overlay == null)
        {
            overlay = gameObject.AddComponent<Image>();
        }
        overlay.color = new Color(0f, 0f, 0f, 0.82f);

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        RectTransform overlayRect = GetComponent<RectTransform>();
        if (overlayRect != null)
        {
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }

        cardRoot = CreateUiObject("PauseCard", transform);
        Image cardImage = cardRoot.AddComponent<Image>();
        cardImage.color = new Color(0.09f, 0.12f, 0.16f, 0.98f);
        Outline cardOutline = cardRoot.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.92f, 0.75f, 0.30f, 0.24f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform cardRect = cardRoot.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(960f, 560f);
        cardRect.anchoredPosition = Vector2.zero;

        titleText = CreateTmpText("Title", cardRoot.transform, string.Empty, 42f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(-64f, 58f);
        titleRect.anchoredPosition = new Vector2(32f, -28f);

        subtitleText = CreateTmpText("Subtitle", cardRoot.transform, string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.72f, 0.82f, 0.85f, 1f));
        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(-64f, 54f);
        subtitleRect.anchoredPosition = new Vector2(32f, -88f);

        GameObject divider = CreateUiObject("Divider", cardRoot.transform);
        Image dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(0.92f, 0.75f, 0.30f, 0.95f);
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(0f, 1f);
        dividerRect.pivot = new Vector2(0f, 1f);
        dividerRect.sizeDelta = new Vector2(150f, 4f);
        dividerRect.anchoredPosition = new Vector2(32f, -154f);

        GameObject buttonColumn = CreateUiObject("ButtonColumn", cardRoot.transform);
        RectTransform buttonColumnRect = buttonColumn.GetComponent<RectTransform>();
        buttonColumnRect.anchorMin = new Vector2(0f, 0f);
        buttonColumnRect.anchorMax = new Vector2(0f, 1f);
        buttonColumnRect.pivot = new Vector2(0f, 0.5f);
        buttonColumnRect.sizeDelta = new Vector2(360f, 0f);
        buttonColumnRect.anchoredPosition = new Vector2(32f, -77f);

        VerticalLayoutGroup layout = buttonColumn.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        resumeButton = CreateMenuButton("ResumeButton", buttonColumn.transform, string.Empty, OnResumeButtonPressed, true);
        retryButton = CreateMenuButton("RetryButton", buttonColumn.transform, string.Empty, OnRetryButtonPressed, false);
        mainMenuButton = CreateMenuButton("MainMenuButton", buttonColumn.transform, string.Empty, OnReturnToMainMenuButtonPressed, false);
        quitButton = CreateMenuButton("QuitButton", buttonColumn.transform, string.Empty, OnQuitGameButtonPressed, false);

        BuildControlsPanel(cardRoot.transform);

        RefreshTexts();
    }

    private void BuildControlsPanel(Transform parent)
    {
        GameObject controlsPanel = CreateUiObject("ControlsPanel", parent);
        Image controlsPanelImage = controlsPanel.AddComponent<Image>();
        controlsPanelImage.color = new Color(0.13f, 0.16f, 0.20f, 0.92f);

        Outline controlsOutline = controlsPanel.AddComponent<Outline>();
        controlsOutline.effectColor = new Color(0.92f, 0.75f, 0.30f, 0.12f);
        controlsOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform controlsRect = controlsPanel.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(1f, 0f);
        controlsRect.anchorMax = new Vector2(1f, 1f);
        controlsRect.pivot = new Vector2(1f, 0.5f);
        controlsRect.sizeDelta = new Vector2(500f, -190f);
        controlsRect.anchoredPosition = new Vector2(-32f, -78f);

        controlsTitleText = CreateTmpText("ControlsTitle", controlsPanel.transform, string.Empty, 28f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform controlsTitleRect = controlsTitleText.rectTransform;
        controlsTitleRect.anchorMin = new Vector2(0f, 1f);
        controlsTitleRect.anchorMax = new Vector2(1f, 1f);
        controlsTitleRect.pivot = new Vector2(0f, 1f);
        controlsTitleRect.sizeDelta = new Vector2(-44f, 36f);
        controlsTitleRect.anchoredPosition = new Vector2(22f, -20f);

        controlsSubtitleText = CreateTmpText("ControlsSubtitle", controlsPanel.transform, string.Empty, 17f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.70f, 0.79f, 0.84f, 1f));
        RectTransform controlsSubtitleRect = controlsSubtitleText.rectTransform;
        controlsSubtitleRect.anchorMin = new Vector2(0f, 1f);
        controlsSubtitleRect.anchorMax = new Vector2(1f, 1f);
        controlsSubtitleRect.pivot = new Vector2(0f, 1f);
        controlsSubtitleRect.sizeDelta = new Vector2(-44f, 46f);
        controlsSubtitleRect.anchoredPosition = new Vector2(22f, -60f);

        GameObject controlsDivider = CreateUiObject("ControlsDivider", controlsPanel.transform);
        Image controlsDividerImage = controlsDivider.AddComponent<Image>();
        controlsDividerImage.color = new Color(0.92f, 0.75f, 0.30f, 0.78f);
        RectTransform controlsDividerRect = controlsDivider.GetComponent<RectTransform>();
        controlsDividerRect.anchorMin = new Vector2(0f, 1f);
        controlsDividerRect.anchorMax = new Vector2(1f, 1f);
        controlsDividerRect.pivot = new Vector2(0.5f, 1f);
        controlsDividerRect.sizeDelta = new Vector2(-44f, 2f);
        controlsDividerRect.anchoredPosition = new Vector2(0f, -112f);

        GameObject controlsContent = CreateUiObject("ControlsContent", controlsPanel.transform);
        RectTransform controlsContentRect = controlsContent.GetComponent<RectTransform>();
        controlsContentRect.anchorMin = new Vector2(0f, 0f);
        controlsContentRect.anchorMax = new Vector2(1f, 1f);
        controlsContentRect.offsetMin = new Vector2(22f, 22f);
        controlsContentRect.offsetMax = new Vector2(-22f, -126f);

        VerticalLayoutGroup controlsLayout = controlsContent.AddComponent<VerticalLayoutGroup>();
        controlsLayout.spacing = 10f;
        controlsLayout.padding = new RectOffset(0, 0, 0, 0);
        controlsLayout.childControlWidth = true;
        controlsLayout.childControlHeight = true;
        controlsLayout.childForceExpandWidth = true;
        controlsLayout.childForceExpandHeight = false;

        AddControlRow(controlsContent.transform, "controls.move", "Move", "WASD / Arrows | Left Stick");
        AddControlRow(controlsContent.transform, "controls.attack", "Attack", "F / Enter | X / Square");
        AddControlRow(controlsContent.transform, "controls.dash", "Dash", "Space | A / Cross");
        AddControlRow(controlsContent.transform, "controls.special", "Special", "Q | Y / Triangle");
        AddControlRow(controlsContent.transform, "controls.interact", "Interact", "E");
        AddControlRow(controlsContent.transform, "controls.items", "Items", "1 / 2 / 3");
        AddControlRow(controlsContent.transform, "controls.pause", "Pause", "Esc");
    }

    private void AddControlRow(Transform parent, string actionKey, string actionFallback, string bindingText)
    {
        GameObject rowObject = CreateUiObject(actionFallback.Replace(" ", string.Empty) + "Row", parent);
        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 42f;

        HorizontalLayoutGroup rowGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 12f;
        rowGroup.padding = new RectOffset(14, 14, 8, 8);
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;

        Image rowImage = rowObject.AddComponent<Image>();
        rowImage.color = new Color(0.10f, 0.13f, 0.17f, 0.95f);

        TextMeshProUGUI actionText = CreateTmpText("Action", rowObject.transform, string.Empty, 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        LayoutElement actionLayout = actionText.gameObject.AddComponent<LayoutElement>();
        actionLayout.preferredWidth = 122f;
        actionLayout.flexibleWidth = 0f;

        TextMeshProUGUI bindingTextComponent = CreateTmpText("Binding", rowObject.transform, bindingText, 17f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.74f, 0.83f, 0.86f, 1f));
        LayoutElement bindingLayout = bindingTextComponent.gameObject.AddComponent<LayoutElement>();
        bindingLayout.flexibleWidth = 1f;

        controlRows.Add(new ControlRow
        {
            ActionKey = actionKey,
            ActionFallback = actionFallback,
            BindingText = bindingText,
            ActionText = actionText,
            BindingTextComponent = bindingTextComponent
        });
    }

    private Button CreateMenuButton(string objectName, Transform parent, string label, UnityEngine.Events.UnityAction action, bool primary)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = primary ? 78f : 68f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = primary
            ? new Color(0.92f, 0.75f, 0.30f, 1f)
            : new Color(0.16f, 0.20f, 0.24f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = primary
            ? new Color(0.97f, 0.82f, 0.38f, 1f)
            : new Color(0.22f, 0.27f, 0.31f, 1f);
        colors.pressedColor = primary
            ? new Color(0.78f, 0.64f, 0.24f, 1f)
            : new Color(0.12f, 0.16f, 0.19f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
        button.colors = colors;
        button.onClick.AddListener(action);

        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
        shadow.effectDistance = new Vector2(0f, -3f);

        TextMeshProUGUI buttonLabel = CreateTmpText(
            "Label",
            buttonObject.transform,
            label,
            primary ? 28f : 24f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            primary ? new Color(0.08f, 0.09f, 0.10f, 1f) : new Color(0.97f, 0.95f, 0.90f, 1f));

        RectTransform labelRect = buttonLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 10f);
        labelRect.offsetMax = new Vector2(-16f, -10f);

        return button;
    }

    private void RefreshTexts()
    {
        if (titleText != null)
        {
            titleText.text = L("pause.title", "Paused");
        }

        if (subtitleText != null)
        {
            subtitleText.text = L("pause.subtitle", "Take a breath, adjust your plan, and jump back in when you're ready.");
        }

        SetButtonLabel(resumeButton, L("pause.resume", "Resume"));
        SetButtonLabel(retryButton, L("pause.retry", "Retry Run"));
        SetButtonLabel(mainMenuButton, L("pause.main_menu", "Return to Main Menu"));
        SetButtonLabel(quitButton, L("pause.quit", "Quit Game"));

        if (controlsTitleText != null)
        {
            controlsTitleText.text = L("pause.controls.title", "Controls");
        }

        if (controlsSubtitleText != null)
        {
            controlsSubtitleText.text = L("pause.controls.subtitle", "Quick reference for the current build while you're paused.");
        }

        for (int i = 0; i < controlRows.Count; i++)
        {
            ControlRow row = controlRows[i];
            if (row.ActionText != null)
            {
                row.ActionText.text = L(row.ActionKey, row.ActionFallback);
            }

            if (row.BindingTextComponent != null)
            {
                row.BindingTextComponent.text = row.BindingText;
            }
        }
    }

    private void HideLegacyChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private string L(string key, string fallback, params object[] args)
    {
        return LocalizationManager.GetString(LocalizationManager.DefaultTable, key, fallback, args);
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = label;
        }
    }

    private TextMeshProUGUI CreateTmpText(string objectName, Transform parent, string message, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset;
        text.text = message;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        return text;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private void SetMenuVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private bool IsMenuVisible()
    {
        return canvasGroup != null && canvasGroup.alpha > 0.01f;
    }
}
