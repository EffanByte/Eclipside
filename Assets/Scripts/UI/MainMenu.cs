using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private GameObject ChallengePanel;
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private string gameplaySceneName = "Demo";

    private const string DefaultCharacterId = "D_Eryndor";

    private GameObject characterSelectPanel;
    private RectTransform characterListContent;
    private Text characterSelectTitleLabel;
    private Text selectedCharacterLabel;
    private Button characterSelectCloseButton;
    private Button startRunButton;
    private readonly Dictionary<string, Button> characterButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, Button> landingButtons = new Dictionary<string, Button>();
    private GameObject modernMenuRoot;
    private GameObject settingsPanel;
    private GameObject gachaPanel;
    private GameObject gachaRewardsPanel;
    private GameObject authOnboardingPanel;
    private RectTransform settingsLanguageListContent;
    private RectTransform gachaRewardsContent;
    private TextMeshProUGUI menuTitleText;
    private TextMeshProUGUI menuSubtitleText;
    private TextMeshProUGUI menuStatusText;
    private TextMeshProUGUI menuGoldAmountText;
    private TextMeshProUGUI menuOrbAmountText;
    private TextMeshProUGUI menuFooterText;
    private TextMeshProUGUI settingsTitleText;
    private TextMeshProUGUI settingsSubtitleText;
    private TextMeshProUGUI settingsCurrentLanguageText;
    private TextMeshProUGUI settingsDisplayModeText;
    private TextMeshProUGUI gachaTitleText;
    private TextMeshProUGUI gachaSubtitleText;
    private TextMeshProUGUI gachaGoldAmountText;
    private TextMeshProUGUI gachaOrbAmountText;
    private TextMeshProUGUI gachaRewardsTitleText;
    private TextMeshProUGUI gachaMeteorNameText;
    private TextMeshProUGUI gachaMeteorIndexText;
    private TextMeshProUGUI gachaMeteorCostText;
    private TextMeshProUGUI gachaMeteorRatesText;
    private TextMeshProUGUI gachaMeteorPlaceholderText;
    private TextMeshProUGUI gachaResultText;
    private Image menuGoldIconImage;
    private Image menuOrbIconImage;
    private Image gachaGoldIconImage;
    private Image gachaOrbIconImage;
    private Image gachaMeteorImage;
    private Image gachaMeteorPlaceholderImage;
    private TextMeshProUGUI authTitleText;
    private TextMeshProUGUI authSubtitleText;
    private TextMeshProUGUI authStatusText;
    private Button settingsCloseButton;
    private Button authSignInTabButton;
    private Button authSignUpTabButton;
    private Button authSubmitButton;
    private Button authGuestButton;
    private GameObject authDisplayNameRow;
    private TMP_InputField authEmailInput;
    private TMP_InputField authPasswordInput;
    private TMP_InputField authDisplayNameInput;
    private readonly Dictionary<string, Button> settingsLanguageButtons = new Dictionary<string, Button>();
    private Button settingsWindowedButton;
    private Button settingsFullscreenButton;
    private Button gachaCloseButton;
    private Button gachaRewardsCloseButton;
    private Button gachaPrevButton;
    private Button gachaNextButton;
    private Button gachaRewardsButton;
    private Button gachaSinglePullButton;
    private Button gachaTenPullButton;
    private TMP_FontAsset fallbackTmpFont;
    private Font fallbackFont;
    private string pendingCharacterSelectionId;
    private int selectedGachaBannerIndex;
    private bool authSignUpMode;
    private bool achievementPanelStyled;
    private bool challengePanelStyled;
    private bool missionPanelStyled;

    private void Start()
    {
        ApplySavedDisplayMode();
        EnsureMenuGachaManager();
        BuildModernLandingMenu();

        if (BackendRuntimeSettings.IsEnabled)
        {
            DevAccountAuthBootstrap.EnsureExists();
            EnsureAuthOnboardingPanel();
            RefreshAuthOnboardingPanel();
            TryShowAuthOnboarding();
        }
    }

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += RefreshLocalizedUi;
        GachaManager.PullSummaryUpdated += HandleGachaPullSummaryUpdated;

        if (BackendRuntimeSettings.IsEnabled)
        {
            DevAccountAuthBootstrap.EnsureExists();
            SubscribeAuthEvents();
        }
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedUi;
        GachaManager.PullSummaryUpdated -= HandleGachaPullSummaryUpdated;

        if (BackendRuntimeSettings.IsEnabled)
        {
            UnsubscribeAuthEvents();
        }
    }

    private void Update()
    {
        if (gachaPanel == null || !gachaPanel.activeSelf)
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.05f)
        {
            SelectPreviousMeteor();
        }
        else if (scroll < -0.05f)
        {
            SelectNextMeteor();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectPreviousMeteor();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectNextMeteor();
        }
    }

    public void OnStartGameButtonPressed()
    {
        EnsureCharacterSelectPanel();
        RefreshCharacterSelectPanel();

        if (characterSelectPanel != null)
        {
            characterSelectPanel.transform.SetAsLastSibling();
            characterSelectPanel.SetActive(true);
        }
    }

    public void OnAchievementButtonPressed()
    {
        BeautifyTrackedPanel(achievementPanel, GetAchievementsTitle(), ref achievementPanelStyled);
        achievementPanel.transform.SetAsLastSibling();
        achievementPanel.SetActive(true);
        ChallengePanel.SetActive(false);
    }

    public void OnChallengeButtonPressed()
    {
        BeautifyTrackedPanel(ChallengePanel, GetChallengesTitle(), ref challengePanelStyled);
        ChallengePanel.transform.SetAsLastSibling();
        ChallengePanel.SetActive(true);
        achievementPanel.SetActive(false);
    }

    public void OnExitAchievementPanel()
    {
        achievementPanel.SetActive(false);
    }

    public void OnExitChallengePanel()
    {
        ChallengePanel.SetActive(false);
    }

    public void OnGachaButtonPressed()
    {
        EnsureGachaPanel();
        RefreshGachaPanel();

        if (gachaPanel != null)
        {
            gachaPanel.transform.SetAsLastSibling();
            gachaPanel.SetActive(true);
        }
    }

    public void OnMissionButtonPressed()
    {
        BeautifyTrackedPanel(missionPanel, GetMissionsTitle(), ref missionPanelStyled);
        missionPanel.transform.SetAsLastSibling();
        missionPanel.SetActive(true);
    }

    public void OnSettingsButtonPressed()
    {
        EnsureSettingsPanel();
        RefreshSettingsPanel();

        if (settingsPanel != null)
        {
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
        }
    }

    public void onExitMissionPanel()
    {
        missionPanel.SetActive(false);
    }

    public void OnCloseCharacterSelectPressed()
    {
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(false);
        }
    }

    public void OnCloseSettingsPressed()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OnCloseGachaPressed()
    {
        if (gachaPanel != null)
        {
            gachaPanel.SetActive(false);
        }
    }

    public void OnAuthSignInTabPressed()
    {
        SetAuthMode(false);
    }

    public void OnAuthSignUpTabPressed()
    {
        SetAuthMode(true);
    }

    public void OnAuthSubmitPressed()
    {
        if (!BackendRuntimeSettings.IsEnabled)
        {
            SetAuthStatus("Backend auth is currently disabled.");
            return;
        }

        CacheAuthInputValues();

        string email = authEmailInput != null ? authEmailInput.text.Trim() : string.Empty;
        string password = authPasswordInput != null ? authPasswordInput.text : string.Empty;
        string displayName = authDisplayNameInput != null ? authDisplayNameInput.text.Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            SetAuthStatus(L("auth.error.missing_credentials", "Enter both email and password."));
            return;
        }

        if (DevAccountAuthManager.Instance == null)
        {
            SetAuthStatus(L("auth.error.manager_missing", "Authentication manager is unavailable."));
            return;
        }

        SetAuthStatus(authSignUpMode
            ? L("auth.status.signing_up", "Creating your account...")
            : L("auth.status.signing_in", "Signing you in..."));

        if (authSignUpMode)
        {
            DevAccountAuthManager.Instance.RegisterAndLinkCurrentGuest(email, password, displayName);
        }
        else
        {
            DevAccountAuthManager.Instance.LoginToExistingAccount(email, password);
        }
    }

    public void OnContinueAsGuestPressed()
    {
        SaveManager.Settings.general.has_completed_auth_onboarding = true;
        SaveManager.SaveSettings();
        HideAuthOnboarding();
    }

    public void OnStartRunFromCharacterSelectPressed()
    {
        if (string.IsNullOrWhiteSpace(pendingCharacterSelectionId))
        {
            Debug.LogWarning("[MainMenu] Start Run blocked because no character is selected.");
            return;
        }

        SaveFile_Profile profile = SaveManager.Profile;
        EnsureDefaultCharacterUnlocked(profile);
        profile.characters.equipped_character_id = pendingCharacterSelectionId;
        SaveManager.SaveProfile();

        RunSceneTransitionState.BeginNewRun();
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void EnsureCharacterSelectPanel()
    {
        if (characterSelectPanel != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not create character select menu because no Canvas was found.");
            return;
        }

        fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        characterSelectPanel = CreateUIObject("CharacterSelectPanel", canvas.transform);
        Image overlayImage = characterSelectPanel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform overlayRect = characterSelectPanel.GetComponent<RectTransform>();
        StretchToParent(overlayRect);
        characterSelectPanel.SetActive(false);

        GameObject card = CreateUIObject("CharacterSelectCard", characterSelectPanel.transform);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 620f);
        cardRect.anchoredPosition = Vector2.zero;

        characterSelectTitleLabel = CreateText("Title", card.transform, L("menu.character_select.title", "Select Character"), 28, TextAnchor.MiddleCenter);
        RectTransform titleRect = characterSelectTitleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -68f);
        titleRect.offsetMax = new Vector2(-24f, -16f);

        selectedCharacterLabel = CreateText("SelectedLabel", card.transform, L("menu.character_select.none", "Selected: None"), 18, TextAnchor.MiddleLeft);
        RectTransform selectedRect = selectedCharacterLabel.rectTransform;
        selectedRect.anchorMin = new Vector2(0f, 1f);
        selectedRect.anchorMax = new Vector2(1f, 1f);
        selectedRect.pivot = new Vector2(0.5f, 1f);
        selectedRect.offsetMin = new Vector2(32f, -108f);
        selectedRect.offsetMax = new Vector2(-32f, -72f);

        GameObject viewport = CreateUIObject("CharacterViewport", card.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.08f, 0.11f, 1f);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = true;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(32f, 92f);
        viewportRect.offsetMax = new Vector2(-32f, -124f);

        GameObject content = CreateUIObject("CharacterListContent", viewport.transform);
        characterListContent = content.GetComponent<RectTransform>();
        characterListContent.anchorMin = new Vector2(0f, 1f);
        characterListContent.anchorMax = new Vector2(1f, 1f);
        characterListContent.pivot = new Vector2(0.5f, 1f);
        characterListContent.anchoredPosition = Vector2.zero;
        characterListContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = characterListContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        characterSelectCloseButton = CreateButton("CloseButton", card.transform, L("common.back", "Back"));
        RectTransform closeRect = characterSelectCloseButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0f, 0f);
        closeRect.anchorMax = new Vector2(0f, 0f);
        closeRect.pivot = new Vector2(0f, 0f);
        closeRect.sizeDelta = new Vector2(180f, 48f);
        closeRect.anchoredPosition = new Vector2(32f, 24f);
        characterSelectCloseButton.onClick.AddListener(OnCloseCharacterSelectPressed);

        startRunButton = CreateButton("StartRunButton", card.transform, L("menu.start_run", "Start Run"));
        RectTransform startRect = startRunButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(1f, 0f);
        startRect.anchorMax = new Vector2(1f, 0f);
        startRect.pivot = new Vector2(1f, 0f);
        startRect.sizeDelta = new Vector2(220f, 48f);
        startRect.anchoredPosition = new Vector2(-32f, 24f);
        startRunButton.onClick.AddListener(OnStartRunFromCharacterSelectPressed);
    }

    private void BuildModernLandingMenu()
    {
        if (modernMenuRoot != null)
        {
            RefreshModernMenuContent();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not build the new main menu because no Canvas was found.");
            return;
        }

        fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fallbackTmpFont = TMP_Settings.defaultFontAsset;

        HideLegacyLandingButtons(canvas.transform);

        modernMenuRoot = CreateUIObject("ModernMenuRoot", canvas.transform);
        RectTransform rootRect = modernMenuRoot.GetComponent<RectTransform>();
        StretchToParent(rootRect);
        modernMenuRoot.transform.SetAsLastSibling();

        Image background = modernMenuRoot.AddComponent<Image>();
        background.color = new Color(0.04f, 0.07f, 0.09f, 1f);

        CreateDecorativeGlow("GlowTopLeft", modernMenuRoot.transform, new Color(0.16f, 0.46f, 0.44f, 0.22f), new Vector2(340f, 340f), new Vector2(140f, -90f), new Vector2(0f, 1f));
        CreateDecorativeGlow("GlowRight", modernMenuRoot.transform, new Color(0.82f, 0.63f, 0.22f, 0.16f), new Vector2(420f, 420f), new Vector2(-120f, 40f), new Vector2(1f, 0.5f));
        CreateDecorativeGlow("GlowBottom", modernMenuRoot.transform, new Color(0.32f, 0.24f, 0.12f, 0.18f), new Vector2(520f, 220f), new Vector2(0f, 80f), new Vector2(0.5f, 0f));

        GameObject frame = CreateUIObject("Frame", modernMenuRoot.transform);
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.06f, 0.10f, 0.12f, 0.82f);
        Outline frameOutline = frame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.90f, 0.77f, 0.36f, 0.16f);
        frameOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 0f);
        frameRect.anchorMax = new Vector2(1f, 1f);
        frameRect.offsetMin = new Vector2(28f, 28f);
        frameRect.offsetMax = new Vector2(-28f, -28f);

        GameObject leftColumn = CreateUIObject("LeftColumn", frame.transform);
        RectTransform leftRect = leftColumn.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.58f, 1f);
        leftRect.offsetMin = new Vector2(48f, 48f);
        leftRect.offsetMax = new Vector2(-24f, -48f);

        menuTitleText = CreateTmpText("Title", leftColumn.transform, string.Empty, 88f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform titleRect = menuTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 190f);
        titleRect.anchoredPosition = Vector2.zero;

        menuSubtitleText = CreateTmpText("Subtitle", leftColumn.transform, string.Empty, 28f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.73f, 0.83f, 0.83f, 1f));
        RectTransform subtitleRect = menuSubtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(0f, 160f);
        subtitleRect.anchoredPosition = new Vector2(0f, -160f);

        GameObject accentLine = CreateUIObject("AccentLine", leftColumn.transform);
        Image accentImage = accentLine.AddComponent<Image>();
        accentImage.color = new Color(0.93f, 0.77f, 0.34f, 1f);
        RectTransform accentRect = accentLine.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 1f);
        accentRect.sizeDelta = new Vector2(170f, 6f);
        accentRect.anchoredPosition = new Vector2(4f, -288f);

        GameObject statusPanel = CreateUIObject("StatusPanel", leftColumn.transform);
        RectTransform statusPanelRect = statusPanel.GetComponent<RectTransform>();
        statusPanelRect.anchorMin = new Vector2(0f, 0f);
        statusPanelRect.anchorMax = new Vector2(1f, 0f);
        statusPanelRect.pivot = new Vector2(0f, 0f);
        statusPanelRect.sizeDelta = new Vector2(0f, 116f);
        statusPanelRect.anchoredPosition = new Vector2(0f, 62f);

        VerticalLayoutGroup statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(0, 0, 0, 0);
        statusLayout.spacing = 10f;
        statusLayout.childControlHeight = true;
        statusLayout.childControlWidth = true;
        statusLayout.childForceExpandHeight = false;
        statusLayout.childForceExpandWidth = false;

        menuStatusText = CreateTmpText("EquippedStatus", statusPanel.transform, string.Empty, 24f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color(0.87f, 0.88f, 0.90f, 0.96f));
        menuStatusText.enableWordWrapping = false;
        LayoutElement equippedLayout = menuStatusText.gameObject.AddComponent<LayoutElement>();
        equippedLayout.preferredHeight = 34f;

        GameObject currencyRow = CreateUIObject("CurrencyRow", statusPanel.transform);
        HorizontalLayoutGroup currencyRowLayout = currencyRow.AddComponent<HorizontalLayoutGroup>();
        currencyRowLayout.spacing = 18f;
        currencyRowLayout.childAlignment = TextAnchor.MiddleLeft;
        currencyRowLayout.childControlHeight = true;
        currencyRowLayout.childControlWidth = false;
        currencyRowLayout.childForceExpandHeight = false;
        currencyRowLayout.childForceExpandWidth = false;
        LayoutElement currencyRowSize = currencyRow.AddComponent<LayoutElement>();
        currencyRowSize.preferredHeight = 42f;

        GameObject menuGoldBadge = CreateCurrencyBadge("GoldStatusBadge", currencyRow.transform, 22f, new Color(0.94f, 0.86f, 0.55f, 1f), out menuGoldIconImage, out menuGoldAmountText);
        GameObject menuOrbBadge = CreateCurrencyBadge("OrbStatusBadge", currencyRow.transform, 22f, new Color(0.94f, 0.86f, 0.55f, 1f), out menuOrbIconImage, out menuOrbAmountText);
        SetCurrencyBadgeWidth(menuGoldBadge, 120f);
        SetCurrencyBadgeWidth(menuOrbBadge, 104f);

        GameObject rightColumn = CreateUIObject("RightColumn", frame.transform);
        Image rightImage = rightColumn.AddComponent<Image>();
        rightImage.color = new Color(0.10f, 0.13f, 0.16f, 0.94f);
        Outline rightOutline = rightColumn.AddComponent<Outline>();
        rightOutline.effectColor = new Color(1f, 1f, 1f, 0.05f);
        rightOutline.effectDistance = new Vector2(1f, -1f);
        RectTransform rightRect = rightColumn.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.61f, 0.5f);
        rightRect.anchorMax = new Vector2(0.94f, 0.5f);
        rightRect.pivot = new Vector2(0.5f, 0.5f);
        rightRect.sizeDelta = new Vector2(0f, 480f);

        VerticalLayoutGroup rightLayout = rightColumn.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(28, 28, 28, 28);
        rightLayout.spacing = 14f;
        rightLayout.childControlHeight = true;
        rightLayout.childControlWidth = true;
        rightLayout.childForceExpandHeight = false;
        rightLayout.childForceExpandWidth = true;

        GameObject rightHeader = CreateUIObject("MenuHeader", rightColumn.transform);
        LayoutElement headerLayout = rightHeader.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 72f;
        TextMeshProUGUI headerText = CreateTmpText("Label", rightHeader.transform, L("menu.section.play", "Choose Your Path"), 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        StretchToParent(headerText.rectTransform, 0f, 0f);

        CreateLandingButton(rightColumn.transform, "StartButton", L("menu.start_run", "Start Run"), L("menu.start_run.subtitle", "Select your character and enter the next run."), OnStartGameButtonPressed, true);
        CreateLandingButton(rightColumn.transform, "GachaButton", L("menu.gacha", "Gacha"), L("menu.gacha.subtitle", "Spend meteorites and expand your roster."), OnGachaButtonPressed, false);
        CreateLandingButton(rightColumn.transform, "MissionsButton", GetMissionsTitle(), L("menu.missions.subtitle", "Track daily goals and collect rewards."), OnMissionButtonPressed, false);
        CreateLandingButton(rightColumn.transform, "AchievementsButton", GetAchievementsTitle(), L("menu.achievements.subtitle", "Review long-term progress and milestones."), OnAchievementButtonPressed, false);
        CreateLandingButton(rightColumn.transform, "ChallengesButton", GetChallengesTitle(), L("menu.challenges.subtitle", "Enable run modifiers and earn bragging rights."), OnChallengeButtonPressed, false);
        CreateLandingButton(rightColumn.transform, "SettingsButton", L("menu.settings", "Settings"), L("menu.settings.subtitle", "Adjust language, display, and menu preferences."), OnSettingsButtonPressed, false);

        GameObject footer = CreateUIObject("Footer", frame.transform);
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.sizeDelta = new Vector2(0f, 34f);
        footerRect.anchoredPosition = new Vector2(0f, 22f);
        menuFooterText = CreateTmpText("FooterLabel", footer.transform, string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.70f, 0.75f, 0.78f, 0.95f));
        StretchToParent(menuFooterText.rectTransform, 0f, 0f);

        RefreshModernMenuContent();
    }

    private void HideLegacyLandingButtons(Transform canvasTransform)
    {
        if (canvasTransform == null)
        {
            return;
        }

        Transform buttonsPanel = canvasTransform.Find("ButtonsPanel");
        if (buttonsPanel != null)
        {
            buttonsPanel.gameObject.SetActive(false);
        }
    }

    private void RefreshModernMenuContent()
    {
        if (modernMenuRoot == null)
        {
            return;
        }

        if (menuTitleText != null)
        {
            menuTitleText.text = L("menu.title", "ECLIPSIDE");
        }

        if (menuSubtitleText != null)
        {
            menuSubtitleText.text = L("menu.subtitle", "Descend through fractured biomes, shape your loadout, and survive the eclipse.");
        }

        if (menuStatusText != null)
        {
            SaveFile_Profile profile = SaveManager.Profile;
            string equippedCharacterName = DefaultCharacterId;
            CharacterData equippedCharacter = GameDatabase.Instance.GetCharacterByID(profile.characters.equipped_character_id);
            if (equippedCharacter != null)
            {
                equippedCharacterName = equippedCharacter.characterName;
            }

            menuStatusText.text = L("menu.status_equipped", "Equipped: {0}", equippedCharacterName);
            RefreshCurrencyBadge(menuGoldIconImage, menuGoldAmountText, CurrencyType.Gold, profile.user_profile.gold);
            RefreshCurrencyBadge(menuOrbIconImage, menuOrbAmountText, CurrencyType.Orb, profile.user_profile.orbs);
        }

        if (menuFooterText != null)
        {
            menuFooterText.text = L("menu.footer", "A bold run begins with a deliberate choice.");
        }

        RefreshLandingButtonLabels();
    }

    private void RefreshLandingButtonLabels()
    {
        SetLandingButtonTexts("StartButton", L("menu.start_run", "Start Run"), L("menu.start_run.subtitle", "Select your character and enter the next run."));
        SetLandingButtonTexts("GachaButton", L("menu.gacha", "Gacha"), L("menu.gacha.subtitle", "Spend meteorites and expand your roster."));
        SetLandingButtonTexts("MissionsButton", GetMissionsTitle(), L("menu.missions.subtitle", "Track daily goals and collect rewards."));
        SetLandingButtonTexts("AchievementsButton", GetAchievementsTitle(), L("menu.achievements.subtitle", "Review long-term progress and milestones."));
        SetLandingButtonTexts("ChallengesButton", GetChallengesTitle(), L("menu.challenges.subtitle", "Enable run modifiers and earn bragging rights."));
        SetLandingButtonTexts("SettingsButton", L("menu.settings", "Settings"), L("menu.settings.subtitle", "Adjust language, display, and menu preferences."));
    }

    private void ApplySavedDisplayMode()
    {
        ApplyDisplayMode(SaveManager.Settings.general.windowed_mode, false);
    }

    private void ApplyDisplayMode(bool windowedMode, bool persist)
    {
        Resolution currentResolution = Screen.currentResolution;
        int width = Mathf.Max(1280, Screen.width > 0 ? Screen.width : currentResolution.width);
        int height = Mathf.Max(720, Screen.height > 0 ? Screen.height : currentResolution.height);

        if (windowedMode)
        {
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
        else
        {
            Screen.SetResolution(currentResolution.width, currentResolution.height, FullScreenMode.FullScreenWindow);
        }

        if (persist)
        {
            SaveManager.Settings.general.windowed_mode = windowedMode;
            SaveManager.SaveSettings();
        }

        RefreshSettingsPanel();
    }

    private void OnWindowedModePressed()
    {
        ApplyDisplayMode(true, true);
    }

    private void OnFullscreenModePressed()
    {
        ApplyDisplayMode(false, true);
    }

    private void EnsureMenuGachaManager()
    {
        if (GachaManager.Instance != null)
        {
            return;
        }

        GameObject gachaManagerObject = new GameObject("MenuGachaManager");
        gachaManagerObject.AddComponent<GachaManager>();
    }

    private void HandleGachaPullSummaryUpdated(string summary)
    {
        if (gachaResultText != null)
        {
            gachaResultText.text = string.IsNullOrWhiteSpace(summary)
                ? L("menu.gacha.result_default", "Choose a meteorite banner to view the pool and spend currency.")
                : summary;
        }

        RefreshModernMenuContent();
        RefreshGachaPanel();
    }

    private void EnsureSettingsPanel()
    {
        if (settingsPanel != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not create settings panel because no Canvas was found.");
            return;
        }

        settingsPanel = CreateUIObject("SettingsPanel", canvas.transform);
        Image overlay = settingsPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.84f);
        RectTransform overlayRect = settingsPanel.GetComponent<RectTransform>();
        StretchToParent(overlayRect);
        settingsPanel.SetActive(false);

        GameObject card = CreateUIObject("SettingsCard", settingsPanel.transform);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.10f, 0.12f, 0.15f, 0.98f);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.90f, 0.77f, 0.34f, 0.22f);
        cardOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 620f);
        cardRect.anchoredPosition = Vector2.zero;

        settingsTitleText = CreateTmpText("Title", card.transform, string.Empty, 34f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform titleRect = settingsTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(-64f, 52f);
        titleRect.anchoredPosition = new Vector2(32f, -28f);

        settingsSubtitleText = CreateTmpText("Subtitle", card.transform, string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.73f, 0.82f, 0.84f, 1f));
        RectTransform subtitleRect = settingsSubtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(-64f, 78f);
        subtitleRect.anchoredPosition = new Vector2(32f, -88f);

        GameObject divider = CreateUIObject("Divider", card.transform);
        Image dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(0.90f, 0.77f, 0.34f, 0.9f);
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(0f, 1f);
        dividerRect.pivot = new Vector2(0f, 1f);
        dividerRect.sizeDelta = new Vector2(150f, 4f);
        dividerRect.anchoredPosition = new Vector2(32f, -158f);

        settingsCurrentLanguageText = CreateTmpText("CurrentLanguage", card.transform, string.Empty, 22f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.75f, 0.28f, 1f));
        RectTransform currentRect = settingsCurrentLanguageText.rectTransform;
        currentRect.anchorMin = new Vector2(0f, 1f);
        currentRect.anchorMax = new Vector2(1f, 1f);
        currentRect.pivot = new Vector2(0f, 1f);
        currentRect.sizeDelta = new Vector2(-64f, 34f);
        currentRect.anchoredPosition = new Vector2(32f, -188f);

        GameObject languageArea = CreateUIObject("LanguageArea", card.transform);
        Image languageAreaImage = languageArea.AddComponent<Image>();
        languageAreaImage.color = new Color(0.13f, 0.16f, 0.19f, 0.98f);
        RectTransform languageAreaRect = languageArea.GetComponent<RectTransform>();
        languageAreaRect.anchorMin = new Vector2(0f, 0f);
        languageAreaRect.anchorMax = new Vector2(1f, 1f);
        languageAreaRect.offsetMin = new Vector2(32f, 176f);
        languageAreaRect.offsetMax = new Vector2(-32f, -234f);

        GameObject languageHeader = CreateUIObject("LanguageHeader", languageArea.transform);
        RectTransform languageHeaderRect = languageHeader.GetComponent<RectTransform>();
        languageHeaderRect.anchorMin = new Vector2(0f, 1f);
        languageHeaderRect.anchorMax = new Vector2(1f, 1f);
        languageHeaderRect.pivot = new Vector2(0f, 1f);
        languageHeaderRect.sizeDelta = new Vector2(-40f, 36f);
        languageHeaderRect.anchoredPosition = new Vector2(20f, -18f);
        TextMeshProUGUI languageHeaderText = CreateTmpText("Label", languageHeader.transform, L("settings.language", "Language"), 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.96f, 0.95f, 0.90f, 1f));
        StretchToParent(languageHeaderText.rectTransform, 0f, 0f);

        GameObject languageList = CreateUIObject("LanguageList", languageArea.transform);
        settingsLanguageListContent = languageList.GetComponent<RectTransform>();
        settingsLanguageListContent.anchorMin = new Vector2(0f, 0f);
        settingsLanguageListContent.anchorMax = new Vector2(1f, 1f);
        settingsLanguageListContent.offsetMin = new Vector2(18f, 18f);
        settingsLanguageListContent.offsetMax = new Vector2(-18f, -64f);

        VerticalLayoutGroup listLayout = languageList.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 10f;
        listLayout.padding = new RectOffset(0, 0, 34, 0);
        listLayout.childControlHeight = true;
        listLayout.childControlWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.childForceExpandWidth = true;

        ContentSizeFitter listFitter = languageList.AddComponent<ContentSizeFitter>();
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        IReadOnlyList<string> supportedCodes = LocalizationManager.GetSupportedLanguageCodes();
        for (int i = 0; i < supportedCodes.Count; i++)
        {
            string code = supportedCodes[i];
            Button button = CreateButton(code + "_LanguageButton", settingsLanguageListContent, LocalizationManager.GetDisplayNameForCode(code));
            LayoutElement buttonLayout = button.gameObject.AddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 52f;

            string capturedCode = code;
            button.onClick.AddListener(() => OnLanguageOptionPressed(capturedCode));
            settingsLanguageButtons[capturedCode] = button;
        }

        settingsDisplayModeText = CreateTmpText("DisplayModeLabel", card.transform, string.Empty, 22f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.75f, 0.28f, 1f));
        RectTransform displayModeRect = settingsDisplayModeText.rectTransform;
        displayModeRect.anchorMin = new Vector2(0f, 0f);
        displayModeRect.anchorMax = new Vector2(1f, 0f);
        displayModeRect.pivot = new Vector2(0f, 0f);
        displayModeRect.sizeDelta = new Vector2(-64f, 32f);
        displayModeRect.anchoredPosition = new Vector2(32f, 130f);

        settingsWindowedButton = CreateButton("WindowedModeButton", card.transform, L("settings.display.windowed", "Windowed"));
        RectTransform windowedRect = settingsWindowedButton.GetComponent<RectTransform>();
        windowedRect.anchorMin = new Vector2(0f, 0f);
        windowedRect.anchorMax = new Vector2(0f, 0f);
        windowedRect.pivot = new Vector2(0f, 0f);
        windowedRect.sizeDelta = new Vector2(180f, 48f);
        windowedRect.anchoredPosition = new Vector2(32f, 74f);
        settingsWindowedButton.onClick.AddListener(OnWindowedModePressed);

        settingsFullscreenButton = CreateButton("FullscreenModeButton", card.transform, L("settings.display.fullscreen", "Fullscreen"));
        RectTransform fullscreenRect = settingsFullscreenButton.GetComponent<RectTransform>();
        fullscreenRect.anchorMin = new Vector2(0f, 0f);
        fullscreenRect.anchorMax = new Vector2(0f, 0f);
        fullscreenRect.pivot = new Vector2(0f, 0f);
        fullscreenRect.sizeDelta = new Vector2(180f, 48f);
        fullscreenRect.anchoredPosition = new Vector2(226f, 74f);
        settingsFullscreenButton.onClick.AddListener(OnFullscreenModePressed);

        settingsCloseButton = CreateButton("SettingsCloseButton", card.transform, L("common.close", "Close"));
        RectTransform closeRect = settingsCloseButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(180f, 48f);
        closeRect.anchoredPosition = new Vector2(-32f, 24f);
        settingsCloseButton.onClick.AddListener(OnCloseSettingsPressed);
    }

    private void OnLanguageOptionPressed(string languageCode)
    {
        LocalizationManager.SetLanguage(languageCode);
        RefreshSettingsPanel();
    }

    private void RefreshSettingsPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (settingsTitleText != null)
        {
            settingsTitleText.text = L("settings.title", "Settings");
        }

        if (settingsSubtitleText != null)
        {
            settingsSubtitleText.text = L("settings.subtitle", "Adjust how the game presents itself before you begin the next run.");
        }

        if (settingsCurrentLanguageText != null)
        {
            string currentCode = LocalizationManager.GetCurrentLanguageCode();
            settingsCurrentLanguageText.text = L(
                "settings.language.current_format",
                "Current Language: {0}",
                LocalizationManager.GetDisplayNameForCode(currentCode));
        }

        if (settingsDisplayModeText != null)
        {
            settingsDisplayModeText.text = L(
                "settings.display.current_format",
                "Display Mode: {0}",
                SaveManager.Settings.general.windowed_mode
                    ? L("settings.display.windowed", "Windowed")
                    : L("settings.display.fullscreen", "Fullscreen"));
        }

        SetButtonLabel(settingsCloseButton, L("common.close", "Close"));
        SetButtonLabel(settingsWindowedButton, L("settings.display.windowed", "Windowed"));
        SetButtonLabel(settingsFullscreenButton, L("settings.display.fullscreen", "Fullscreen"));

        string selectedCode = LocalizationManager.GetCurrentLanguageCode();
        foreach (KeyValuePair<string, Button> entry in settingsLanguageButtons)
        {
            if (entry.Value == null)
            {
                continue;
            }

            SetButtonLabel(entry.Value, LocalizationManager.GetDisplayNameForCode(entry.Key));

            Image image = entry.Value.GetComponent<Image>();
            if (image != null)
            {
                bool isSelected = entry.Key == selectedCode;
                image.color = isSelected
                    ? new Color(0.92f, 0.75f, 0.28f, 1f)
                    : new Color(1f, 1f, 1f, 0.95f);
            }
        }

        RefreshDisplayModeButtonState(settingsWindowedButton, SaveManager.Settings.general.windowed_mode);
        RefreshDisplayModeButtonState(settingsFullscreenButton, !SaveManager.Settings.general.windowed_mode);
    }

    private void RefreshDisplayModeButtonState(Button button, bool isSelected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isSelected
                ? new Color(0.92f, 0.75f, 0.28f, 1f)
                : new Color(1f, 1f, 1f, 0.95f);
        }
    }

    private void EnsureGachaPanel()
    {
        if (gachaPanel != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not create gacha panel because no Canvas was found.");
            return;
        }

        gachaPanel = CreateUIObject("GachaPanel", canvas.transform);
        Image overlay = gachaPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.86f);
        StretchToParent(gachaPanel.GetComponent<RectTransform>());
        gachaPanel.SetActive(false);

        GameObject card = CreateUIObject("GachaCard", gachaPanel.transform);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.08f, 0.10f, 0.13f, 0.985f);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.90f, 0.77f, 0.34f, 0.16f);
        cardOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1120f, 720f);
        cardRect.anchoredPosition = Vector2.zero;

        CreateDecorativeGlow("GachaGlowRight", card.transform, new Color(0.86f, 0.64f, 0.18f, 0.05f), new Vector2(170f, 230f), new Vector2(-18f, -12f), new Vector2(1f, 0.5f));

        gachaTitleText = CreateTmpText("Title", card.transform, string.Empty, 34f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform titleRect = gachaTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(-64f, 52f);
        titleRect.anchoredPosition = new Vector2(32f, -28f);

        gachaSubtitleText = CreateTmpText("Subtitle", card.transform, string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.73f, 0.82f, 0.84f, 1f));
        RectTransform subtitleRect = gachaSubtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(520f, 78f);
        subtitleRect.anchoredPosition = new Vector2(32f, -88f);

        GameObject walletPanel = CreateUIObject("WalletPanel", card.transform);
        Image walletPanelImage = walletPanel.AddComponent<Image>();
        walletPanelImage.color = new Color(0.11f, 0.14f, 0.18f, 0.96f);
        Outline walletPanelOutline = walletPanel.AddComponent<Outline>();
        walletPanelOutline.effectColor = new Color(0.92f, 0.75f, 0.28f, 0.14f);
        walletPanelOutline.effectDistance = new Vector2(1f, -1f);
        RectTransform walletPanelRect = walletPanel.GetComponent<RectTransform>();
        walletPanelRect.anchorMin = new Vector2(1f, 1f);
        walletPanelRect.anchorMax = new Vector2(1f, 1f);
        walletPanelRect.pivot = new Vector2(1f, 1f);
        walletPanelRect.sizeDelta = new Vector2(320f, 48f);
        walletPanelRect.anchoredPosition = new Vector2(-34f, -28f);

        HorizontalLayoutGroup walletLayout = walletPanel.AddComponent<HorizontalLayoutGroup>();
        walletLayout.padding = new RectOffset(14, 14, 8, 8);
        walletLayout.spacing = 16f;
        walletLayout.childAlignment = TextAnchor.MiddleLeft;
        walletLayout.childControlHeight = true;
        walletLayout.childControlWidth = false;
        walletLayout.childForceExpandHeight = false;
        walletLayout.childForceExpandWidth = false;

        GameObject gachaGoldBadge = CreateCurrencyBadge("GoldWalletBadge", walletPanel.transform, 21f, new Color(0.94f, 0.86f, 0.55f, 1f), out gachaGoldIconImage, out gachaGoldAmountText);
        GameObject gachaOrbBadge = CreateCurrencyBadge("OrbWalletBadge", walletPanel.transform, 21f, new Color(0.94f, 0.86f, 0.55f, 1f), out gachaOrbIconImage, out gachaOrbAmountText);
        SetCurrencyBadgeWidth(gachaGoldBadge, 132f);
        SetCurrencyBadgeWidth(gachaOrbBadge, 92f);

        GameObject indexPanel = CreateUIObject("MeteorIndexPanel", card.transform);
        Image indexPanelImage = indexPanel.AddComponent<Image>();
        indexPanelImage.color = new Color(0.11f, 0.14f, 0.18f, 0.96f);
        Outline indexPanelOutline = indexPanel.AddComponent<Outline>();
        indexPanelOutline.effectColor = new Color(0.92f, 0.75f, 0.28f, 0.16f);
        indexPanelOutline.effectDistance = new Vector2(1f, -1f);
        RectTransform indexPanelRect = indexPanel.GetComponent<RectTransform>();
        indexPanelRect.anchorMin = new Vector2(1f, 1f);
        indexPanelRect.anchorMax = new Vector2(1f, 1f);
        indexPanelRect.pivot = new Vector2(1f, 1f);
        indexPanelRect.sizeDelta = new Vector2(84f, 40f);
        indexPanelRect.anchoredPosition = new Vector2(-32f, -92f);

        gachaMeteorIndexText = CreateTmpText("MeteorIndex", indexPanel.transform, string.Empty, 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.92f, 0.75f, 0.28f, 1f));
        StretchToParent(gachaMeteorIndexText.rectTransform, 8f, 8f);
        indexPanel.SetActive(false);

        gachaPrevButton = CreateButton("PrevMeteorButton", card.transform, "<");
        RectTransform prevRect = gachaPrevButton.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0.5f, 0.5f);
        prevRect.anchorMax = new Vector2(0.5f, 0.5f);
        prevRect.pivot = new Vector2(0.5f, 0.5f);
        prevRect.sizeDelta = new Vector2(58f, 58f);
        prevRect.anchoredPosition = new Vector2(-435f, -8f);
        gachaPrevButton.onClick.AddListener(SelectPreviousMeteor);

        gachaNextButton = CreateButton("NextMeteorButton", card.transform, ">");
        RectTransform nextRect = gachaNextButton.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.5f, 0.5f);
        nextRect.anchorMax = new Vector2(0.5f, 0.5f);
        nextRect.pivot = new Vector2(0.5f, 0.5f);
        nextRect.sizeDelta = new Vector2(58f, 58f);
        nextRect.anchoredPosition = new Vector2(-65f, -8f);
        gachaNextButton.onClick.AddListener(SelectNextMeteor);

        GameObject meteorFrame = CreateUIObject("MeteorFrame", card.transform);
        Image meteorFrameImage = meteorFrame.AddComponent<Image>();
        meteorFrameImage.color = new Color(0.09f, 0.13f, 0.17f, 0.98f);
        Outline meteorFrameOutline = meteorFrame.AddComponent<Outline>();
        meteorFrameOutline.effectColor = new Color(0.92f, 0.75f, 0.28f, 0.14f);
        meteorFrameOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform meteorFrameRect = meteorFrame.GetComponent<RectTransform>();
        meteorFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
        meteorFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
        meteorFrameRect.pivot = new Vector2(0.5f, 0.5f);
        meteorFrameRect.sizeDelta = new Vector2(300f, 380f);
        meteorFrameRect.anchoredPosition = new Vector2(-250f, -8f);

        GameObject meteorImagePanel = CreateUIObject("MeteorImagePanel", meteorFrame.transform);
        gachaMeteorPlaceholderImage = meteorImagePanel.AddComponent<Image>();
        Outline meteorImageOutline = meteorImagePanel.AddComponent<Outline>();
        meteorImageOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        meteorImageOutline.effectDistance = new Vector2(1f, -1f);
        RectTransform meteorImageRect = meteorImagePanel.GetComponent<RectTransform>();
        meteorImageRect.anchorMin = new Vector2(0.5f, 1f);
        meteorImageRect.anchorMax = new Vector2(0.5f, 1f);
        meteorImageRect.pivot = new Vector2(0.5f, 1f);
        meteorImageRect.sizeDelta = new Vector2(200f, 190f);
        meteorImageRect.anchoredPosition = new Vector2(0f, -22f);

        GameObject meteorSpriteObject = CreateUIObject("MeteorSprite", meteorImagePanel.transform);
        gachaMeteorImage = meteorSpriteObject.AddComponent<Image>();
        RectTransform meteorSpriteRect = gachaMeteorImage.rectTransform;
        meteorSpriteRect.anchorMin = new Vector2(0.5f, 0.5f);
        meteorSpriteRect.anchorMax = new Vector2(0.5f, 0.5f);
        meteorSpriteRect.pivot = new Vector2(0.5f, 0.5f);
        meteorSpriteRect.sizeDelta = new Vector2(168f, 150f);
        meteorSpriteRect.anchoredPosition = Vector2.zero;
        gachaMeteorImage.preserveAspect = true;

        gachaMeteorPlaceholderText = CreateTmpText("MeteorPlaceholder", meteorImagePanel.transform, string.Empty, 58f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.96f, 0.96f, 0.98f, 0.96f));
        StretchToParent(gachaMeteorPlaceholderText.rectTransform, 0f, 0f);

        GameObject infoBand = CreateUIObject("MeteorInfoBand", meteorFrame.transform);
        Image infoBandImage = infoBand.AddComponent<Image>();
        infoBandImage.color = new Color(0.08f, 0.10f, 0.13f, 0.92f);
        RectTransform infoBandRect = infoBand.GetComponent<RectTransform>();
        infoBandRect.anchorMin = new Vector2(0f, 0f);
        infoBandRect.anchorMax = new Vector2(1f, 0f);
        infoBandRect.pivot = new Vector2(0.5f, 0f);
        infoBandRect.offsetMin = new Vector2(0f, 0f);
        infoBandRect.offsetMax = new Vector2(0f, 116f);

        GameObject infoDivider = CreateUIObject("MeteorInfoDivider", meteorFrame.transform);
        Image infoDividerImage = infoDivider.AddComponent<Image>();
        infoDividerImage.color = new Color(1f, 1f, 1f, 0.08f);
        RectTransform infoDividerRect = infoDivider.GetComponent<RectTransform>();
        infoDividerRect.anchorMin = new Vector2(0f, 0f);
        infoDividerRect.anchorMax = new Vector2(1f, 0f);
        infoDividerRect.pivot = new Vector2(0.5f, 0f);
        infoDividerRect.offsetMin = new Vector2(18f, 116f);
        infoDividerRect.offsetMax = new Vector2(-18f, 118f);

        gachaMeteorNameText = CreateTmpText("MeteorName", meteorFrame.transform, string.Empty, 32f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform meteorNameRect = gachaMeteorNameText.rectTransform;
        meteorNameRect.anchorMin = new Vector2(0f, 0f);
        meteorNameRect.anchorMax = new Vector2(1f, 0f);
        meteorNameRect.pivot = new Vector2(0.5f, 0f);
        meteorNameRect.sizeDelta = new Vector2(-24f, 72f);
        meteorNameRect.anchoredPosition = new Vector2(0f, 50f);

        gachaMeteorCostText = CreateTmpText("MeteorCost", meteorFrame.transform, string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.92f, 0.75f, 0.28f, 1f));
        RectTransform meteorCostRect = gachaMeteorCostText.rectTransform;
        meteorCostRect.anchorMin = new Vector2(0f, 0f);
        meteorCostRect.anchorMax = new Vector2(1f, 0f);
        meteorCostRect.pivot = new Vector2(0.5f, 0f);
        meteorCostRect.sizeDelta = new Vector2(-32f, 24f);
        meteorCostRect.anchoredPosition = new Vector2(0f, 26f);
        gachaMeteorCostText.gameObject.SetActive(false);

        gachaMeteorRatesText = CreateTmpText("MeteorRates", meteorFrame.transform, string.Empty, 15f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.75f, 0.80f, 0.84f, 1f));
        RectTransform meteorRatesRect = gachaMeteorRatesText.rectTransform;
        meteorRatesRect.anchorMin = new Vector2(0f, 0f);
        meteorRatesRect.anchorMax = new Vector2(1f, 0f);
        meteorRatesRect.pivot = new Vector2(0.5f, 0f);
        meteorRatesRect.sizeDelta = new Vector2(-32f, 22f);
        meteorRatesRect.anchoredPosition = new Vector2(0f, 2f);
        gachaMeteorRatesText.gameObject.SetActive(false);

        gachaRewardsButton = CreateButton("RewardsButton", card.transform, string.Empty);
        RectTransform rewardsButtonRect = gachaRewardsButton.GetComponent<RectTransform>();
        rewardsButtonRect.anchorMin = new Vector2(0.5f, 0f);
        rewardsButtonRect.anchorMax = new Vector2(0.5f, 0f);
        rewardsButtonRect.pivot = new Vector2(0.5f, 0f);
        rewardsButtonRect.sizeDelta = new Vector2(0f, 0f);
        rewardsButtonRect.anchoredPosition = new Vector2(-9999f, -9999f);
        gachaRewardsButton.onClick.AddListener(OnOpenGachaRewardsPressed);

        GameObject pullButtonRow = CreateUIObject("PullButtonRow", card.transform);
        RectTransform pullButtonRowRect = pullButtonRow.GetComponent<RectTransform>();
        pullButtonRowRect.anchorMin = new Vector2(0.5f, 0f);
        pullButtonRowRect.anchorMax = new Vector2(0.5f, 0f);
        pullButtonRowRect.pivot = new Vector2(0.5f, 0f);
        pullButtonRowRect.sizeDelta = new Vector2(360f, 44f);
        pullButtonRowRect.anchoredPosition = new Vector2(-200f, 96f);

        HorizontalLayoutGroup pullButtonLayout = pullButtonRow.AddComponent<HorizontalLayoutGroup>();
        pullButtonLayout.spacing = 16f;
        pullButtonLayout.childAlignment = TextAnchor.MiddleCenter;
        pullButtonLayout.childControlWidth = false;
        pullButtonLayout.childControlHeight = true;
        pullButtonLayout.childForceExpandWidth = false;
        pullButtonLayout.childForceExpandHeight = false;

        gachaSinglePullButton = CreateButton("SinglePullButton", pullButtonRow.transform, string.Empty);
        RectTransform singleRect = gachaSinglePullButton.GetComponent<RectTransform>();
        singleRect.sizeDelta = new Vector2(172f, 44f);
        LayoutElement singleLayout = gachaSinglePullButton.gameObject.AddComponent<LayoutElement>();
        singleLayout.preferredWidth = 172f;
        singleLayout.preferredHeight = 44f;
        gachaSinglePullButton.onClick.AddListener(() => OnCurrentMeteorPullPressed(false));

        gachaTenPullButton = CreateButton("TenPullButton", pullButtonRow.transform, string.Empty);
        RectTransform tenRect = gachaTenPullButton.GetComponent<RectTransform>();
        tenRect.sizeDelta = new Vector2(172f, 44f);
        LayoutElement tenLayout = gachaTenPullButton.gameObject.AddComponent<LayoutElement>();
        tenLayout.preferredWidth = 172f;
        tenLayout.preferredHeight = 44f;
        gachaTenPullButton.onClick.AddListener(() => OnCurrentMeteorPullPressed(true));

        GameObject resultArea = CreateUIObject("ResultArea", card.transform);
        Image resultImage = resultArea.AddComponent<Image>();
        resultImage.color = new Color(0.11f, 0.14f, 0.18f, 0.96f);
        Outline resultOutline = resultArea.AddComponent<Outline>();
        resultOutline.effectColor = new Color(1f, 1f, 1f, 0.06f);
        resultOutline.effectDistance = new Vector2(1f, -1f);
        RectTransform resultRect = resultArea.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0f, 0f);
        resultRect.anchorMax = new Vector2(1f, 0f);
        resultRect.offsetMin = new Vector2(40f, 28f);
        resultRect.offsetMax = new Vector2(-236f, 78f);

        gachaResultText = CreateTmpText("ResultText", resultArea.transform, string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.92f, 0.94f, 1f));
        gachaResultText.enableWordWrapping = true;
        StretchToParent(gachaResultText.rectTransform, 18f, 18f);

        gachaCloseButton = CreateButton("GachaCloseButton", card.transform, L("common.close", "Close"));
        RectTransform closeRect = gachaCloseButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(1f, 0f);
        closeRect.sizeDelta = new Vector2(170f, 46f);
        closeRect.anchoredPosition = new Vector2(-40f, 28f);
        gachaCloseButton.onClick.AddListener(OnCloseGachaPressed);

        gachaRewardsPanel = CreateUIObject("GachaRewardsPanel", card.transform);
        Image rewardsPanelImage = gachaRewardsPanel.AddComponent<Image>();
        rewardsPanelImage.color = new Color(0.09f, 0.12f, 0.16f, 0.98f);
        Outline rewardsPanelOutline = gachaRewardsPanel.AddComponent<Outline>();
        rewardsPanelOutline.effectColor = new Color(0.92f, 0.75f, 0.28f, 0.14f);
        rewardsPanelOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform rewardsPanelRect = gachaRewardsPanel.GetComponent<RectTransform>();
        rewardsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        rewardsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        rewardsPanelRect.pivot = new Vector2(0.5f, 0.5f);
        rewardsPanelRect.sizeDelta = new Vector2(420f, 430f);
        rewardsPanelRect.anchoredPosition = new Vector2(260f, -44f);
        gachaRewardsPanel.SetActive(true);

        gachaRewardsTitleText = CreateTmpText("RewardsPanelTitle", gachaRewardsPanel.transform, string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform rewardsTitleRect = gachaRewardsTitleText.rectTransform;
        rewardsTitleRect.anchorMin = new Vector2(0f, 1f);
        rewardsTitleRect.anchorMax = new Vector2(1f, 1f);
        rewardsTitleRect.pivot = new Vector2(0f, 1f);
        rewardsTitleRect.sizeDelta = new Vector2(-32f, 34f);
        rewardsTitleRect.anchoredPosition = new Vector2(16f, -14f);

        GameObject rewardsDivider = CreateUIObject("RewardsDivider", gachaRewardsPanel.transform);
        Image rewardsDividerImage = rewardsDivider.AddComponent<Image>();
        rewardsDividerImage.color = new Color(1f, 1f, 1f, 0.08f);
        RectTransform rewardsDividerRect = rewardsDivider.GetComponent<RectTransform>();
        rewardsDividerRect.anchorMin = new Vector2(0f, 1f);
        rewardsDividerRect.anchorMax = new Vector2(1f, 1f);
        rewardsDividerRect.pivot = new Vector2(0.5f, 1f);
        rewardsDividerRect.offsetMin = new Vector2(16f, -50f);
        rewardsDividerRect.offsetMax = new Vector2(-16f, -48f);

        GameObject rewardsViewport = CreateUIObject("RewardsViewport", gachaRewardsPanel.transform);
        Image rewardsViewportImage = rewardsViewport.AddComponent<Image>();
        rewardsViewportImage.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);
        Mask rewardsMask = rewardsViewport.AddComponent<Mask>();
        rewardsMask.showMaskGraphic = true;
        RectTransform rewardsViewportRect = rewardsViewport.GetComponent<RectTransform>();
        rewardsViewportRect.anchorMin = new Vector2(0f, 0f);
        rewardsViewportRect.anchorMax = new Vector2(1f, 1f);
        rewardsViewportRect.offsetMin = new Vector2(14f, 14f);
        rewardsViewportRect.offsetMax = new Vector2(-14f, -60f);

        GameObject rewardsContent = CreateUIObject("RewardsContent", rewardsViewport.transform);
        gachaRewardsContent = rewardsContent.GetComponent<RectTransform>();
        gachaRewardsContent.anchorMin = new Vector2(0f, 1f);
        gachaRewardsContent.anchorMax = new Vector2(1f, 1f);
        gachaRewardsContent.pivot = new Vector2(0f, 1f);
        gachaRewardsContent.sizeDelta = Vector2.zero;
        gachaRewardsContent.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup rewardsLayout = rewardsContent.AddComponent<VerticalLayoutGroup>();
        rewardsLayout.spacing = 8f;
        rewardsLayout.padding = new RectOffset(8, 8, 8, 8);
        rewardsLayout.childControlHeight = true;
        rewardsLayout.childControlWidth = true;
        rewardsLayout.childForceExpandHeight = false;
        rewardsLayout.childForceExpandWidth = true;

        ContentSizeFitter rewardsFitter = rewardsContent.AddComponent<ContentSizeFitter>();
        rewardsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rewardsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect rewardsScroll = rewardsViewport.AddComponent<ScrollRect>();
        rewardsScroll.viewport = rewardsViewportRect;
        rewardsScroll.content = gachaRewardsContent;
        rewardsScroll.horizontal = false;
        rewardsScroll.vertical = true;

        gachaRewardsCloseButton = CreateButton("RewardsCloseButton", gachaRewardsPanel.transform, L("common.close", "Close"));
        RectTransform rewardsCloseRect = gachaRewardsCloseButton.GetComponent<RectTransform>();
        rewardsCloseRect.anchorMin = new Vector2(1f, 0f);
        rewardsCloseRect.anchorMax = new Vector2(1f, 0f);
        rewardsCloseRect.pivot = new Vector2(1f, 0f);
        rewardsCloseRect.sizeDelta = new Vector2(0f, 0f);
        rewardsCloseRect.anchoredPosition = new Vector2(-9999f, -9999f);
        gachaRewardsCloseButton.onClick.AddListener(OnCloseGachaRewardsPressed);

        ApplyGachaButtonStyle(gachaPrevButton, new Color(0.12f, 0.15f, 0.19f, 0.95f), new Color(0.92f, 0.75f, 0.28f, 1f), 28, FontStyle.Bold);
        ApplyGachaButtonStyle(gachaNextButton, new Color(0.12f, 0.15f, 0.19f, 0.95f), new Color(0.92f, 0.75f, 0.28f, 1f), 28, FontStyle.Bold);
        ApplyGachaButtonStyle(gachaRewardsButton, new Color(0.17f, 0.20f, 0.24f, 0.98f), new Color(0.96f, 0.94f, 0.90f, 1f), 18, FontStyle.Bold);
        ApplyGachaButtonStyle(gachaSinglePullButton, new Color(0.90f, 0.72f, 0.24f, 1f), new Color(0.08f, 0.09f, 0.11f, 1f), 18, FontStyle.Bold);
        ApplyGachaButtonStyle(gachaTenPullButton, new Color(0.24f, 0.34f, 0.50f, 1f), new Color(0.96f, 0.97f, 0.98f, 1f), 18, FontStyle.Bold);
        ApplyGachaButtonStyle(gachaCloseButton, new Color(0.16f, 0.18f, 0.22f, 0.98f), new Color(0.93f, 0.93f, 0.95f, 1f), 18, FontStyle.Bold);
        ApplyGachaButtonStyle(gachaRewardsCloseButton, new Color(0.16f, 0.18f, 0.22f, 0.98f), new Color(0.93f, 0.93f, 0.95f, 1f), 18, FontStyle.Bold);
        gachaRewardsButton.gameObject.SetActive(false);
        gachaRewardsCloseButton.gameObject.SetActive(false);
    }

    private void RefreshGachaPanel()
    {
        if (gachaPanel == null)
        {
            return;
        }

        EnsureMenuGachaManager();

        if (gachaTitleText != null)
        {
            gachaTitleText.text = L("menu.gacha", "Gacha");
        }

        if (gachaSubtitleText != null)
        {
            gachaSubtitleText.text = L("menu.gacha.panel_subtitle", "Browse meteorite banners, inspect the pool, and pull from the main menu.");
        }

        SaveFile_Profile profile = SaveManager.Profile;
        if (profile != null)
        {
            RefreshCurrencyBadge(gachaGoldIconImage, gachaGoldAmountText, CurrencyType.Gold, profile.user_profile.gold);
            RefreshCurrencyBadge(gachaOrbIconImage, gachaOrbAmountText, CurrencyType.Orb, profile.user_profile.orbs);
        }

        if (gachaResultText != null)
        {
            string summary = GachaManager.Instance != null ? GachaManager.Instance.LastPullSummary : string.Empty;
            gachaResultText.text = string.IsNullOrWhiteSpace(summary)
                ? L("menu.gacha.result_default", "Choose a meteor, inspect its rewards, and pull when you are ready.")
                : summary;
        }

        SetButtonLabel(gachaCloseButton, L("common.close", "Close"));
        SetButtonLabel(gachaRewardsCloseButton, L("common.close", "Close"));
        SetButtonLabel(gachaRewardsButton, L("menu.gacha.meteor_rewards", "Meteor Rewards"));

        List<MeteoriteBanner> banners = GetAvailableGachaBanners();
        if (banners.Count == 0)
        {
            ShowEmptyGachaState();
            return;
        }

        selectedGachaBannerIndex = Mathf.Clamp(selectedGachaBannerIndex, 0, banners.Count - 1);
        MeteoriteBanner banner = banners[selectedGachaBannerIndex];
        if (gachaMeteorIndexText != null)
        {
            gachaMeteorIndexText.text = $"{selectedGachaBannerIndex + 1} / {banners.Count}";
        }

        if (gachaMeteorNameText != null)
        {
            gachaMeteorNameText.text = GetBannerDisplayName(banner);
        }

        if (gachaMeteorCostText != null)
        {
            gachaMeteorCostText.text = string.Empty;
        }

        if (gachaMeteorRatesText != null)
        {
            gachaMeteorRatesText.text = string.Empty;
        }

        SetCurrencyButtonLabel(gachaSinglePullButton, "1 Pull", banner.singlePullCost, banner.currencyType);
        SetPullButtonUnavailableState(
            gachaTenPullButton,
            banner.tenPullCost > 0,
            banner.tenPullCost,
            banner.currencyType,
            "10 Pull");

        if (gachaPrevButton != null)
        {
            gachaPrevButton.interactable = banners.Count > 1;
        }

        if (gachaNextButton != null)
        {
            gachaNextButton.interactable = banners.Count > 1;
        }

        if (gachaSinglePullButton != null)
        {
            gachaSinglePullButton.interactable = GachaManager.Instance != null && GachaManager.Instance.CanAfford(banner, false);
        }

        if (gachaTenPullButton != null)
        {
            gachaTenPullButton.interactable = banner.tenPullCost > 0 && GachaManager.Instance != null && GachaManager.Instance.CanAfford(banner, true);
        }

        if (gachaRewardsTitleText != null)
        {
            gachaRewardsTitleText.text = L("menu.gacha.rewards", "Rewards");
        }

        RefreshMeteorPlaceholderVisual(banner);
        PopulateGachaRewardsPanel(banner);
    }

    private List<MeteoriteBanner> GetAvailableGachaBanners()
    {
        List<MeteoriteBanner> banners = new List<MeteoriteBanner>();
        if (GameDatabase.Instance == null || GameDatabase.Instance.allGachaBanners == null)
        {
            return banners;
        }

        foreach (MeteoriteBanner banner in GameDatabase.Instance.allGachaBanners)
        {
            if (banner != null)
            {
                banners.Add(banner);
            }
        }

        return banners;
    }

    private MeteoriteBanner GetCurrentGachaBanner()
    {
        List<MeteoriteBanner> banners = GetAvailableGachaBanners();
        if (banners.Count == 0)
        {
            return null;
        }

        selectedGachaBannerIndex = Mathf.Clamp(selectedGachaBannerIndex, 0, banners.Count - 1);
        return banners[selectedGachaBannerIndex];
    }

    private void SelectPreviousMeteor()
    {
        List<MeteoriteBanner> banners = GetAvailableGachaBanners();
        if (banners.Count <= 1)
        {
            return;
        }

        selectedGachaBannerIndex = (selectedGachaBannerIndex - 1 + banners.Count) % banners.Count;
        RefreshGachaPanel();
    }

    private void SelectNextMeteor()
    {
        List<MeteoriteBanner> banners = GetAvailableGachaBanners();
        if (banners.Count <= 1)
        {
            return;
        }

        selectedGachaBannerIndex = (selectedGachaBannerIndex + 1) % banners.Count;
        RefreshGachaPanel();
    }

    private void ShowEmptyGachaState()
    {
        if (gachaRewardsContent != null)
        {
            foreach (Transform child in gachaRewardsContent)
            {
                Destroy(child.gameObject);
            }
        }

        if (gachaMeteorIndexText != null)
        {
            gachaMeteorIndexText.text = "0 / 0";
        }

        if (gachaMeteorNameText != null)
        {
            gachaMeteorNameText.text = L("menu.gacha.empty", "No meteor banners configured.");
        }

        if (gachaMeteorCostText != null)
        {
            gachaMeteorCostText.text = string.Empty;
        }

        if (gachaMeteorRatesText != null)
        {
            gachaMeteorRatesText.text = string.Empty;
        }

        if (gachaMeteorImage != null)
        {
            gachaMeteorImage.enabled = false;
            gachaMeteorImage.sprite = null;
        }

        if (gachaMeteorPlaceholderImage != null)
        {
            gachaMeteorPlaceholderImage.color = new Color(0.20f, 0.23f, 0.28f, 1f);
        }

        if (gachaMeteorPlaceholderText != null)
        {
            gachaMeteorPlaceholderText.text = "--";
        }

        if (gachaRewardsTitleText != null)
        {
            gachaRewardsTitleText.text = L("menu.gacha.rewards", "Rewards");
        }

        if (gachaResultText != null)
        {
            gachaResultText.text = L("menu.gacha.empty_result", "Add meteorite banners to the game database to enable pulls.");
        }

        if (gachaSinglePullButton != null)
        {
            gachaSinglePullButton.interactable = false;
            SetButtonLabel(gachaSinglePullButton, L("menu.gacha.single_pull", "Single Pull"));
            ConfigureButtonCurrencyIcon(gachaSinglePullButton, null);
        }

        if (gachaTenPullButton != null)
        {
            gachaTenPullButton.interactable = false;
            SetButtonLabel(gachaTenPullButton, L("menu.gacha.ten_pull", "Ten Pull"));
            ConfigureButtonCurrencyIcon(gachaTenPullButton, null);
        }

        if (gachaPrevButton != null)
        {
            gachaPrevButton.interactable = false;
        }

        if (gachaNextButton != null)
        {
            gachaNextButton.interactable = false;
        }
    }

    private void RefreshMeteorPlaceholderVisual(MeteoriteBanner banner)
    {
        Color accentColor = GetMeteorAccentColor(banner);

        if (gachaMeteorPlaceholderImage != null)
        {
            gachaMeteorPlaceholderImage.color = Color.Lerp(new Color(0.11f, 0.14f, 0.18f, 1f), accentColor, 0.82f);
            Outline outline = gachaMeteorPlaceholderImage.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.28f);
            }
        }

        if (gachaMeteorImage == null || gachaMeteorPlaceholderText == null)
        {
            return;
        }

        if (banner != null && banner.displaySprite != null)
        {
            gachaMeteorImage.sprite = banner.displaySprite;
            gachaMeteorImage.enabled = true;
            gachaMeteorPlaceholderText.text = string.Empty;
        }
        else
        {
            gachaMeteorImage.sprite = null;
            gachaMeteorImage.enabled = false;
            gachaMeteorPlaceholderText.text = GetMeteorPlaceholderLabel(banner);
        }
    }

    private string GetMeteorPlaceholderLabel(MeteoriteBanner banner)
    {
        string displayName = GetBannerDisplayName(banner);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "M";
        }

        string[] words = displayName.Split(' ');
        string label = string.Empty;
        for (int i = 0; i < words.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(words[i]))
            {
                continue;
            }

            label += char.ToUpperInvariant(words[i][0]);
            if (label.Length >= 2)
            {
                break;
            }
        }

        return string.IsNullOrWhiteSpace(label) ? "M" : label;
    }

    private string GetBannerDisplayName(MeteoriteBanner banner)
    {
        if (banner == null)
        {
            return L("menu.gacha.reward_unknown", "Unknown reward");
        }

        if (!string.IsNullOrWhiteSpace(banner.bannerName))
        {
            return banner.bannerName.Trim();
        }

        return banner.name.Replace('_', ' ').Trim();
    }

    private Color GetMeteorAccentColor(MeteoriteBanner banner)
    {
        string id = banner != null ? banner.GetBackendBannerId() : string.Empty;
        id = id.ToLowerInvariant();

        if (id.Contains("arcane"))
        {
            return new Color(0.42f, 0.26f, 0.62f, 1f);
        }

        if (id.Contains("dust"))
        {
            return new Color(0.53f, 0.39f, 0.24f, 1f);
        }

        if (id.Contains("eth"))
        {
            return new Color(0.42f, 0.62f, 0.78f, 1f);
        }

        if (id.Contains("luminous"))
        {
            return new Color(0.70f, 0.63f, 0.26f, 1f);
        }

        if (id.Contains("radiant"))
        {
            return new Color(0.75f, 0.44f, 0.22f, 1f);
        }

        if (id.Contains("runic"))
        {
            return new Color(0.22f, 0.54f, 0.58f, 1f);
        }

        if (id.Contains("shiny"))
        {
            return new Color(0.58f, 0.60f, 0.70f, 1f);
        }

        return new Color(0.25f, 0.35f, 0.55f, 1f);
    }

    private void OnOpenGachaRewardsPressed()
    {
        MeteoriteBanner banner = GetCurrentGachaBanner();
        if (banner != null)
        {
            PopulateGachaRewardsPanel(banner);
        }
    }

    private void OnCloseGachaRewardsPressed()
    {
        // Rewards are embedded in the main gacha layout now.
    }

    private void PopulateGachaRewardsPanel(MeteoriteBanner banner)
    {
        if (gachaRewardsContent == null)
        {
            return;
        }

        foreach (Transform child in gachaRewardsContent)
        {
            Destroy(child.gameObject);
        }

        if (gachaRewardsTitleText != null)
        {
            gachaRewardsTitleText.text = L("menu.gacha.rewards", "Rewards");
        }

        SaveFile_Profile profile = SaveManager.Profile;
        bool addedAnySection = false;
        addedAnySection |= CreateGachaRewardSection(L("menu.gacha.rarity.common", "Common"), banner.commonPool, profile, new Color(0.46f, 0.51f, 0.58f, 0.22f));
        addedAnySection |= CreateGachaRewardSection(L("menu.gacha.rarity.rare", "Rare"), banner.rarePool, profile, new Color(0.20f, 0.44f, 0.69f, 0.22f));
        addedAnySection |= CreateGachaRewardSection(L("menu.gacha.rarity.epic", "Epic"), banner.epicPool, profile, new Color(0.49f, 0.26f, 0.63f, 0.22f));
        addedAnySection |= CreateGachaRewardSection(L("menu.gacha.rarity.mythic", "Mythic"), banner.mythicPool, profile, new Color(0.78f, 0.58f, 0.18f, 0.22f));

        if (!addedAnySection)
        {
            TextMeshProUGUI emptyText = CreateTmpText("NoRewards", gachaRewardsContent, L("menu.gacha.no_rewards", "No rewards configured."), 18f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color(0.80f, 0.82f, 0.85f, 1f));
            LayoutElement layout = emptyText.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 42f;
        }

        ScrollRect rewardsScroll = gachaRewardsPanel != null ? gachaRewardsPanel.GetComponentInChildren<ScrollRect>(true) : null;
        if (rewardsScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            rewardsScroll.horizontalNormalizedPosition = 0f;
            rewardsScroll.verticalNormalizedPosition = 1f;
        }
    }

    private bool CreateGachaRewardSection(string title, List<GachaRewardEntry> pool, SaveFile_Profile profile, Color accentColor)
    {
        if (pool == null || pool.Count == 0 || gachaRewardsContent == null)
        {
            return false;
        }

        GameObject section = CreateUIObject(title + "_Section", gachaRewardsContent);
        Image sectionImage = section.AddComponent<Image>();
        sectionImage.color = new Color(0.12f, 0.15f, 0.19f, 0.98f);
        Outline sectionOutline = section.AddComponent<Outline>();
        sectionOutline.effectColor = accentColor;
        sectionOutline.effectDistance = new Vector2(1f, -1f);
        VerticalLayoutGroup sectionLayout = section.AddComponent<VerticalLayoutGroup>();
        sectionLayout.padding = new RectOffset(10, 10, 10, 10);
        sectionLayout.spacing = 6f;
        sectionLayout.childControlHeight = true;
        sectionLayout.childControlWidth = true;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.childForceExpandWidth = true;
        ContentSizeFitter sectionFitter = section.AddComponent<ContentSizeFitter>();
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sectionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        TextMeshProUGUI sectionTitle = CreateTmpText("Title", section.transform, title, 17f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        LayoutElement sectionTitleLayout = sectionTitle.gameObject.AddComponent<LayoutElement>();
        sectionTitleLayout.preferredHeight = 22f;

        for (int i = 0; i < pool.Count; i++)
        {
            GachaRewardEntry reward = pool[i];
            if (reward == null)
            {
                continue;
            }

            GameObject rewardRow = CreateUIObject("Reward_" + i, section.transform);
            Image rowImage = rewardRow.AddComponent<Image>();
            rowImage.color = new Color(0.08f, 0.10f, 0.13f, 0.96f);
            LayoutElement rowLayout = rewardRow.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 44f;

            HorizontalLayoutGroup rewardLayout = rewardRow.AddComponent<HorizontalLayoutGroup>();
            rewardLayout.padding = new RectOffset(12, 12, 6, 6);
            rewardLayout.spacing = 8f;
            rewardLayout.childAlignment = TextAnchor.MiddleLeft;
            rewardLayout.childControlWidth = true;
            rewardLayout.childControlHeight = true;
            rewardLayout.childForceExpandWidth = false;
            rewardLayout.childForceExpandHeight = false;

            Image rewardIcon = CreateUIObject("Icon", rewardRow.transform).AddComponent<Image>();
            LayoutElement rewardIconLayout = rewardIcon.gameObject.AddComponent<LayoutElement>();
            rewardIconLayout.preferredWidth = 28f;
            rewardIconLayout.preferredHeight = 28f;
            rewardIconLayout.minWidth = 28f;
            rewardIconLayout.minHeight = 28f;
            CurrencyUiUtility.ApplySprite(rewardIcon, GetGachaRewardIcon(reward));

            TextMeshProUGUI rewardNameText = CreateTmpText("Name", rewardRow.transform, GetGachaRewardDisplayName(reward), 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.94f, 0.93f, 0.90f, 1f));
            rewardNameText.enableWordWrapping = false;
            rewardNameText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement rewardNameLayout = rewardNameText.gameObject.AddComponent<LayoutElement>();
            rewardNameLayout.flexibleWidth = 1f;
            rewardNameLayout.minWidth = 110f;

            GameObject rewardMetaGroup = CreateUIObject("MetaGroup", rewardRow.transform);
            HorizontalLayoutGroup metaLayout = rewardMetaGroup.AddComponent<HorizontalLayoutGroup>();
            metaLayout.spacing = 6f;
            metaLayout.childAlignment = TextAnchor.MiddleRight;
            metaLayout.childControlWidth = false;
            metaLayout.childControlHeight = true;
            metaLayout.childForceExpandWidth = false;
            metaLayout.childForceExpandHeight = false;
            LayoutElement rewardMetaGroupLayout = rewardMetaGroup.AddComponent<LayoutElement>();
            rewardMetaGroupLayout.preferredWidth = 116f;
            rewardMetaGroupLayout.minWidth = 84f;

            Image rewardMetaIcon = CreateUIObject("MetaIcon", rewardMetaGroup.transform).AddComponent<Image>();
            LayoutElement rewardMetaIconLayout = rewardMetaIcon.gameObject.AddComponent<LayoutElement>();
            rewardMetaIconLayout.preferredWidth = 18f;
            rewardMetaIconLayout.preferredHeight = 18f;
            rewardMetaIconLayout.minWidth = 18f;
            rewardMetaIconLayout.minHeight = 18f;
            CurrencyType? metaCurrencyType = GetGachaRewardMetaCurrencyType(reward, profile);
            if (metaCurrencyType.HasValue)
            {
                CurrencyUiUtility.ApplyCurrencySprite(rewardMetaIcon, metaCurrencyType.Value);
            }
            else
            {
                rewardMetaIcon.gameObject.SetActive(false);
            }

            TextMeshProUGUI rewardMetaText = CreateTmpText("Meta", rewardMetaGroup.transform, GetGachaRewardMetaText(reward, profile), 11f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, GetGachaRewardMetaColor(reward, profile));
            rewardMetaText.enableWordWrapping = false;
            rewardMetaText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement rewardMetaLayout = rewardMetaText.gameObject.AddComponent<LayoutElement>();
            rewardMetaLayout.preferredWidth = 92f;
            rewardMetaLayout.minWidth = 54f;
        }

        return true;
    }

    private string GetGachaRewardMetaText(GachaRewardEntry reward, SaveFile_Profile profile)
    {
        if (reward == null)
        {
            return string.Empty;
        }

        if (IsGachaRewardOwned(reward, profile))
        {
            return $"+{CurrencyUiUtility.FormatAmount(reward.duplicateConversionAmount)}";
        }

        switch (reward.type)
        {
            case RewardType.Character:
                return L("menu.gacha.reward_character", "Character");
            case RewardType.Weapon:
                return L("menu.gacha.reward_weapon", "Weapon");
            case RewardType.Consumable:
                return L("menu.gacha.reward_consumable", "Consumable");
            case RewardType.Gold:
            case RewardType.Orb:
                return string.Empty;
            case RewardType.Currency:
                return L("menu.gacha.reward_currency", "Currency");
            default:
                return string.Empty;
        }
    }

    private CurrencyType? GetGachaRewardMetaCurrencyType(GachaRewardEntry reward, SaveFile_Profile profile)
    {
        if (reward == null)
        {
            return null;
        }

        return IsGachaRewardOwned(reward, profile) ? reward.duplicateConversionType : (CurrencyType?)null;
    }

    private Color GetGachaRewardMetaColor(GachaRewardEntry reward, SaveFile_Profile profile)
    {
        return IsGachaRewardOwned(reward, profile)
            ? new Color(0.92f, 0.75f, 0.28f, 1f)
            : new Color(0.73f, 0.82f, 0.84f, 1f);
    }

    private bool IsGachaRewardOwned(GachaRewardEntry reward, SaveFile_Profile profile)
    {
        if (reward == null || profile == null)
        {
            return false;
        }

        if (reward.type == RewardType.Weapon)
        {
            string weaponId = reward.itemReference != null ? reward.itemReference.name : reward.idName;
            return !string.IsNullOrWhiteSpace(weaponId) && profile.weapons.unlocked_weapon_ids.Contains(weaponId);
        }

        if (reward.type == RewardType.Character)
        {
            string characterId = reward.characterReference != null ? reward.characterReference.characterID : reward.idName;
            return !string.IsNullOrWhiteSpace(characterId) && profile.characters.owned_character_ids.Contains(characterId);
        }

        return false;
    }

    private void OnCurrentMeteorPullPressed(bool isTenPull)
    {
        EnsureMenuGachaManager();

        MeteoriteBanner banner = GetCurrentGachaBanner();
        if (GachaManager.Instance == null || banner == null)
        {
            return;
        }

        GachaManager.Instance.PerformPull(banner, isTenPull);
        RefreshModernMenuContent();
        RefreshGachaPanel();
    }


    private string GetGachaRewardDisplayName(GachaRewardEntry reward)
    {
        if (reward == null)
        {
            return L("menu.gacha.reward_unknown", "Unknown reward");
        }

        if (reward.type == RewardType.Character && reward.characterReference != null)
        {
            return reward.characterReference.characterName;
        }

        if ((reward.type == RewardType.Weapon || reward.type == RewardType.Consumable) && reward.itemReference != null)
        {
            return reward.itemReference.itemName;
        }

        CurrencyType? rewardCurrencyType = GetGachaRewardCurrencyType(reward);
        if (rewardCurrencyType.HasValue && CurrencyUiUtility.GetSprite(rewardCurrencyType.Value) != null)
        {
            return CurrencyUiUtility.FormatAmount(reward.amount);
        }

        return string.IsNullOrWhiteSpace(reward.idName) ? L("menu.gacha.reward_unknown", "Unknown reward") : reward.idName;
    }

    private Sprite GetGachaRewardIcon(GachaRewardEntry reward)
    {
        if (reward == null)
        {
            return null;
        }

        CurrencyType? currencyType = GetGachaRewardCurrencyType(reward);
        if (currencyType.HasValue)
        {
            Sprite currencySprite = CurrencyUiUtility.GetSprite(currencyType.Value);
            if (currencySprite != null)
            {
                return currencySprite;
            }
        }

        if (reward.itemReference != null)
        {
            return reward.itemReference.icon;
        }

        return null;
    }

    private CurrencyType? GetGachaRewardCurrencyType(GachaRewardEntry reward)
    {
        if (reward == null)
        {
            return null;
        }

        switch (reward.type)
        {
            case RewardType.Gold:
                return CurrencyType.Gold;
            case RewardType.Orb:
                return CurrencyType.Orb;
            case RewardType.Currency:
                if (reward.itemReference is CurrencyItem currencyItem)
                {
                    return currencyItem.currencyType;
                }

                return null;
            default:
                return null;
        }
    }

    private void EnsureAuthOnboardingPanel()
    {
        if (authOnboardingPanel != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not create auth onboarding panel because no Canvas was found.");
            return;
        }

        authOnboardingPanel = CreateUIObject("AuthOnboardingPanel", canvas.transform);
        Image overlay = authOnboardingPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.88f);
        StretchToParent(authOnboardingPanel.GetComponent<RectTransform>());
        authOnboardingPanel.SetActive(false);

        GameObject card = CreateUIObject("AuthCard", authOnboardingPanel.transform);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.09f, 0.11f, 0.14f, 0.985f);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.90f, 0.77f, 0.34f, 0.22f);
        cardOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(720f, 610f);
        cardRect.anchoredPosition = Vector2.zero;

        authTitleText = CreateTmpText("Title", card.transform, string.Empty, 38f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.98f, 0.95f, 0.88f, 1f));
        RectTransform titleRect = authTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(-56f, 56f);
        titleRect.anchoredPosition = new Vector2(28f, -28f);

        authSubtitleText = CreateTmpText("Subtitle", card.transform, string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.73f, 0.82f, 0.84f, 1f));
        RectTransform subtitleRect = authSubtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(-56f, 74f);
        subtitleRect.anchoredPosition = new Vector2(28f, -84f);

        authSignInTabButton = CreateButton("AuthSignInTab", card.transform, L("auth.sign_in", "Sign In"));
        RectTransform signInTabRect = authSignInTabButton.GetComponent<RectTransform>();
        signInTabRect.anchorMin = new Vector2(0f, 1f);
        signInTabRect.anchorMax = new Vector2(0f, 1f);
        signInTabRect.pivot = new Vector2(0f, 1f);
        signInTabRect.sizeDelta = new Vector2(160f, 46f);
        signInTabRect.anchoredPosition = new Vector2(28f, -176f);
        authSignInTabButton.onClick.AddListener(OnAuthSignInTabPressed);

        authSignUpTabButton = CreateButton("AuthSignUpTab", card.transform, L("auth.sign_up", "Sign Up"));
        RectTransform signUpTabRect = authSignUpTabButton.GetComponent<RectTransform>();
        signUpTabRect.anchorMin = new Vector2(0f, 1f);
        signUpTabRect.anchorMax = new Vector2(0f, 1f);
        signUpTabRect.pivot = new Vector2(0f, 1f);
        signUpTabRect.sizeDelta = new Vector2(160f, 46f);
        signUpTabRect.anchoredPosition = new Vector2(198f, -176f);
        authSignUpTabButton.onClick.AddListener(OnAuthSignUpTabPressed);

        authDisplayNameRow = CreateInputRow(card.transform, "DisplayNameRow", L("auth.display_name", "Display Name"), out authDisplayNameInput, false, new Vector2(28f, -248f));
        CreateInputRow(card.transform, "EmailRow", L("auth.email", "Email"), out authEmailInput, false, new Vector2(28f, -342f));
        CreateInputRow(card.transform, "PasswordRow", L("auth.password", "Password"), out authPasswordInput, true, new Vector2(28f, -436f));

        if (authEmailInput != null)
        {
            authEmailInput.onEndEdit.AddListener(_ => CacheAuthInputValues());
        }

        if (authDisplayNameInput != null)
        {
            authDisplayNameInput.onEndEdit.AddListener(_ => CacheAuthInputValues());
        }

        authStatusText = CreateTmpText("Status", card.transform, string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.75f, 0.28f, 1f));
        RectTransform statusRect = authStatusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0f, 0f);
        statusRect.sizeDelta = new Vector2(-56f, 54f);
        statusRect.anchoredPosition = new Vector2(28f, 114f);

        authGuestButton = CreateButton("ContinueGuestButton", card.transform, L("auth.continue_guest", "Continue as Guest"));
        RectTransform guestRect = authGuestButton.GetComponent<RectTransform>();
        guestRect.anchorMin = new Vector2(0f, 0f);
        guestRect.anchorMax = new Vector2(0f, 0f);
        guestRect.pivot = new Vector2(0f, 0f);
        guestRect.sizeDelta = new Vector2(220f, 48f);
        guestRect.anchoredPosition = new Vector2(28f, 28f);
        authGuestButton.onClick.AddListener(OnContinueAsGuestPressed);

        authSubmitButton = CreateButton("AuthSubmitButton", card.transform, string.Empty);
        RectTransform submitRect = authSubmitButton.GetComponent<RectTransform>();
        submitRect.anchorMin = new Vector2(1f, 0f);
        submitRect.anchorMax = new Vector2(1f, 0f);
        submitRect.pivot = new Vector2(1f, 0f);
        submitRect.sizeDelta = new Vector2(220f, 48f);
        submitRect.anchoredPosition = new Vector2(-28f, 28f);
        authSubmitButton.onClick.AddListener(OnAuthSubmitPressed);

        SaveFile_Settings settings = SaveManager.Settings;
        if (authEmailInput != null)
        {
            authEmailInput.text = settings.general.cached_auth_email ?? string.Empty;
        }

        if (authDisplayNameInput != null)
        {
            authDisplayNameInput.text = settings.general.cached_auth_display_name ?? string.Empty;
        }

        SetAuthMode(false);
    }

    private void RefreshAuthOnboardingPanel()
    {
        if (authOnboardingPanel == null)
        {
            return;
        }

        if (authTitleText != null)
        {
            authTitleText.text = L("auth.onboarding.title", "Welcome to Eclipside");
        }

        if (authSubtitleText != null)
        {
            authSubtitleText.text = L("auth.onboarding.subtitle", "Create an account to keep your progress synced, or continue as a guest and decide later.");
        }

        SetButtonLabel(authSignInTabButton, L("auth.sign_in", "Sign In"));
        SetButtonLabel(authSignUpTabButton, L("auth.sign_up", "Sign Up"));
        SetButtonLabel(authGuestButton, L("auth.continue_guest", "Continue as Guest"));
        SetButtonLabel(authSubmitButton, authSignUpMode ? L("auth.create_account", "Create Account") : L("auth.login_action", "Log In"));

        if (authDisplayNameRow != null)
        {
            authDisplayNameRow.SetActive(authSignUpMode);
        }

        StyleAuthModeButtons();
        UpdateAuthStatusSummary();
    }

    private void TryShowAuthOnboarding()
    {
        if (authOnboardingPanel == null)
        {
            return;
        }

        SaveFile_Profile profile = SaveManager.Profile;
        SaveFile_Settings settings = SaveManager.Settings;
        bool hasAccount = !string.IsNullOrWhiteSpace(profile.user_profile.account_id);
        bool hasCompletedOnboarding = settings.general.has_completed_auth_onboarding;

        bool shouldShow = !hasAccount && !hasCompletedOnboarding;
        authOnboardingPanel.SetActive(shouldShow);
        if (shouldShow)
        {
            authOnboardingPanel.transform.SetAsLastSibling();
        }
    }

    private void HideAuthOnboarding()
    {
        if (authOnboardingPanel != null)
        {
            authOnboardingPanel.SetActive(false);
        }
    }

    private void SetAuthMode(bool signUpMode)
    {
        authSignUpMode = signUpMode;
        if (authStatusText != null)
        {
            authStatusText.text = string.Empty;
        }
        RefreshAuthOnboardingPanel();
    }

    private void StyleAuthModeButtons()
    {
        StyleAuthModeButton(authSignInTabButton, !authSignUpMode);
        StyleAuthModeButton(authSignUpTabButton, authSignUpMode);
    }

    private void StyleAuthModeButton(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected
                ? new Color(0.92f, 0.75f, 0.28f, 1f)
                : new Color(1f, 1f, 1f, 0.95f);
        }
    }

    private void CacheAuthInputValues()
    {
        SaveFile_Settings settings = SaveManager.Settings;

        if (authEmailInput != null)
        {
            settings.general.cached_auth_email = authEmailInput.text.Trim();
        }

        if (authDisplayNameInput != null)
        {
            settings.general.cached_auth_display_name = authDisplayNameInput.text.Trim();
        }

        SaveManager.SaveSettings();
    }

    private void UpdateAuthStatusSummary()
    {
        if (authStatusText == null || !string.IsNullOrWhiteSpace(authStatusText.text))
        {
            return;
        }

        authStatusText.text = authSignUpMode
            ? L("auth.status.idle_signup", "Choose a display name, then create your account.")
            : L("auth.status.idle_signin", "Sign in to load your account-linked profile.");
    }

    private void SetAuthStatus(string message)
    {
        if (authStatusText != null)
        {
            authStatusText.text = message;
        }
    }

    private void HandleAuthSucceeded(FirebaseAuthSession response)
    {
        SaveManager.Settings.general.has_completed_auth_onboarding = true;
        SaveManager.SaveSettings();
        CacheAuthInputValues();
        RefreshModernMenuContent();
        RefreshSettingsPanel();
        HideAuthOnboarding();
    }

    private void HandleAuthFailed(string message)
    {
        SetAuthStatus(message);
    }

    private void HandleAuthStatusChanged(string message)
    {
        RefreshModernMenuContent();
    }

    private void SubscribeAuthEvents()
    {
        if (DevAccountAuthManager.Instance == null)
        {
            return;
        }

        DevAccountAuthManager.Instance.OnAuthSucceeded -= HandleAuthSucceeded;
        DevAccountAuthManager.Instance.OnAuthFailed -= HandleAuthFailed;
        DevAccountAuthManager.Instance.OnStatusChanged -= HandleAuthStatusChanged;

        DevAccountAuthManager.Instance.OnAuthSucceeded += HandleAuthSucceeded;
        DevAccountAuthManager.Instance.OnAuthFailed += HandleAuthFailed;
        DevAccountAuthManager.Instance.OnStatusChanged += HandleAuthStatusChanged;
    }

    private void UnsubscribeAuthEvents()
    {
        if (DevAccountAuthManager.Instance == null)
        {
            return;
        }

        DevAccountAuthManager.Instance.OnAuthSucceeded -= HandleAuthSucceeded;
        DevAccountAuthManager.Instance.OnAuthFailed -= HandleAuthFailed;
        DevAccountAuthManager.Instance.OnStatusChanged -= HandleAuthStatusChanged;
    }

    private void CreateLandingButton(Transform parent, string key, string title, string subtitle, UnityEngine.Events.UnityAction callback, bool primary)
    {
        GameObject buttonRoot = CreateUIObject(key, parent);
        LayoutElement layout = buttonRoot.AddComponent<LayoutElement>();
        layout.preferredHeight = primary ? 118f : 98f;

        Image image = buttonRoot.AddComponent<Image>();
        image.color = primary
            ? new Color(0.90f, 0.77f, 0.34f, 1f)
            : new Color(0.16f, 0.20f, 0.24f, 1f);

        Button button = buttonRoot.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = primary
            ? new Color(0.96f, 0.83f, 0.40f, 1f)
            : new Color(0.22f, 0.27f, 0.31f, 1f);
        colors.pressedColor = primary
            ? new Color(0.78f, 0.65f, 0.24f, 1f)
            : new Color(0.12f, 0.16f, 0.19f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.32f, 0.32f, 0.32f, 0.65f);
        button.colors = colors;
        button.onClick.AddListener(callback);

        Shadow shadow = buttonRoot.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
        shadow.effectDistance = new Vector2(0f, -4f);

        GameObject textWrap = CreateUIObject("TextWrap", buttonRoot.transform);
        RectTransform textWrapRect = textWrap.GetComponent<RectTransform>();
        StretchToParent(textWrapRect, 20f, 14f);

        TextMeshProUGUI titleText = CreateTmpText("Title", textWrap.transform, title, primary ? 29f : 23f, FontStyles.Bold, TextAlignmentOptions.TopLeft, primary ? new Color(0.09f, 0.10f, 0.10f, 1f) : new Color(0.97f, 0.95f, 0.90f, 1f));
        RectTransform titleRect = titleText.rectTransform;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(0f, primary ? 36f : 30f);
        titleRect.anchoredPosition = new Vector2(0f, -2f);

        TextMeshProUGUI subtitleText = CreateTmpText("Subtitle", textWrap.transform, subtitle, 13f, FontStyles.Normal, TextAlignmentOptions.TopLeft, primary ? new Color(0.16f, 0.16f, 0.16f, 0.88f) : new Color(0.75f, 0.80f, 0.84f, 1f));
        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleText.enableWordWrapping = true;
        subtitleText.overflowMode = TextOverflowModes.Ellipsis;
        subtitleRect.anchorMin = new Vector2(0f, 0f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 0f);
        subtitleRect.offsetMin = new Vector2(0f, 8f);
        subtitleRect.offsetMax = new Vector2(0f, primary ? -40f : -34f);

        landingButtons[key] = button;
    }

    private void SetLandingButtonTexts(string key, string title, string subtitle)
    {
        if (!landingButtons.TryGetValue(key, out Button button) || button == null)
        {
            return;
        }

        TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null)
            {
                continue;
            }

            if (label.name == "Title")
            {
                label.text = title;
            }
            else if (label.name == "Subtitle")
            {
                label.text = subtitle;
            }
        }
    }

    private void BeautifyTrackedPanel(GameObject panel, string title, ref bool styledFlag)
    {
        if (panel == null)
        {
            return;
        }

        if (fallbackFont == null)
        {
            fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (styledFlag)
        {
            return;
        }

        styledFlag = true;
        BeautifyExistingPanel(panel, title);
    }

    private void BeautifyExistingPanel(GameObject panel, string title)
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return;
        }

        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image overlayImage = panel.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = panel.AddComponent<Image>();
        }
        overlayImage.color = new Color(0f, 0f, 0f, 0.82f);
        overlayImage.raycastTarget = true;

        Transform existingCard = panel.transform.Find("PrettyCard");
        GameObject card = existingCard != null ? existingCard.gameObject : CreateUIObject("PrettyCard", panel.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1040f, 760f);
        cardRect.anchoredPosition = new Vector2(0f, -24f);
        card.transform.SetAsFirstSibling();

        Image cardImage = card.GetComponent<Image>();
        if (cardImage == null)
        {
            cardImage = card.AddComponent<Image>();
        }
        cardImage.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

        Outline cardOutline = card.GetComponent<Outline>();
        if (cardOutline == null)
        {
            cardOutline = card.AddComponent<Outline>();
        }
        cardOutline.effectColor = new Color(0.92f, 0.75f, 0.28f, 0.45f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        Transform headerTransform = panel.transform.Find("PrettyHeader");
        Text header = headerTransform != null ? headerTransform.GetComponent<Text>() : CreateText("PrettyHeader", panel.transform, title, 30, TextAnchor.MiddleCenter);
        header.font = fallbackFont;
        header.fontStyle = FontStyle.Bold;
        header.color = new Color(1f, 0.96f, 0.88f, 1f);
        header.text = title;

        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0.5f, 1f);
        headerRect.anchorMax = new Vector2(0.5f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(480f, 44f);
        headerRect.anchoredPosition = new Vector2(0f, -42f);
        header.transform.SetSiblingIndex(1);

        foreach (ScrollRect scrollRect in panel.GetComponentsInChildren<ScrollRect>(true))
        {
            StyleScrollRect(scrollRect);
        }

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            StyleButton(button);
        }

        foreach (Toggle toggle in panel.GetComponentsInChildren<Toggle>(true))
        {
            StyleToggle(toggle);
        }

        foreach (Text text in panel.GetComponentsInChildren<Text>(true))
        {
            StyleLegacyText(text, text == header);
        }

        foreach (TMPro.TextMeshProUGUI tmp in panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            StyleTmpText(tmp);
        }

        foreach (Image image in panel.GetComponentsInChildren<Image>(true))
        {
            StylePanelImage(image, panel, cardImage, overlayImage);
        }
    }

    private void StyleScrollRect(ScrollRect scrollRect)
    {
        if (scrollRect == null)
        {
            return;
        }

        scrollRect.horizontal = false;
        Image image = scrollRect.GetComponent<Image>();
        if (image == null)
        {
            image = scrollRect.gameObject.AddComponent<Image>();
        }
        image.color = new Color(0.08f, 0.08f, 0.11f, 0.94f);

        Outline outline = scrollRect.GetComponent<Outline>();
        if (outline == null)
        {
            outline = scrollRect.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private void StyleButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        bool isCloseButton = button.name.ToLowerInvariant().Contains("exit") || button.name.ToLowerInvariant().Contains("close");
        image.color = isCloseButton
            ? new Color(0.92f, 0.34f, 0.30f, 0.95f)
            : new Color(0.94f, 0.88f, 0.72f, 0.98f);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = isCloseButton
            ? new Color(0.97f, 0.42f, 0.38f, 1f)
            : new Color(0.98f, 0.92f, 0.78f, 1f);
        colors.pressedColor = isCloseButton
            ? new Color(0.82f, 0.26f, 0.24f, 1f)
            : new Color(0.84f, 0.78f, 0.62f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.95f);
        button.colors = colors;

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.25f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private void StyleToggle(Toggle toggle)
    {
        if (toggle == null)
        {
            return;
        }

        Image background = toggle.targetGraphic as Image;
        if (background == null)
        {
            background = toggle.GetComponent<Image>();
        }

        if (background != null)
        {
            background.color = new Color(0.18f, 0.18f, 0.24f, 0.98f);
        }

        if (toggle.graphic is Image checkmark)
        {
            checkmark.color = new Color(0.92f, 0.75f, 0.28f, 1f);
        }
    }

    private void StyleLegacyText(Text text, bool isHeader)
    {
        if (text == null)
        {
            return;
        }

        text.font = fallbackFont;
        if (isHeader)
        {
            text.color = new Color(1f, 0.96f, 0.88f, 1f);
        }
        else
        {
            text.color = text.GetComponentInParent<Button>() != null
                ? Color.black
                : new Color(0.92f, 0.92f, 0.94f, 1f);
        }
    }

    private void StyleTmpText(TMPro.TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        string lowerName = text.name.ToLowerInvariant();
        bool isButtonLabel = text.GetComponentInParent<Button>() != null;
        bool isTitleLike = lowerName.Contains("title") || lowerName.Contains("header");
        bool isProgress = lowerName.Contains("progress");

        if (isButtonLabel)
        {
            text.color = Color.black;
        }
        else if (isTitleLike)
        {
            text.color = new Color(1f, 0.96f, 0.88f, 1f);
        }
        else if (isProgress)
        {
            text.color = new Color(0.92f, 0.75f, 0.28f, 1f);
        }
        else
        {
            text.color = new Color(0.92f, 0.92f, 0.94f, 1f);
        }
    }

    private void StylePanelImage(Image image, GameObject rootPanel, Image cardImage, Image overlayImage)
    {
        if (image == null)
        {
            return;
        }

        if (image == overlayImage || image == cardImage)
        {
            return;
        }

        if (image.GetComponent<Button>() != null)
        {
            return;
        }

        if (image.transform == rootPanel.transform)
        {
            return;
        }

        string lowerName = image.name.ToLowerInvariant();
        if (lowerName.Contains("background") || lowerName.Contains("panel") || lowerName.Contains("viewport"))
        {
            image.color = new Color(0.14f, 0.14f, 0.18f, 0.94f);
        }
    }

    private void RefreshCharacterSelectPanel()
    {
        if (characterListContent == null)
        {
            return;
        }

        SaveFile_Profile profile = SaveManager.Profile;
        EnsureDefaultCharacterUnlocked(profile);

        List<CharacterData> characters = new List<CharacterData>(GameDatabase.Instance.allCharacters);
        characters.Sort(CompareCharactersForMenu);

        if (string.IsNullOrWhiteSpace(profile.characters.equipped_character_id) || !profile.characters.owned_character_ids.Contains(profile.characters.equipped_character_id))
        {
            profile.characters.equipped_character_id = DefaultCharacterId;
        }

        pendingCharacterSelectionId = profile.characters.equipped_character_id;

        foreach (Transform child in characterListContent)
        {
            Destroy(child.gameObject);
        }

        characterButtons.Clear();

        foreach (CharacterData character in characters)
        {
            if (character == null)
            {
                continue;
            }

            bool isUnlocked = profile.characters.owned_character_ids.Contains(character.characterID);
            Button button = CreateButton(character.characterID + "_Button", characterListContent, BuildCharacterLabel(character, isUnlocked));
            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 56f;
            button.interactable = isUnlocked;

            if (isUnlocked)
            {
                string characterId = character.characterID;
                button.onClick.AddListener(() => OnCharacterSelected(characterId));
            }

            characterButtons[character.characterID] = button;
        }

        UpdateCharacterSelectionVisuals();
        SaveManager.SaveProfile();
    }

    private void OnCharacterSelected(string characterId)
    {
        pendingCharacterSelectionId = characterId;
        UpdateCharacterSelectionVisuals();
    }

    private void UpdateCharacterSelectionVisuals()
    {
        SaveFile_Profile profile = SaveManager.Profile;

        foreach (KeyValuePair<string, Button> entry in characterButtons)
        {
            if (entry.Value == null)
            {
                continue;
            }

            bool isUnlocked = profile.characters.owned_character_ids.Contains(entry.Key);
            bool isSelected = entry.Key == pendingCharacterSelectionId;

            ColorBlock colors = entry.Value.colors;
            colors.normalColor = isSelected ? new Color(0.92f, 0.75f, 0.28f, 1f) : new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = isSelected ? new Color(0.98f, 0.82f, 0.36f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.68f, 0.28f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.95f);
            entry.Value.colors = colors;
            entry.Value.interactable = isUnlocked;
        }

        CharacterData selectedCharacter = GameDatabase.Instance.GetCharacterByID(pendingCharacterSelectionId);
        selectedCharacterLabel.text = selectedCharacter != null
            ? L("menu.character_select.selected_format", "Selected: {0} ({1})", selectedCharacter.characterName, selectedCharacter.rarity.ToString())
            : L("menu.character_select.none", "Selected: None");

        if (startRunButton != null)
        {
            startRunButton.interactable = !string.IsNullOrWhiteSpace(pendingCharacterSelectionId)
                && profile.characters.owned_character_ids.Contains(pendingCharacterSelectionId);
        }
    }

    private void EnsureDefaultCharacterUnlocked(SaveFile_Profile profile)
    {
        if (!profile.characters.owned_character_ids.Contains(DefaultCharacterId))
        {
            profile.characters.owned_character_ids.Add(DefaultCharacterId);
        }
    }

    private int CompareCharactersForMenu(CharacterData a, CharacterData b)
    {
        SaveFile_Profile profile = SaveManager.Profile;
        bool aUnlocked = a != null && profile.characters.owned_character_ids.Contains(a.characterID);
        bool bUnlocked = b != null && profile.characters.owned_character_ids.Contains(b.characterID);

        if (aUnlocked != bUnlocked)
        {
            return aUnlocked ? -1 : 1;
        }

        if (a != null && b != null && a.rarity != b.rarity)
        {
            return a.rarity.CompareTo(b.rarity);
        }

        if (a == null || b == null)
        {
            return 0;
        }

        return string.Compare(a.characterName, b.characterName, System.StringComparison.OrdinalIgnoreCase);
    }

    private string BuildCharacterLabel(CharacterData character, bool isUnlocked)
    {
        string status = isUnlocked
            ? L("common.status.unlocked", "Unlocked")
            : L("common.status.locked", "Locked");
        return $"{character.characterName}  [{character.rarity}]  -  {status}";
    }

    private void RefreshLocalizedUi()
    {
        RefreshModernMenuContent();

        if (characterSelectTitleLabel != null)
        {
            characterSelectTitleLabel.text = L("menu.character_select.title", "Select Character");
        }

        SetButtonLabel(startRunButton, L("menu.start_run", "Start Run"));

        SetButtonLabel(characterSelectCloseButton, L("common.back", "Back"));
        RefreshSettingsPanel();
        RefreshGachaPanel();
        RefreshAuthOnboardingPanel();

        if (achievementPanelStyled)
        {
            BeautifyExistingPanel(achievementPanel, GetAchievementsTitle());
        }

        if (challengePanelStyled)
        {
            BeautifyExistingPanel(ChallengePanel, GetChallengesTitle());
        }

        if (missionPanelStyled)
        {
            BeautifyExistingPanel(missionPanel, GetMissionsTitle());
        }

        if (characterListContent != null)
        {
            RefreshCharacterSelectPanel();
        }
    }

    private string GetAchievementsTitle()
    {
        return L("menu.achievements", "Achievements");
    }

    private string GetChallengesTitle()
    {
        return L("menu.challenges", "Challenges");
    }

    private string GetMissionsTitle()
    {
        return L("menu.missions", "Missions");
    }

    private TextMeshProUGUI CreateTmpText(string objectName, Transform parent, string message, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
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

    private GameObject CreateInputRow(Transform parent, string objectName, string placeholder, out TMP_InputField inputField, bool isPassword, Vector2 anchoredPosition)
    {
        GameObject row = CreateUIObject(objectName, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.sizeDelta = new Vector2(-56f, 76f);
        rowRect.anchoredPosition = anchoredPosition;

        inputField = CreateTmpInputField("Input", row.transform, placeholder, isPassword);
        StretchToParent(inputField.GetComponent<RectTransform>(), 0f, 0f);
        return row;
    }

    private TMP_InputField CreateTmpInputField(string objectName, Transform parent, string placeholderText, bool isPassword)
    {
        GameObject inputObject = CreateUIObject(objectName, parent);
        Image background = inputObject.AddComponent<Image>();
        background.color = new Color(0.14f, 0.16f, 0.20f, 1f);
        Outline outline = inputObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        outline.effectDistance = new Vector2(1f, -1f);

        TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();
        inputField.targetGraphic = background;
        inputField.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        inputField.lineType = TMP_InputField.LineType.SingleLine;

        GameObject textArea = CreateUIObject("Text Area", inputObject.transform);
        textArea.AddComponent<RectMask2D>();
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        StretchToParent(textAreaRect, 16f, 12f);

        TextMeshProUGUI textComponent = CreateTmpText("Text", textArea.transform, string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.96f, 0.96f, 0.97f, 1f));
        StretchToParent(textComponent.rectTransform, 0f, 0f);

        TextMeshProUGUI placeholder = CreateTmpText("Placeholder", textArea.transform, placeholderText, 22f, FontStyles.Italic, TextAlignmentOptions.Left, new Color(0.64f, 0.68f, 0.72f, 0.9f));
        StretchToParent(placeholder.rectTransform, 0f, 0f);

        inputField.textViewport = textAreaRect;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholder;
        return inputField;
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

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    private void SetCurrencyButtonLabel(Button button, string prefix, int amount, CurrencyType type)
    {
        if (button == null)
        {
            return;
        }

        SetButtonLabel(button, $"{prefix} - {CurrencyUiUtility.FormatAmount(amount)}");
        ConfigureButtonCurrencyIcon(button, CurrencyUiUtility.GetSprite(type));
    }

    private void SetPullButtonUnavailableState(Button button, bool isAvailable, int amount, CurrencyType type, string prefix)
    {
        if (isAvailable)
        {
            SetCurrencyButtonLabel(button, prefix, amount, type);
            return;
        }

        SetButtonLabel(button, $"{prefix} - {L("menu.gacha.unavailable", "Unavailable")}");
        ConfigureButtonCurrencyIcon(button, null);
    }

    private void ConfigureButtonCurrencyIcon(Button button, Sprite sprite)
    {
        if (button == null)
        {
            return;
        }

        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;

            if (sprite != null)
            {
                labelRect.offsetMin = new Vector2(14f, 12f);
                labelRect.offsetMax = new Vector2(-42f, -12f);
                label.alignment = TextAnchor.MiddleLeft;
            }
            else
            {
                labelRect.offsetMin = new Vector2(12f, 12f);
                labelRect.offsetMax = new Vector2(-12f, -12f);
                label.alignment = TextAnchor.MiddleCenter;
            }
        }

        Image iconImage = GetOrCreateButtonCurrencyIcon(button);
        CurrencyUiUtility.ApplySprite(iconImage, sprite);
    }

    private Image GetOrCreateButtonCurrencyIcon(Button button)
    {
        Transform existing = button.transform.Find("CurrencyIcon");
        if (existing != null && existing.TryGetComponent(out Image existingImage))
        {
            return existingImage;
        }

        Image iconImage = CreateUIObject("CurrencyIcon", button.transform).AddComponent<Image>();
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.sizeDelta = new Vector2(18f, 18f);
        iconRect.anchoredPosition = new Vector2(-14f, 0f);
        iconImage.preserveAspect = true;
        return iconImage;
    }

    private GameObject CreateCurrencyBadge(string objectName, Transform parent, float fontSize, Color textColor, out Image iconImage, out TextMeshProUGUI amountText)
    {
        GameObject badge = CreateUIObject(objectName, parent);
        HorizontalLayoutGroup badgeLayout = badge.AddComponent<HorizontalLayoutGroup>();
        badgeLayout.spacing = 8f;
        badgeLayout.childAlignment = TextAnchor.MiddleLeft;
        badgeLayout.childControlHeight = true;
        badgeLayout.childControlWidth = false;
        badgeLayout.childForceExpandHeight = false;
        badgeLayout.childForceExpandWidth = false;

        ContentSizeFitter badgeFitter = badge.AddComponent<ContentSizeFitter>();
        badgeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        badgeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        iconImage = CreateUIObject("Icon", badge.transform).AddComponent<Image>();
        LayoutElement iconLayout = iconImage.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 24f;
        iconLayout.preferredHeight = 24f;
        iconLayout.minWidth = 24f;
        iconLayout.minHeight = 24f;

        amountText = CreateTmpText("Amount", badge.transform, string.Empty, fontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, textColor);
        amountText.enableWordWrapping = false;
        amountText.overflowMode = TextOverflowModes.Overflow;
        LayoutElement amountLayout = amountText.gameObject.AddComponent<LayoutElement>();
        amountLayout.minWidth = 36f;

        return badge;
    }

    private void RefreshCurrencyBadge(Image iconImage, TextMeshProUGUI amountText, CurrencyType type, int amount)
    {
        CurrencyUiUtility.ApplyCurrencySprite(iconImage, type);

        if (amountText != null)
        {
            amountText.text = CurrencyUiUtility.FormatAmount(amount);
        }
    }

    private void SetCurrencyBadgeWidth(GameObject badge, float preferredWidth)
    {
        if (badge == null)
        {
            return;
        }

        LayoutElement layout = badge.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = badge.AddComponent<LayoutElement>();
        }

        layout.preferredWidth = preferredWidth;
        layout.minWidth = preferredWidth;
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private Text CreateText(string objectName, Transform parent, string message, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = fallbackFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = message;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreateUIObject(objectName, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.95f);
        button.colors = colors;

        Text labelText = CreateText("Label", buttonObject.transform, label, 18, TextAnchor.MiddleCenter);
        labelText.color = Color.black;
        RectTransform labelRect = labelText.rectTransform;
        StretchToParent(labelRect, 12f, 12f);

        return button;
    }

    private void StretchToParent(RectTransform rectTransform, float horizontalPadding = 0f, float verticalPadding = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rectTransform.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }

    private void ApplyGachaButtonStyle(Button button, Color fillColor, Color labelColor, int fontSize, FontStyle fontStyle)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = fillColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = fillColor;
        colors.highlightedColor = Color.Lerp(fillColor, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(fillColor, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.28f, 0.30f, 0.34f, 0.9f);
        button.colors = colors;

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(1f, -1f);

        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.font = fallbackFont;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = labelColor;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(10, fontSize - 6);
            label.resizeTextMaxSize = fontSize;
        }
    }

    private void CreateDecorativeGlow(string objectName, Transform parent, Color color, Vector2 size, Vector2 anchoredPosition, Vector2 anchor)
    {
        GameObject glow = CreateUIObject(objectName, parent);
        Image image = glow.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = glow.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }
}
