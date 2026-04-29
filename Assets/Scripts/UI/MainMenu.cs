using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
#endif

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private GameObject ChallengePanel;
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private string gameplaySceneName = "Demo";
    [Header("Main Menu Art")]
    [SerializeField] private GameObject staticModernMenuRoot;
    [SerializeField] private bool buildModernMenuFallbackAtRuntime = true;
    [SerializeField] private bool enableSunFireParticles = true;
    [SerializeField] private Sprite menuBackgroundSprite;
    [SerializeField] private Sprite[] mainMenuSpriteSheetSprites;
    [SerializeField] private TMP_FontAsset pixelMenuFont;
    [SerializeField] private TMP_FontAsset titleMenuFont;
    [Header("Main Menu Panel Prefabs")]
    [SerializeField] private GameObject characterSelectPanelPrefab;
    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private GameObject gachaPanelPrefab;
    [SerializeField] private GameObject achievementsPanelPrefab;
    [SerializeField] private GameObject challengesPanelPrefab;
    [SerializeField] private GameObject missionsPanelPrefab;

    private const string DefaultCharacterId = "D_Eryndor";
    private enum SettingsSection
    {
        Language,
        Display,
        Controls
    }

    private sealed class ControlBindingEntry
    {
        public string Id;
        public string ActionName;
        public string BindingId;
        public string LabelKey;
        public string LabelFallback;
        public bool Rebindable = true;
        public string StaticBindingKey;
        public string StaticBindingFallback;
    }

    private static readonly ControlBindingEntry[] SettingsControlBindingEntries =
    {
        new ControlBindingEntry { Id = "move_up", ActionName = "Move", BindingId = "e2062cb9-1b15-46a2-838c-2f8d72a0bdd9", LabelKey = "controls.move_up", LabelFallback = "Move Up" },
        new ControlBindingEntry { Id = "move_down", ActionName = "Move", BindingId = "320bffee-a40b-4347-ac70-c210eb8bc73a", LabelKey = "controls.move_down", LabelFallback = "Move Down" },
        new ControlBindingEntry { Id = "move_left", ActionName = "Move", BindingId = "d2581a9b-1d11-4566-b27d-b92aff5fabbc", LabelKey = "controls.move_left", LabelFallback = "Move Left" },
        new ControlBindingEntry { Id = "move_right", ActionName = "Move", BindingId = "fcfe95b8-67b9-4526-84b5-5d0bc98d6400", LabelKey = "controls.move_right", LabelFallback = "Move Right" },
        new ControlBindingEntry { Id = "fire", ActionName = "Fire", BindingId = "05f6913d-c316-48b2-a6bb-e225f14c7960", LabelKey = "controls.attack", LabelFallback = "Attack" },
        new ControlBindingEntry { Id = "dash", ActionName = "Dash", BindingId = "1c04ea5f-b012-41d1-a6f7-02e963b52893", LabelKey = "controls.dash", LabelFallback = "Dash" },
        new ControlBindingEntry { Id = "special", ActionName = "Special", BindingId = "36e52cba-0905-478e-a818-f4bfcb9f3b9a", LabelKey = "controls.special", LabelFallback = "Special" },
        new ControlBindingEntry { Id = "interact", ActionName = "Interact", BindingId = "76b5981b-9579-421c-9462-6e6e3f0414ab", LabelKey = "controls.interact", LabelFallback = "Interact" },
        new ControlBindingEntry { Id = "item1", ActionName = "Item1", BindingId = "c3d33415-964f-4ad2-9b19-3c036a68fe15", LabelKey = "controls.item1", LabelFallback = "Item Slot 1" },
        new ControlBindingEntry { Id = "item2", ActionName = "Item2", BindingId = "d82db456-6990-45ac-92cd-44fe83a18cac", LabelKey = "controls.item2", LabelFallback = "Item Slot 2" },
        new ControlBindingEntry { Id = "item3", ActionName = "Item3", BindingId = "0f395c88-afb8-443c-8719-edc1992c2253", LabelKey = "controls.item3", LabelFallback = "Item Slot 3" },
        new ControlBindingEntry { Id = "pause", LabelKey = "controls.pause", LabelFallback = "Pause", Rebindable = false, StaticBindingKey = "controls.pause.binding", StaticBindingFallback = "Esc" }
    };
#if UNITY_EDITOR
    private const string MenuBackgroundAssetPath = "Assets/UI/MenuBG.jpeg";
    private const string MainMenuSpriteSheetAssetPath = "Assets/UI/main menu sprite sheet.png";
    private const string PixelMenuFontAssetPath = "Assets/Fonts/PixelifySans.asset";
    private const string TitleMenuFontAssetPath = "Assets/Fonts/CinzelDecorative-Bold SDF.asset";
    private const string MainMenuPanelPrefabDirectory = "Assets/UI/MainMenuPanels";
    private const string CharacterSelectPanelPrefabPath = MainMenuPanelPrefabDirectory + "/CharacterSelectPanel.prefab";
    private const string SettingsPanelPrefabPath = MainMenuPanelPrefabDirectory + "/SettingsPanel.prefab";
    private const string GachaPanelPrefabPath = MainMenuPanelPrefabDirectory + "/GachaPanel.prefab";
    private const string AchievementsPanelPrefabPath = MainMenuPanelPrefabDirectory + "/AchievementsPanel.prefab";
    private const string ChallengesPanelPrefabPath = MainMenuPanelPrefabDirectory + "/ChallengesPanel.prefab";
    private const string MissionsPanelPrefabPath = MainMenuPanelPrefabDirectory + "/MissionsPanel.prefab";
#endif

    private GameObject characterSelectPanel;
    private RectTransform characterListContent;
    private TextMeshProUGUI characterSelectTitleLabel;
    private TextMeshProUGUI selectedCharacterLabel;
    private Image characterPreviewImage;
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
    private TextMeshProUGUI menuTitleGlowText;
    private TextMeshProUGUI menuTitleShadowText;
    private TextMeshProUGUI menuSubtitleText;
    private TextMeshProUGUI menuStatusText;
    private TextMeshProUGUI menuCharacterNameText;
    private TextMeshProUGUI menuCharacterRarityText;
    private TextMeshProUGUI menuCharacterHintText;
    private TextMeshProUGUI menuGoldAmountText;
    private TextMeshProUGUI menuOrbAmountText;
    private TextMeshProUGUI menuFooterText;
    private TextMeshProUGUI settingsTitleText;
    private TextMeshProUGUI settingsSubtitleText;
    private TextMeshProUGUI settingsCurrentLanguageText;
    private TextMeshProUGUI settingsDisplayModeText;
    private TextMeshProUGUI settingsControlsHeaderText;
    private TextMeshProUGUI settingsControlsHintText;
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
    private MenuSunFireParticles menuSunFireParticles;
    private Image menuGoldIconImage;
    private Image menuOrbIconImage;
    private Image menuCharacterPreviewImage;
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
    private readonly Dictionary<string, Button> settingsControlBindingButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, TextMeshProUGUI> settingsControlActionLabels = new Dictionary<string, TextMeshProUGUI>();
    private Button settingsWindowedButton;
    private Button settingsFullscreenButton;
    private Button settingsLanguageTabButton;
    private Button settingsDisplayTabButton;
    private Button settingsControlsTabButton;
    private Button menuCharacterPrevButton;
    private Button menuCharacterNextButton;
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
    private bool forceGenerateModernMenuRoot;
    private SettingsSection activeSettingsSection = SettingsSection.Language;
    private GameObject settingsLanguageArea;
    private GameObject settingsDisplayArea;
    private GameObject settingsControlsArea;
    private RectTransform settingsControlsListContent;
    private PlayerControls settingsControls;
    private InputActionRebindingExtensions.RebindingOperation activeSettingsRebindOperation;
    private string activeRebindEntryId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveMainMenuArtReferences();
        ResolveMainMenuPanelPrefabReferences();
    }
#endif

    private void Awake()
    {
        DisablePlayerJoiningInMenu();
    }

    private void Start()
    {
        DisablePlayerJoiningInMenu();
        UINavigationUtility.EnsureEventSystem();
        ResolveTrackedPanelReferences();
        ApplySavedDisplayMode();
        EnsureMenuGachaManager();
        BuildModernLandingMenu();
        EnsureMenuSunFireParticles();
        ;

        if (BackendRuntimeSettings.IsEnabled)
        {
            DevAccountAuthBootstrap.EnsureExists();
            EnsureAuthOnboardingPanel();
            RefreshAuthOnboardingPanel();
            TryShowAuthOnboarding();
        }
    }

    private void DisablePlayerJoiningInMenu()
    {
        PlayerInputManager[] managers = FindObjectsByType<PlayerInputManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            PlayerInputManager manager = managers[i];
            if (manager == null)
            {
                continue;
            }

            manager.DisableJoining();
            manager.enabled = false;
        }
    }

    private void OnEnable()
    {
        UINavigationUtility.EnsureEventSystem();
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
        CancelActiveSettingsRebind();

        if (BackendRuntimeSettings.IsEnabled)
        {
            UnsubscribeAuthEvents();
        }
    }

    private void OnDestroy()
    {
        CancelActiveSettingsRebind();
        if (settingsControls != null)
        {
            settingsControls.Dispose();
            settingsControls = null;
        }
    }

    private void Update()
    {

        if (gachaPanel != null && gachaPanel.activeSelf)
        {
            float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (scroll > 0.05f)
            {
                SelectPreviousMeteor();
            }
            else if (scroll < -0.05f)
            {
                SelectNextMeteor();
            }
        }
    }

    public void OnStartGameButtonPressed()
    {
        OnStartRunFromCharacterSelectPressed();
    }

    public void OnQuitGameButtonPressed()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnAchievementButtonPressed()
    {
        if (!TryShowTrackedPanel(ref achievementPanel, GetAchievementsTitle(), ref achievementPanelStyled, "Achievements"))
        {
            return;
        }

        if (ChallengePanel != null)
        {
            ChallengePanel.SetActive(false);
        }

        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
    }

    public void OnChallengeButtonPressed()
    {
        if (!TryShowTrackedPanel(ref ChallengePanel, GetChallengesTitle(), ref challengePanelStyled, "Challenges"))
        {
            return;
        }

        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
        }

        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
    }

    public void OnExitAchievementPanel()
    {
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
    }

    public void OnExitChallengePanel()
    {
        if (ChallengePanel != null)
        {
            ChallengePanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
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

        RefreshLandingEffectsVisibility();
        ;
    }

    public void OnMissionButtonPressed()
    {
        if (!TryShowTrackedPanel(ref missionPanel, GetMissionsTitle(), ref missionPanelStyled, "Missions"))
        {
            return;
        }

        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
        }

        if (ChallengePanel != null)
        {
            ChallengePanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
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

        RefreshLandingEffectsVisibility();
        ;
    }

    public void onExitMissionPanel()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
    }

    private bool TryShowTrackedPanel(ref GameObject panel, string title, ref bool styledFlag, string panelLabel)
    {
        ResolveTrackedPanelReferences();
        if (panel == null)
        {
            panel = TryInstantiateTrackedPanelPrefab(panelLabel);
        }

        if (panel == null)
        {
            Debug.LogWarning($"[MainMenu] {panelLabel} panel is not assigned and prefab instantiation failed.");
            return false;
        }

        RefreshTrackedPanelChrome(panel, panelLabel);
        PopulateTrackedPanelContent(panel, panelLabel);
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        ;
        return true;
    }

    private void ResolveTrackedPanelReferences()
    {
        if (achievementPanel == null)
        {
            AchievementMenuUI achievementUi = FindFirstObjectByType<AchievementMenuUI>(FindObjectsInactive.Include);
            achievementPanel = achievementUi != null ? achievementUi.gameObject : FindPanelObject("AchievementPanel", "AchievementsPanel", "Achievements");
        }

        if (missionPanel == null)
        {
            MissionMenuUI missionUi = FindFirstObjectByType<MissionMenuUI>(FindObjectsInactive.Include);
            missionPanel = missionUi != null ? missionUi.gameObject : FindPanelObject("MissionPanel", "MissionsPanel", "Missions");
        }

        if (ChallengePanel == null)
        {
            ChallengePanel = FindPanelObject("ChallengePanel", "ChallengesPanel", "Challenges");
        }
    }

    private GameObject InstantiateMenuPanelPrefab(GameObject prefab, string fallbackName)
    {
        if (prefab == null)
        {
            return null;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning($"[MainMenu] Could not instantiate {fallbackName} prefab because no Canvas was found.");
            return null;
        }

        GameObject instance = Instantiate(prefab, canvas.transform, false);
        instance.name = string.IsNullOrWhiteSpace(prefab.name) ? fallbackName : prefab.name;
        instance.SetActive(false);
        return instance;
    }

    private GameObject GetTrackedPanelPrefab(string panelLabel)
    {
        switch (panelLabel)
        {
            case "Achievements":
                return achievementsPanelPrefab;
            case "Challenges":
                return challengesPanelPrefab;
            case "Missions":
                return missionsPanelPrefab;
            default:
                return null;
        }
    }

    private GameObject TryInstantiateTrackedPanelPrefab(string panelLabel)
    {
        GameObject prefab = GetTrackedPanelPrefab(panelLabel);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = InstantiateMenuPanelPrefab(prefab, panelLabel + "Panel");
        if (instance != null)
        {
            Button closeButton = FindComponentAtPath<Button>(instance.transform, "PrettyCard/GeneratedCloseButton");
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() =>
                {
                    instance.SetActive(false);
                    RefreshLandingEffectsVisibility();
                });
            }

            Debug.Log($"[MainMenu] Instantiated {panelLabel} panel from prefab.");
        }

        return instance;
    }

    private bool TryInstantiateCharacterSelectPanelFromPrefab()
    {
        GameObject instance = InstantiateMenuPanelPrefab(characterSelectPanelPrefab, "CharacterSelectPanel");
        if (instance == null)
        {
            return false;
        }

        characterSelectPanel = instance;
        BindCharacterSelectPanelReferences();
        if (characterListContent == null || characterSelectCloseButton == null || startRunButton == null)
        {
            Debug.LogWarning("[MainMenu] CharacterSelectPanel prefab is missing required references. Falling back to generated panel.");
            DestroyMenuObject(characterSelectPanel);
            characterSelectPanel = null;
            return false;
        }

        characterSelectCloseButton.onClick.RemoveAllListeners();
        characterSelectCloseButton.onClick.AddListener(OnCloseCharacterSelectPressed);
        startRunButton.onClick.RemoveAllListeners();
        startRunButton.onClick.AddListener(OnStartRunFromCharacterSelectPressed);
        Debug.Log("[MainMenu] Using CharacterSelectPanel prefab.");
        return true;
    }

    private bool TryInstantiateSettingsPanelFromPrefab()
    {
        GameObject instance = InstantiateMenuPanelPrefab(settingsPanelPrefab, "SettingsPanel");
        if (instance == null)
        {
            return false;
        }

        settingsPanel = instance;
        BindSettingsPanelReferences();
        EnsureSettingsTabLayout();
        BindSettingsPanelReferences();
        if (settingsLanguageListContent == null || settingsCloseButton == null || settingsWindowedButton == null || settingsFullscreenButton == null)
        {
            Debug.LogWarning("[MainMenu] SettingsPanel prefab is missing required references. Falling back to generated panel.");
            DestroyMenuObject(settingsPanel);
            settingsPanel = null;
            return false;
        }

        EnsureSettingsControlsInstance();
        settingsCloseButton.onClick.RemoveAllListeners();
        settingsCloseButton.onClick.AddListener(OnCloseSettingsPressed);
        settingsWindowedButton.onClick.RemoveAllListeners();
        settingsWindowedButton.onClick.AddListener(OnWindowedModePressed);
        settingsFullscreenButton.onClick.RemoveAllListeners();
        settingsFullscreenButton.onClick.AddListener(OnFullscreenModePressed);
        WireSettingsTabButtons();

        IReadOnlyList<string> supportedCodes = LocalizationManager.GetSupportedLanguageCodes();
        settingsLanguageButtons.Clear();
        settingsControlBindingButtons.Clear();
        settingsControlActionLabels.Clear();
        for (int i = 0; i < supportedCodes.Count; i++)
        {
            string code = supportedCodes[i];
            Button button = FindComponentAtPath<Button>(settingsPanel.transform, $"SettingsCard/LanguageArea/LanguageList/{code}_LanguageButton");
            if (button == null)
            {
                button = CreateButton(code + "_LanguageButton", settingsLanguageListContent, LocalizationManager.GetDisplayNameForCode(code));
                LayoutElement buttonLayout = button.gameObject.AddComponent<LayoutElement>();
                buttonLayout.preferredHeight = 52f;
            }

            string capturedCode = code;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnLanguageOptionPressed(capturedCode));
            settingsLanguageButtons[capturedCode] = button;
        }

        EnsureSettingsControlRows();
        SetActiveSettingsSection(activeSettingsSection);
        Debug.Log("[MainMenu] Using SettingsPanel prefab.");
        return true;
    }

    private bool TryInstantiateGachaPanelFromPrefab()
    {
        GameObject instance = InstantiateMenuPanelPrefab(gachaPanelPrefab, "GachaPanel");
        if (instance == null)
        {
            return false;
        }

        gachaPanel = instance;
        BindGachaPanelReferences();
        if (gachaCloseButton == null || gachaPrevButton == null || gachaNextButton == null || gachaSinglePullButton == null || gachaTenPullButton == null)
        {
            Debug.LogWarning("[MainMenu] GachaPanel prefab is missing required references. Falling back to generated panel.");
            DestroyMenuObject(gachaPanel);
            gachaPanel = null;
            return false;
        }

        gachaCloseButton.onClick.RemoveAllListeners();
        gachaCloseButton.onClick.AddListener(OnCloseGachaPressed);
        if (gachaRewardsCloseButton != null)
        {
            gachaRewardsCloseButton.onClick.RemoveAllListeners();
            gachaRewardsCloseButton.onClick.AddListener(OnCloseGachaRewardsPressed);
        }

        gachaPrevButton.onClick.RemoveAllListeners();
        gachaPrevButton.onClick.AddListener(SelectPreviousMeteor);
        gachaNextButton.onClick.RemoveAllListeners();
        gachaNextButton.onClick.AddListener(SelectNextMeteor);
        if (gachaRewardsButton != null)
        {
            gachaRewardsButton.onClick.RemoveAllListeners();
            gachaRewardsButton.onClick.AddListener(OnOpenGachaRewardsPressed);
        }

        gachaSinglePullButton.onClick.RemoveAllListeners();
        gachaSinglePullButton.onClick.AddListener(() => OnCurrentMeteorPullPressed(false));
        gachaTenPullButton.onClick.RemoveAllListeners();
        gachaTenPullButton.onClick.AddListener(() => OnCurrentMeteorPullPressed(true));
        Debug.Log("[MainMenu] Using GachaPanel prefab.");
        return true;
    }

    private void BindCharacterSelectPanelReferences()
    {
        if (characterSelectPanel == null)
        {
            return;
        }

        characterListContent = FindComponentAtPath<RectTransform>(characterSelectPanel.transform, "CharacterSelectCard/CharacterViewport/CharacterListContent");
        characterSelectTitleLabel = FindComponentAtPath<TextMeshProUGUI>(characterSelectPanel.transform, "CharacterSelectCard/Title");
        selectedCharacterLabel = FindComponentAtPath<TextMeshProUGUI>(characterSelectPanel.transform, "CharacterSelectCard/SelectedLabel");
        characterPreviewImage = FindComponentAtPath<Image>(characterSelectPanel.transform, "CharacterSelectCard/PreviewPanel/PreviewImage");
        characterSelectCloseButton = FindComponentAtPath<Button>(characterSelectPanel.transform, "CharacterSelectCard/CloseButton");
        startRunButton = FindComponentAtPath<Button>(characterSelectPanel.transform, "CharacterSelectCard/StartRunButton");
    }

    private void BindSettingsPanelReferences()
    {
        if (settingsPanel == null)
        {
            return;
        }

        settingsTitleText = FindComponentAtPath<TextMeshProUGUI>(settingsPanel.transform, "SettingsCard/Title");
        settingsSubtitleText = FindComponentAtPath<TextMeshProUGUI>(settingsPanel.transform, "SettingsCard/Subtitle");
        settingsCurrentLanguageText = FindComponentAtAnyPath<TextMeshProUGUI>(
            settingsPanel.transform,
            "SettingsCard/LanguageArea/CurrentLanguage",
            "SettingsCard/CurrentLanguage");
        settingsDisplayModeText = FindComponentAtAnyPath<TextMeshProUGUI>(
            settingsPanel.transform,
            "SettingsCard/DisplayArea/DisplayModeLabel",
            "SettingsCard/DisplayModeLabel");
        settingsControlsHeaderText = FindComponentAtAnyPath<TextMeshProUGUI>(
            settingsPanel.transform,
            "SettingsCard/ControlsArea/ControlsHeader");
        settingsControlsHintText = FindComponentAtAnyPath<TextMeshProUGUI>(
            settingsPanel.transform,
            "SettingsCard/ControlsArea/RebindHint");
        settingsLanguageListContent = FindComponentAtAnyPath<RectTransform>(
            settingsPanel.transform,
            "SettingsCard/LanguageArea/LanguageList");
        settingsControlsListContent = FindComponentAtAnyPath<RectTransform>(
            settingsPanel.transform,
            "SettingsCard/ControlsArea/ControlsViewport/ControlsList",
            "SettingsCard/ControlsArea/ControlsList");
        settingsLanguageArea = FindTransformAtAnyPath(settingsPanel.transform, "SettingsCard/LanguageArea")?.gameObject;
        settingsDisplayArea = FindTransformAtAnyPath(settingsPanel.transform, "SettingsCard/DisplayArea")?.gameObject;
        settingsControlsArea = FindTransformAtAnyPath(settingsPanel.transform, "SettingsCard/ControlsArea")?.gameObject;
        settingsLanguageTabButton = FindComponentAtPath<Button>(settingsPanel.transform, "SettingsCard/SectionTabs/LanguageTabButton");
        settingsDisplayTabButton = FindComponentAtPath<Button>(settingsPanel.transform, "SettingsCard/SectionTabs/DisplayTabButton");
        settingsControlsTabButton = FindComponentAtPath<Button>(settingsPanel.transform, "SettingsCard/SectionTabs/ControlsTabButton");
        settingsWindowedButton = FindComponentAtAnyPath<Button>(
            settingsPanel.transform,
            "SettingsCard/DisplayArea/WindowedModeButton",
            "SettingsCard/WindowedModeButton");
        settingsFullscreenButton = FindComponentAtAnyPath<Button>(
            settingsPanel.transform,
            "SettingsCard/DisplayArea/FullscreenModeButton",
            "SettingsCard/FullscreenModeButton");
        settingsCloseButton = FindComponentAtPath<Button>(settingsPanel.transform, "SettingsCard/SettingsCloseButton");
    }

    private void BindGachaPanelReferences()
    {
        if (gachaPanel == null)
        {
            return;
        }

        gachaTitleText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/Title");
        gachaSubtitleText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/Subtitle");
        gachaGoldIconImage = FindComponentAtPath<Image>(gachaPanel.transform, "GachaCard/WalletPanel/GoldWalletBadge/Icon");
        gachaGoldAmountText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/WalletPanel/GoldWalletBadge/Amount");
        gachaOrbIconImage = FindComponentAtPath<Image>(gachaPanel.transform, "GachaCard/WalletPanel/OrbWalletBadge/Icon");
        gachaOrbAmountText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/WalletPanel/OrbWalletBadge/Amount");
        gachaMeteorIndexText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/MeteorIndexPanel/MeteorIndex");
        gachaPrevButton = FindComponentAtPath<Button>(gachaPanel.transform, "GachaCard/PrevMeteorButton");
        gachaNextButton = FindComponentAtPath<Button>(gachaPanel.transform, "GachaCard/NextMeteorButton");
        gachaMeteorPlaceholderImage = FindComponentAtPath<Image>(gachaPanel.transform, "GachaCard/MeteorFrame/MeteorImagePanel");
        gachaMeteorImage = FindComponentAtPath<Image>(gachaPanel.transform, "GachaCard/MeteorFrame/MeteorImagePanel/MeteorSprite");
        gachaMeteorPlaceholderText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/MeteorFrame/MeteorImagePanel/MeteorPlaceholder");
        gachaMeteorNameText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/MeteorFrame/MeteorName");
        gachaMeteorCostText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/MeteorFrame/MeteorCost");
        gachaMeteorRatesText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/MeteorFrame/MeteorRates");
        gachaRewardsButton = FindComponentAtPath<Button>(gachaPanel.transform, "GachaCard/RewardsButton");
        gachaSinglePullButton = FindComponentAtPath<Button>(gachaPanel.transform, "GachaCard/PullButtonRow/SinglePullButton");
        gachaTenPullButton = FindComponentAtPath<Button>(gachaPanel.transform, "GachaCard/PullButtonRow/TenPullButton");
        gachaResultText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/ResultArea/ResultText");
        gachaCloseButton = FindComponentAtPath<Button>(gachaPanel.transform, "GachaCard/GachaCloseButton");
        gachaRewardsPanel = FindTransformAtPath(gachaPanel.transform, "GachaCard/GachaRewardsPanel")?.gameObject;
        gachaRewardsTitleText = FindComponentAtPath<TextMeshProUGUI>(gachaPanel.transform, "GachaCard/GachaRewardsPanel/RewardsPanelTitle");
        gachaRewardsContent = FindComponentAtPath<RectTransform>(gachaPanel.transform, "GachaCard/GachaRewardsPanel/RewardsViewport/RewardsContent");
        gachaRewardsCloseButton = FindComponentAtPath<Button>(gachaPanel.transform, "GachaCard/GachaRewardsPanel/RewardsCloseButton");
    }

    private static Transform FindTransformAtPath(Transform root, string path)
    {
        return root != null ? root.Find(path) : null;
    }

    private static T FindComponentAtPath<T>(Transform root, string path) where T : Component
    {
        Transform transform = FindTransformAtPath(root, path);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static Transform FindTransformAtAnyPath(Transform root, params string[] paths)
    {
        if (root == null || paths == null)
        {
            return null;
        }

        for (int i = 0; i < paths.Length; i++)
        {
            Transform match = FindTransformAtPath(root, paths[i]);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static T FindComponentAtAnyPath<T>(Transform root, params string[] paths) where T : Component
    {
        Transform transform = FindTransformAtAnyPath(root, paths);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private void DestroyMenuObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private GameObject FindPanelObject(params string[] names)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            return null;
        }

        Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < names.Length; i++)
        {
            for (int j = 0; j < children.Length; j++)
            {
                Transform child = children[j];
                if (child == null || !string.Equals(child.name, names[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (child.GetComponent<Button>() != null)
                {
                    continue;
                }

                return child.gameObject;
            }
        }

        return null;
    }

    public void OnCloseCharacterSelectPressed()
    {
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
    }

    public void OnCloseSettingsPressed()
    {
        CancelActiveSettingsRebind();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
    }

    public void OnCloseGachaPressed()
    {
        if (gachaPanel != null)
        {
            gachaPanel.SetActive(false);
        }

        RefreshLandingEffectsVisibility();
        ;
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
        EnsurePendingLandingCharacterSelection();
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

    private void EnsurePendingLandingCharacterSelection()
    {
        SaveFile_Profile profile = SaveManager.Profile;
        if (profile == null || profile.characters == null)
        {
            return;
        }

        EnsureDefaultCharacterUnlocked(profile);

        if (string.IsNullOrWhiteSpace(profile.characters.equipped_character_id) || !profile.characters.owned_character_ids.Contains(profile.characters.equipped_character_id))
        {
            profile.characters.equipped_character_id = DefaultCharacterId;
        }

        if (string.IsNullOrWhiteSpace(pendingCharacterSelectionId) || !profile.characters.owned_character_ids.Contains(pendingCharacterSelectionId))
        {
            pendingCharacterSelectionId = profile.characters.equipped_character_id;
        }
    }

    private void SelectAdjacentLandingCharacter(int direction)
    {
        EnsurePendingLandingCharacterSelection();
        List<CharacterData> availableCharacters = GetSelectableLandingCharacters();
        if (availableCharacters.Count == 0)
        {
            return;
        }

        int currentIndex = 0;
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            if (availableCharacters[i] != null && string.Equals(availableCharacters[i].characterID, pendingCharacterSelectionId, StringComparison.Ordinal))
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = (currentIndex + direction) % availableCharacters.Count;
        if (nextIndex < 0)
        {
            nextIndex += availableCharacters.Count;
        }

        CharacterData selectedCharacter = availableCharacters[nextIndex];
        pendingCharacterSelectionId = selectedCharacter != null ? selectedCharacter.characterID : pendingCharacterSelectionId;
        RefreshModernMenuContent();
    }

    private List<CharacterData> GetSelectableLandingCharacters()
    {
        List<CharacterData> characters = new List<CharacterData>();
        SaveFile_Profile profile = SaveManager.Profile;
        if (GameDatabase.Instance == null || profile == null || profile.characters == null || profile.characters.owned_character_ids == null)
        {
            return characters;
        }

        EnsureDefaultCharacterUnlocked(profile);
        for (int i = 0; i < GameDatabase.Instance.allCharacters.Count; i++)
        {
            CharacterData character = GameDatabase.Instance.allCharacters[i];
            if (character == null || !profile.characters.owned_character_ids.Contains(character.characterID))
            {
                continue;
            }

            characters.Add(character);
        }

        characters.Sort(CompareCharactersForMenu);
        return characters;
    }

    private void EnsureCharacterSelectPanel()
    {
        if (characterSelectPanel != null)
        {
            return;
        }

        if (TryInstantiateCharacterSelectPanelFromPrefab())
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

        characterSelectTitleLabel = CreateTmpText("Title", card.transform, L("menu.character_select.title", "Select Character"), 28f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        RectTransform titleRect = characterSelectTitleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -68f);
        titleRect.offsetMax = new Vector2(-24f, -16f);

        selectedCharacterLabel = CreateTmpText("SelectedLabel", card.transform, L("menu.character_select.none", "Selected: None"), 18f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
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

        ApplyCharacterSelectPanelTheme();
    }

    private void BuildModernLandingMenu()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not initialize the main menu because no Canvas was found.");
            return;
        }

        fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ResolveMainMenuArtReferences();
        fallbackTmpFont = LocalizedFontResolver.ResolveTmpFont(pixelMenuFont != null ? pixelMenuFont : TMP_Settings.defaultFontAsset);

        HideLegacyLandingButtons(canvas.transform);

        if (!forceGenerateModernMenuRoot && TryBindStaticModernMenuRoot(canvas))
        {
            RefreshModernMenuContent();
            return;
        }

        if (modernMenuRoot != null)
        {
            BindModernMenuReferences();
            RefreshModernMenuContent();
            return;
        }

        if (!buildModernMenuFallbackAtRuntime && Application.isPlaying)
        {
            Debug.LogWarning("[MainMenu] Static ModernMenuRoot is not assigned, and runtime menu generation is disabled.");
            return;
        }

        modernMenuRoot = CreateUIObject("ModernMenuRoot", canvas.transform);
        RectTransform rootRect = modernMenuRoot.GetComponent<RectTransform>();
        StretchToParent(rootRect);
        modernMenuRoot.transform.SetAsLastSibling();

        Image background = modernMenuRoot.AddComponent<Image>();
        background.sprite = menuBackgroundSprite;
        background.preserveAspect = true;
        background.color = menuBackgroundSprite != null ? Color.white : new Color(0.05f, 0.02f, 0.08f, 1f);
        background.raycastTarget = false;

        GameObject shade = CreateUIObject("PixelShade", modernMenuRoot.transform);
        Image shadeImage = shade.AddComponent<Image>();
        shadeImage.color = new Color(0f, 0f, 0f, 0.18f);
        shadeImage.raycastTarget = false;
        StretchToParent(shade.GetComponent<RectTransform>());

        GameObject topCurrency = CreateUIObject("TopCurrency", modernMenuRoot.transform);
        RectTransform topCurrencyRect = topCurrency.GetComponent<RectTransform>();
        topCurrencyRect.anchorMin = new Vector2(1f, 1f);
        topCurrencyRect.anchorMax = new Vector2(1f, 1f);
        topCurrencyRect.pivot = new Vector2(1f, 1f);
        topCurrencyRect.sizeDelta = new Vector2(320f, 52f);
        topCurrencyRect.anchoredPosition = new Vector2(-32f, -28f);

        HorizontalLayoutGroup topCurrencyLayout = topCurrency.AddComponent<HorizontalLayoutGroup>();
        topCurrencyLayout.spacing = 18f;
        topCurrencyLayout.childAlignment = TextAnchor.MiddleRight;
        topCurrencyLayout.childControlWidth = false;
        topCurrencyLayout.childControlHeight = true;
        topCurrencyLayout.childForceExpandWidth = false;
        topCurrencyLayout.childForceExpandHeight = false;

        CreatePixelCurrencyBadge("GoldBadge", topCurrency.transform, GetMainMenuSprite(1), GetMainMenuSprite(7), out menuGoldIconImage, out menuGoldAmountText);
        CreatePixelCurrencyBadge("OrbBadge", topCurrency.transform, GetMainMenuSprite(2), GetMainMenuSprite(6), out menuOrbIconImage, out menuOrbAmountText);

        GameObject titleGroup = CreateUIObject("TitleGroup", modernMenuRoot.transform);
        RectTransform titleGroupRect = titleGroup.GetComponent<RectTransform>();
        titleGroupRect.anchorMin = new Vector2(0.5f, 1f);
        titleGroupRect.anchorMax = new Vector2(0.5f, 1f);
        titleGroupRect.pivot = new Vector2(0.5f, 1f);
        titleGroupRect.sizeDelta = new Vector2(900f, 236f);
        titleGroupRect.anchoredPosition = new Vector2(0f, -134f);

        GameObject titleBacking = CreateUIObject("TitleBacking", titleGroup.transform);
        Image titleBackingImage = titleBacking.AddComponent<Image>();
        titleBackingImage.color = new Color32(16, 8, 5, 218);
        titleBackingImage.raycastTarget = false;
        RectTransform titleBackingRect = titleBacking.GetComponent<RectTransform>();
        titleBackingRect.anchorMin = new Vector2(0.5f, 1f);
        titleBackingRect.anchorMax = new Vector2(0.5f, 1f);
        titleBackingRect.pivot = new Vector2(0.5f, 1f);
        titleBackingRect.sizeDelta = new Vector2(760f, 108f);
        titleBackingRect.anchoredPosition = new Vector2(0f, -8f);

        menuTitleGlowText = CreateTitleLayer("TitleGlow", titleGroup.transform, 116f, new Color(0.74f, 0.20f, 0.08f, 0.22f), new Vector2(0f, -7f), 8f);
        menuTitleShadowText = CreateTitleLayer("TitleShadow", titleGroup.transform, 106f, new Color(0.12f, 0.04f, 0.01f, 0.98f), new Vector2(5f, -9f), 8f);
        menuTitleText = CreateTitleLayer("Title", titleGroup.transform, 106f, new Color(0.95f, 0.68f, 0.34f, 1f), Vector2.zero, 8f);
        ApplyTitleGoldGradient(menuTitleText);

        RectTransform titleRect = menuTitleText.rectTransform;

        Outline titleOutline = menuTitleText.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.37f, 0.16f, 0.05f, 0.85f);
        titleOutline.effectDistance = new Vector2(1.4f, -2.2f);

        Shadow titleShadow = menuTitleText.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.05f, 0.01f, 0.00f, 0.92f);
        titleShadow.effectDistance = new Vector2(0f, -5f);

        GameObject titleUnderline = CreateUIObject("TitleUnderline", titleGroup.transform);
        Image titleUnderlineImage = titleUnderline.AddComponent<Image>();
        titleUnderlineImage.color = new Color(0.08f, 0.01f, 0.00f, 0.60f);
        titleUnderlineImage.raycastTarget = false;
        RectTransform underlineRect = titleUnderline.GetComponent<RectTransform>();
        underlineRect.anchorMin = new Vector2(0.5f, 1f);
        underlineRect.anchorMax = new Vector2(0.5f, 1f);
        underlineRect.pivot = new Vector2(0.5f, 0.5f);
        underlineRect.sizeDelta = new Vector2(560f, 5f);
        underlineRect.anchoredPosition = new Vector2(0f, -110f);

        menuSubtitleText = CreateTmpText("Subtitle", titleGroup.transform, string.Empty, 20f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.88f, 0.77f, 0.52f, 0.98f));
        menuSubtitleText.characterSpacing = 1.5f;
        menuSubtitleText.lineSpacing = -8f;
        RectTransform subtitleRect = menuSubtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.sizeDelta = new Vector2(-200f, 66f);
        subtitleRect.anchoredPosition = new Vector2(0f, -118f);

        EnsureLandingCharacterSelectorLayout();

        CreatePixelLandingButton(modernMenuRoot.transform, "StartButton", L("menu.start_run", "Start Run"), string.Empty, OnStartGameButtonPressed, true, GetMainMenuSprite(0), null, new Vector2(0.5f, 0.5f), new Vector2(480f, 122f), new Vector2(0f, -50f), 42f);

        CreatePixelLandingButton(modernMenuRoot.transform, "GachaButton", L("menu.gacha", "Gacha"), string.Empty, OnGachaButtonPressed, false, GetMainMenuSprite(5), GetMainMenuSprite(4), new Vector2(0f, 0.5f), new Vector2(330f, 96f), new Vector2(176f, 90f), 25f);
        CreatePixelLandingButton(modernMenuRoot.transform, "MissionsButton", GetMissionsTitle(), string.Empty, OnMissionButtonPressed, false, GetMainMenuSprite(11), GetMainMenuSprite(10), new Vector2(0f, 0.5f), new Vector2(330f, 96f), new Vector2(176f, -34f), 25f);
        CreatePixelLandingButton(modernMenuRoot.transform, "SettingsButton", L("menu.settings", "Settings"), string.Empty, OnSettingsButtonPressed, false, GetMainMenuSprite(14), GetMainMenuSprite(13), new Vector2(0f, 0.5f), new Vector2(330f, 96f), new Vector2(176f, -158f), 25f);

        CreatePixelLandingButton(modernMenuRoot.transform, "AchievementsButton", GetAchievementsTitle(), string.Empty, OnAchievementButtonPressed, false, GetMainMenuSprite(5), null, new Vector2(1f, 0.5f), new Vector2(330f, 96f), new Vector2(-176f, 90f), 24f);
        CreatePixelLandingButton(modernMenuRoot.transform, "ChallengesButton", GetChallengesTitle(), string.Empty, OnChallengeButtonPressed, false, GetMainMenuSprite(11), null, new Vector2(1f, 0.5f), new Vector2(330f, 96f), new Vector2(-176f, -34f), 24f);

        GameObject footer = CreateUIObject("Footer", modernMenuRoot.transform);
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.sizeDelta = new Vector2(0f, 72f);
        footerRect.anchoredPosition = new Vector2(0f, 22f);

        menuStatusText = CreateTmpText("EquippedStatus", footer.transform, string.Empty, 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.90f, 0.82f, 0.62f, 0.98f));
        menuStatusText.characterSpacing = 2f;
        RectTransform statusRect = menuStatusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.sizeDelta = new Vector2(0f, 34f);
        statusRect.anchoredPosition = Vector2.zero;

        menuFooterText = CreateTmpText("FooterLabel", footer.transform, string.Empty, 16f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.72f, 0.67f, 0.55f, 0.90f));
        menuFooterText.characterSpacing = 2f;
        RectTransform footerLabelRect = menuFooterText.rectTransform;
        footerLabelRect.anchorMin = new Vector2(0f, 0f);
        footerLabelRect.anchorMax = new Vector2(1f, 0f);
        footerLabelRect.pivot = new Vector2(0.5f, 0f);
        footerLabelRect.sizeDelta = new Vector2(0f, 28f);
        footerLabelRect.anchoredPosition = Vector2.zero;

        ApplyLandingLayout();
        RefreshModernMenuContent();
    }

    private bool TryBindStaticModernMenuRoot(Canvas canvas)
    {
        if (staticModernMenuRoot == null && canvas != null)
        {
            Transform savedRoot = canvas.transform.Find("ModernMenuRoot");
            if (savedRoot != null)
            {
                staticModernMenuRoot = savedRoot.gameObject;
            }
        }

        if (staticModernMenuRoot == null)
        {
            return false;
        }

        modernMenuRoot = staticModernMenuRoot;
        modernMenuRoot.transform.SetAsLastSibling();
        BindModernMenuReferences();
        ApplyLandingLayout();
        return true;
    }

    private void BindModernMenuReferences()
    {
        landingButtons.Clear();

        if (modernMenuRoot == null)
        {
            return;
        }

        Transform root = modernMenuRoot.transform;
        Transform titleGroup = root.Find("TitleGroup");
        Transform characterSelector = root.Find("CharacterSelector");
        Transform footer = root.Find("Footer");
        Transform goldBadge = root.Find("TopCurrency/GoldBadge");
        Transform orbBadge = root.Find("TopCurrency/OrbBadge");

        menuTitleGlowText = GetComponentAtPath<TextMeshProUGUI>(titleGroup, "TitleGlow");
        menuTitleShadowText = GetComponentAtPath<TextMeshProUGUI>(titleGroup, "TitleShadow");
        menuTitleText = GetComponentAtPath<TextMeshProUGUI>(titleGroup, "Title");
        menuSubtitleText = GetComponentAtPath<TextMeshProUGUI>(titleGroup, "Subtitle");

        menuStatusText = GetComponentAtPath<TextMeshProUGUI>(footer, "EquippedStatus");
        menuFooterText = GetComponentAtPath<TextMeshProUGUI>(footer, "FooterLabel");
        menuCharacterNameText = GetComponentAtPath<TextMeshProUGUI>(characterSelector, "CharacterName");
        menuCharacterRarityText = GetComponentAtPath<TextMeshProUGUI>(characterSelector, "CharacterRarity");
        menuCharacterHintText = GetComponentAtPath<TextMeshProUGUI>(characterSelector, "CharacterHint");
        menuCharacterPreviewImage = GetComponentAtPath<Image>(characterSelector, "PreviewFrame/PreviewImage");
        menuCharacterPrevButton = GetComponentAtPath<Button>(characterSelector, "PrevCharacterButton");
        menuCharacterNextButton = GetComponentAtPath<Button>(characterSelector, "NextCharacterButton");

        menuGoldIconImage = GetComponentAtPath<Image>(goldBadge, "Icon");
        menuGoldAmountText = GetComponentAtPath<TextMeshProUGUI>(goldBadge, "Amount");
        menuOrbIconImage = GetComponentAtPath<Image>(orbBadge, "Icon");
        menuOrbAmountText = GetComponentAtPath<TextMeshProUGUI>(orbBadge, "Amount");

        if (menuCharacterPrevButton != null)
        {
            menuCharacterPrevButton.onClick.RemoveAllListeners();
            menuCharacterPrevButton.onClick.AddListener(() => SelectAdjacentLandingCharacter(-1));
        }

        if (menuCharacterNextButton != null)
        {
            menuCharacterNextButton.onClick.RemoveAllListeners();
            menuCharacterNextButton.onClick.AddListener(() => SelectAdjacentLandingCharacter(1));
        }

        BindLandingButton("StartButton");
        BindLandingButton("GachaButton");
        BindLandingButton("MissionsButton");
        BindLandingButton("AchievementsButton");
        BindLandingButton("ChallengesButton");
        BindLandingButton("SettingsButton");
        BindLandingButton("QuitGameButton");
    }

    private void ApplyLandingLayout()
    {
        if (modernMenuRoot == null)
        {
            return;
        }

        EnsureLandingCharacterSelectorLayout();
        PositionLandingButton("GachaButton", new Vector2(0.5f, 0.5f), new Vector2(360f, 92f), new Vector2(0f, 192f), new Vector2(0.5f, 0.5f));
        PositionLandingButton("MissionsButton", new Vector2(0.5f, 0.5f), new Vector2(360f, 92f), new Vector2(0f, 80f), new Vector2(0.5f, 0.5f));
        PositionLandingButton("AchievementsButton", new Vector2(0.5f, 0.5f), new Vector2(360f, 92f), new Vector2(0f, -32f), new Vector2(0.5f, 0.5f));
        PositionLandingButton("ChallengesButton", new Vector2(0.5f, 0.5f), new Vector2(360f, 92f), new Vector2(0f, -144f), new Vector2(0.5f, 0.5f));
        PositionLandingButton("SettingsButton", new Vector2(0.5f, 0.5f), new Vector2(360f, 92f), new Vector2(0f, -256f), new Vector2(0.5f, 0.5f));
        PositionLandingButton("QuitGameButton", new Vector2(0.5f, 0.5f), new Vector2(360f, 92f), new Vector2(0f, -368f), new Vector2(0.5f, 0.5f));
        PositionLandingButton("StartButton", new Vector2(1f, 0f), new Vector2(390f, 112f), new Vector2(-48f, 48f), new Vector2(1f, 0f));
    }

    private void PositionLandingButton(string key, Vector2 anchor, Vector2 size, Vector2 anchoredPosition, Vector2 pivot)
    {
        Button button = GetComponentAtPath<Button>(modernMenuRoot.transform, key);
        if (button == null)
        {
            return;
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            return;
        }

        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = pivot;
        buttonRect.sizeDelta = size;
        buttonRect.anchoredPosition = anchoredPosition;
    }

    private void BindLandingButton(string key)
    {
        Button button = GetComponentAtPath<Button>(modernMenuRoot.transform, key);
        if (button != null)
        {
            landingButtons[key] = button;
        }
    }

    private void EnsureMenuSunFireParticles()
    {
        if (!enableSunFireParticles || modernMenuRoot == null)
        {
            return;
        }

        menuSunFireParticles = modernMenuRoot.GetComponent<MenuSunFireParticles>();
        if (menuSunFireParticles == null)
        {
            menuSunFireParticles = modernMenuRoot.AddComponent<MenuSunFireParticles>();
        }

        menuSunFireParticles.enabled = true;
        RefreshLandingEffectsVisibility();
    }

    private void RefreshLandingEffectsVisibility()
    {
        if (menuSunFireParticles == null && modernMenuRoot != null)
        {
            menuSunFireParticles = modernMenuRoot.GetComponent<MenuSunFireParticles>();
        }

        if (menuSunFireParticles == null)
        {
            return;
        }

        menuSunFireParticles.SetParticlesVisible(IsLandingScreenClear());
    }

    private bool IsLandingScreenClear()
    {
        return !IsPanelActive(characterSelectPanel)
            && !IsPanelActive(settingsPanel)
            && !IsPanelActive(gachaPanel)
            && !IsPanelActive(authOnboardingPanel)
            && !IsPanelActive(achievementPanel)
            && !IsPanelActive(ChallengePanel)
            && !IsPanelActive(missionPanel);
    }

    private bool IsPanelActive(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    private T GetComponentAtPath<T>(Transform root, string path) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        Transform child = root.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

#if UNITY_EDITOR
    public static void BakeStaticMainMenuSceneForBatchMode()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        MainMenu menu = FindFirstObjectByType<MainMenu>();
        if (menu == null)
        {
            throw new System.InvalidOperationException("[MainMenu] Could not find a MainMenu component in Assets/Scenes/MainMenu.unity.");
        }

        menu.PatchStaticModernMenuRoot();
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Eclipside/Patch Static Main Menu Root")]
    private static void PatchStaticModernMenuRootMenuItem()
    {
        MainMenu menu = FindEditableMainMenu();
        if (menu == null)
        {
            Debug.LogError("[MainMenu] Could not find a MainMenu component to bake.");
            return;
        }

        menu.PatchStaticModernMenuRoot();
    }

    [MenuItem("Tools/Eclipside/Rebuild Static Main Menu Root")]
    private static void RebuildStaticModernMenuRootMenuItem()
    {
        MainMenu menu = FindEditableMainMenu();
        if (menu == null)
        {
            Debug.LogError("[MainMenu] Could not find a MainMenu component to bake.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Rebuild Static Main Menu Root",
                "This deletes the existing ModernMenuRoot and recreates it from code. Manual edits under ModernMenuRoot will be lost.",
                "Rebuild",
                "Cancel"))
        {
            return;
        }

        menu.RebuildStaticModernMenuRoot();
    }

    [MenuItem("Tools/Eclipside/Export Main Menu Panels As Prefabs")]
    private static void ExportMainMenuPanelsAsPrefabsMenuItem()
    {
        MainMenu menu = FindEditableMainMenu();
        if (menu == null)
        {
            Debug.LogError("[MainMenu] Could not find a MainMenu component to export panels from.");
            return;
        }

        menu.ExportMainMenuPanelsAsPrefabs();
    }

    private static MainMenu FindEditableMainMenu()
    {
        MainMenu menu = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<MainMenu>()
            : null;

        return menu != null ? menu : FindFirstObjectByType<MainMenu>();
    }

    [ContextMenu("Patch Static Modern Menu Root")]
    private void PatchStaticModernMenuRoot()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[MainMenu] Exit play mode before patching the static main menu root.");
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not patch the static main menu because no Canvas was found.");
            return;
        }

        Transform existingRoot = canvas.transform.Find("ModernMenuRoot");
        if (existingRoot == null)
        {
            RebuildStaticModernMenuRoot();
            return;
        }

        Undo.RegisterCompleteObjectUndo(this, "Patch Static Main Menu Root");
        staticModernMenuRoot = existingRoot.gameObject;
        modernMenuRoot = staticModernMenuRoot;
        buildModernMenuFallbackAtRuntime = false;

        BindModernMenuReferences();
        ApplyLandingLayout();
        PatchTitleBacking();
        ApplyTitleGoldGradient(menuTitleText);
        PatchSunFireParticles();
        ConfigurePersistentLandingButtonListeners();

        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(staticModernMenuRoot);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("[MainMenu] Patched existing ModernMenuRoot without deleting manual edits.");
    }

    [ContextMenu("Rebuild Static Modern Menu Root")]
    private void RebuildStaticModernMenuRoot()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[MainMenu] Exit play mode before rebuilding the static main menu root.");
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not rebuild the static main menu because no Canvas was found.");
            return;
        }

        Transform existingRoot = canvas.transform.Find("ModernMenuRoot");
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot.gameObject);
        }

        Undo.RegisterCompleteObjectUndo(this, "Rebuild Static Main Menu Root");
        staticModernMenuRoot = null;
        modernMenuRoot = null;

        bool previousForceGenerate = forceGenerateModernMenuRoot;
        forceGenerateModernMenuRoot = true;
        buildModernMenuFallbackAtRuntime = true;
        BuildModernLandingMenu();
        forceGenerateModernMenuRoot = previousForceGenerate;
        buildModernMenuFallbackAtRuntime = false;

        staticModernMenuRoot = modernMenuRoot;
        BindModernMenuReferences();
        ApplyLandingLayout();
        PatchSunFireParticles();
        ConfigurePersistentLandingButtonListeners();

        EditorUtility.SetDirty(this);
        if (staticModernMenuRoot != null)
        {
            EditorUtility.SetDirty(staticModernMenuRoot);
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("[MainMenu] Rebuilt ModernMenuRoot into the scene and wired persistent button listeners.");
    }

    [ContextMenu("Export Main Menu Panels As Prefabs")]
    private void ExportMainMenuPanelsAsPrefabs()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[MainMenu] Exit play mode before exporting main menu panels.");
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Could not export panels because no Canvas was found.");
            return;
        }

        ResolveMainMenuArtReferences();
        EnsureEditorFolderPath(MainMenuPanelPrefabDirectory);

        bool createdMenuGachaManager = false;
        if (GachaManager.Instance == null)
        {
            EnsureMenuGachaManager();
            createdMenuGachaManager = GachaManager.Instance != null;
        }

        GameObject originalCharacterSelectPanel = characterSelectPanel;
        GameObject originalSettingsPanel = settingsPanel;
        GameObject originalGachaPanel = gachaPanel;
        GameObject originalAchievementPanel = achievementPanel;
        GameObject originalChallengePanel = ChallengePanel;
        GameObject originalMissionPanel = missionPanel;
        bool destroyCharacterSelectPanel = originalCharacterSelectPanel == null;
        bool destroySettingsPanel = originalSettingsPanel == null;
        bool destroyGachaPanel = originalGachaPanel == null;

        EnsureCharacterSelectPanel();
        RefreshCharacterSelectPanel();
        ApplyCharacterSelectPanelTheme();

        EnsureSettingsPanel();
        RefreshSettingsPanel();
        ApplySettingsPanelTheme();

        EnsureGachaPanel();
        RefreshGachaPanel();
        ApplyGachaPanelTheme();

        ResolveTrackedPanelReferences();
        if (achievementPanel == null)
        {
            achievementPanel = TryInstantiateTrackedPanelPrefab("Achievements");
        }

        if (ChallengePanel == null)
        {
            ChallengePanel = TryInstantiateTrackedPanelPrefab("Challenges");
        }

        if (missionPanel == null)
        {
            missionPanel = TryInstantiateTrackedPanelPrefab("Missions");
        }

        bool destroyAchievementPanel = originalAchievementPanel == null && achievementPanel != null;
        bool destroyChallengePanel = originalChallengePanel == null && ChallengePanel != null;
        bool destroyMissionPanel = originalMissionPanel == null && missionPanel != null;

        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
            RefreshTrackedPanelChrome(achievementPanel, "Achievements");
        }

        if (ChallengePanel != null)
        {
            ChallengePanel.SetActive(false);
            RefreshTrackedPanelChrome(ChallengePanel, "Challenges");
        }

        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
            RefreshTrackedPanelChrome(missionPanel, "Missions");
        }

        ExportPanelPrefab(characterSelectPanel, "CharacterSelectPanel.prefab");
        ExportPanelPrefab(settingsPanel, "SettingsPanel.prefab");
        ExportPanelPrefab(gachaPanel, "GachaPanel.prefab");
        ExportPanelPrefab(achievementPanel, "AchievementsPanel.prefab");
        ExportPanelPrefab(ChallengePanel, "ChallengesPanel.prefab");
        ExportPanelPrefab(missionPanel, "MissionsPanel.prefab");

        CleanupTemporaryExportPanel(characterSelectPanel, destroyCharacterSelectPanel);
        CleanupTemporaryExportPanel(settingsPanel, destroySettingsPanel);
        CleanupTemporaryExportPanel(gachaPanel, destroyGachaPanel);
        CleanupTemporaryExportPanel(achievementPanel, destroyAchievementPanel);
        CleanupTemporaryExportPanel(ChallengePanel, destroyChallengePanel);
        CleanupTemporaryExportPanel(missionPanel, destroyMissionPanel);

        characterSelectPanel = originalCharacterSelectPanel;
        settingsPanel = originalSettingsPanel;
        gachaPanel = originalGachaPanel;
        achievementPanel = originalAchievementPanel;
        ChallengePanel = originalChallengePanel;
        missionPanel = originalMissionPanel;

        if (createdMenuGachaManager && GachaManager.Instance != null)
        {
            DestroyImmediate(GachaManager.Instance.gameObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MainMenu] Exported main menu panels to {MainMenuPanelPrefabDirectory}.");
    }

    private void ExportPanelPrefab(GameObject panel, string prefabFileName)
    {
        if (panel == null)
        {
            Debug.LogWarning($"[MainMenu] Skipped exporting {prefabFileName} because the panel was null.");
            return;
        }

        GameObject clone = Instantiate(panel);
        clone.name = Path.GetFileNameWithoutExtension(prefabFileName);
        clone.hideFlags = HideFlags.None;
        clone.transform.SetParent(null, false);
        clone.SetActive(panel.activeSelf);

        string assetPath = $"{MainMenuPanelPrefabDirectory}/{prefabFileName}";
        PrefabUtility.SaveAsPrefabAsset(clone, assetPath);
        DestroyImmediate(clone);
    }

    private void CleanupTemporaryExportPanel(GameObject currentPanel, bool shouldDestroy)
    {
        if (shouldDestroy && currentPanel != null)
        {
            DestroyImmediate(currentPanel);
        }
    }

    private static void EnsureEditorFolderPath(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return;
        }

        string[] segments = assetFolderPath.Split('/');
        string currentPath = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = currentPath + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }

    private void PatchTitleBacking()
    {
        if (modernMenuRoot == null)
        {
            return;
        }

        Transform titleGroup = modernMenuRoot.transform.Find("TitleGroup");
        if (titleGroup == null)
        {
            return;
        }

        Transform existingBacking = titleGroup.Find("TitleBacking");
        GameObject titleBacking = existingBacking != null ? existingBacking.gameObject : null;
        if (titleBacking == null)
        {
            titleBacking = new GameObject("TitleBacking", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(titleBacking, "Add Title Backing");
            titleBacking.transform.SetParent(titleGroup, false);
            titleBacking.transform.SetAsFirstSibling();
        }

        Image titleBackingImage = titleBacking.GetComponent<Image>();
        if (titleBackingImage == null)
        {
            titleBackingImage = Undo.AddComponent<Image>(titleBacking);
        }

        titleBackingImage.color = new Color32(16, 8, 5, 218);
        titleBackingImage.raycastTarget = false;

        RectTransform titleBackingRect = titleBacking.GetComponent<RectTransform>();
        titleBackingRect.anchorMin = new Vector2(0.5f, 1f);
        titleBackingRect.anchorMax = new Vector2(0.5f, 1f);
        titleBackingRect.pivot = new Vector2(0.5f, 1f);
        titleBackingRect.sizeDelta = new Vector2(760f, 108f);
        titleBackingRect.anchoredPosition = new Vector2(0f, -8f);
        titleBacking.transform.SetAsFirstSibling();

        EditorUtility.SetDirty(titleBacking);
    }

    private void PatchSunFireParticles()
    {
        if (!enableSunFireParticles || modernMenuRoot == null)
        {
            return;
        }

        MenuSunFireParticles particles = modernMenuRoot.GetComponent<MenuSunFireParticles>();
        if (particles == null)
        {
            particles = Undo.AddComponent<MenuSunFireParticles>(modernMenuRoot);
        }

        particles.enabled = true;
        EditorUtility.SetDirty(particles);
    }

    private void ConfigurePersistentLandingButtonListeners()
    {
        ConfigurePersistentLandingButtonListener("StartButton", OnStartGameButtonPressed);
        ConfigurePersistentLandingButtonListener("GachaButton", OnGachaButtonPressed);
        ConfigurePersistentLandingButtonListener("MissionsButton", OnMissionButtonPressed);
        ConfigurePersistentLandingButtonListener("AchievementsButton", OnAchievementButtonPressed);
        ConfigurePersistentLandingButtonListener("ChallengesButton", OnChallengeButtonPressed);
        ConfigurePersistentLandingButtonListener("SettingsButton", OnSettingsButtonPressed);
        ConfigurePersistentLandingButtonListener("QuitGameButton", OnQuitGameButtonPressed);
    }

    private void ConfigurePersistentLandingButtonListener(string key, UnityAction callback)
    {
        if (!landingButtons.TryGetValue(key, out Button button) || button == null)
        {
            Debug.LogWarning($"[MainMenu] Could not wire persistent listener because {key} was not found.");
            return;
        }

        button.onClick.RemoveAllListeners();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        UnityEventTools.AddPersistentListener(button.onClick, callback);
        EditorUtility.SetDirty(button);
    }
#endif

    private TextMeshProUGUI CreateTitleLayer(string objectName, Transform parent, float fontSize, Color color, Vector2 offset, float characterSpacing)
    {
        TextMeshProUGUI title = CreateTmpText(objectName, parent, string.Empty, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, color);
        title.characterSpacing = characterSpacing;
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Overflow;
        title.raycastTarget = false;
        title.font = LocalizedFontResolver.ResolveTmpFont(titleMenuFont != null ? titleMenuFont : title.font);

        RectTransform rect = title.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 132f);
        rect.anchoredPosition = offset;
        return title;
    }

    private void ApplyTitleGoldGradient(TextMeshProUGUI title)
    {
        if (title == null)
        {
            return;
        }

        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(
            new Color32(255, 218, 135, 255),
            new Color32(255, 205, 112, 255),
            new Color32(132, 69, 34, 255),
            new Color32(108, 49, 25, 255));
    }

    private Button CreatePixelLandingButton(
        Transform parent,
        string key,
        string title,
        string subtitle,
        UnityEngine.Events.UnityAction callback,
        bool primary,
        Sprite frameSprite,
        Sprite iconSprite,
        Vector2 anchor,
        Vector2 size,
        Vector2 anchoredPosition,
        float titleFontSize)
    {
        GameObject buttonRoot = CreateUIObject(key, parent);
        RectTransform buttonRect = buttonRoot.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;
        buttonRect.anchoredPosition = anchoredPosition;

        Image image = buttonRoot.AddComponent<Image>();
        image.sprite = frameSprite;
        image.color = frameSprite != null ? Color.white : new Color(0.10f, 0.08f, 0.10f, 0.92f);
        image.type = Image.Type.Simple;

        Button button = buttonRoot.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.68f, 1f);
        colors.pressedColor = new Color(0.72f, 0.58f, 0.36f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.38f, 0.38f, 0.38f, 0.75f);
        button.colors = colors;
        button.onClick.AddListener(callback);

        Shadow shadow = buttonRoot.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -4f);

        if (iconSprite != null)
        {
            Image iconImage = CreateUIObject("MenuIcon", buttonRoot.transform).AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = primary ? new Vector2(58f, 58f) : new Vector2(70f, 70f);
            iconRect.anchoredPosition = new Vector2(-34f, 0f);
        }

        TextMeshProUGUI titleText = CreateTmpText("Title", buttonRoot.transform, title, titleFontSize, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.94f, 0.84f, 0.58f, 1f));
        titleText.characterSpacing = primary ? 8f : 2f;
        titleText.enableWordWrapping = true;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform titleRect = titleText.rectTransform;
        StretchToParent(titleRect, primary ? 40f : 34f, primary ? 24f : 18f);

        Shadow titleShadow = titleText.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0.03f, 0.01f, 0.01f, 0.9f);
        titleShadow.effectDistance = new Vector2(0f, -2f);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            TextMeshProUGUI subtitleText = CreateTmpText("Subtitle", buttonRoot.transform, subtitle, 13f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.70f, 0.66f, 0.56f, 0.92f));
            RectTransform subtitleRect = subtitleText.rectTransform;
            subtitleRect.anchorMin = new Vector2(0f, 0f);
            subtitleRect.anchorMax = new Vector2(1f, 0f);
            subtitleRect.pivot = new Vector2(0.5f, 0f);
            subtitleRect.sizeDelta = new Vector2(-52f, 26f);
            subtitleRect.anchoredPosition = new Vector2(0f, 12f);
        }

        landingButtons[key] = button;
        return button;
    }

    private GameObject CreatePixelCurrencyBadge(string objectName, Transform parent, Sprite frameSprite, Sprite iconSprite, out Image iconImage, out TextMeshProUGUI amountText)
    {
        GameObject badge = CreateUIObject(objectName, parent);
        Image frame = badge.AddComponent<Image>();
        frame.sprite = frameSprite;
        frame.color = frameSprite != null ? Color.white : new Color(0.09f, 0.07f, 0.09f, 0.92f);
        frame.type = Image.Type.Simple;

        LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 116f;
        badgeLayout.preferredHeight = 48f;

        iconImage = CreateUIObject("Icon", badge.transform).AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(25f, 25f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);

        amountText = CreateTmpText("Amount", badge.transform, string.Empty, 21f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.95f, 0.84f, 0.55f, 1f));
        amountText.enableWordWrapping = false;
        amountText.characterSpacing = 0f;
        amountText.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform amountRect = amountText.rectTransform;
        amountRect.anchorMin = Vector2.zero;
        amountRect.anchorMax = Vector2.one;
        amountRect.offsetMin = new Vector2(44f, 8f);
        amountRect.offsetMax = new Vector2(-14f, -8f);

        return badge;
    }

    private void RefreshMenuCurrencyBadge(Image iconImage, TextMeshProUGUI amountText, CurrencyType type, int amount, Sprite preferredSprite)
    {
        Sprite spriteToUse = preferredSprite != null ? preferredSprite : CurrencyUiUtility.GetSprite(type);
        CurrencyUiUtility.ApplySprite(iconImage, spriteToUse);

        if (amountText != null)
        {
            amountText.text = CurrencyUiUtility.FormatAmount(amount);
        }
    }

    private Sprite GetMainMenuSprite(int index)
    {
        ResolveMainMenuArtReferences();

        if (mainMenuSpriteSheetSprites == null || index < 0 || index >= mainMenuSpriteSheetSprites.Length)
        {
            return null;
        }

        return mainMenuSpriteSheetSprites[index];
    }

    private void ResolveMainMenuArtReferences()
    {
#if UNITY_EDITOR
        if (menuBackgroundSprite == null)
        {
            menuBackgroundSprite = LoadEditorSprite(MenuBackgroundAssetPath, "MenuBG_0");
        }

        if (pixelMenuFont == null)
        {
            pixelMenuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelMenuFontAssetPath);
        }

        if (titleMenuFont == null)
        {
            titleMenuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleMenuFontAssetPath);
        }

        bool needsSpriteSheet = mainMenuSpriteSheetSprites == null || mainMenuSpriteSheetSprites.Length < 17;
        if (!needsSpriteSheet)
        {
            for (int i = 0; i < 17; i++)
            {
                if (mainMenuSpriteSheetSprites[i] == null)
                {
                    needsSpriteSheet = true;
                    break;
                }
            }
        }

        if (needsSpriteSheet)
        {
            mainMenuSpriteSheetSprites = new Sprite[17];
            for (int i = 0; i < mainMenuSpriteSheetSprites.Length; i++)
            {
                mainMenuSpriteSheetSprites[i] = LoadEditorSprite(MainMenuSpriteSheetAssetPath, $"main menu sprite sheet_{i}");
            }
        }

        LocalizedFontResolver.EnsureEditorLocaleFontFallbacks(pixelMenuFont, titleMenuFont, TMP_Settings.defaultFontAsset);
        ResolveMainMenuPanelPrefabReferences();
#endif
    }

#if UNITY_EDITOR
    private void ResolveMainMenuPanelPrefabReferences()
    {
        bool changed = false;

        if (characterSelectPanelPrefab == null)
        {
            characterSelectPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSelectPanelPrefabPath);
            changed |= characterSelectPanelPrefab != null;
        }

        if (settingsPanelPrefab == null)
        {
            settingsPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPanelPrefabPath);
            changed |= settingsPanelPrefab != null;
        }

        if (gachaPanelPrefab == null)
        {
            gachaPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GachaPanelPrefabPath);
            changed |= gachaPanelPrefab != null;
        }

        if (achievementsPanelPrefab == null)
        {
            achievementsPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AchievementsPanelPrefabPath);
            changed |= achievementsPanelPrefab != null;
        }

        if (challengesPanelPrefab == null)
        {
            challengesPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChallengesPanelPrefabPath);
            changed |= challengesPanelPrefab != null;
        }

        if (missionsPanelPrefab == null)
        {
            missionsPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissionsPanelPrefabPath);
            changed |= missionsPanelPrefab != null;
        }

        if (changed)
        {
            EditorUtility.SetDirty(this);
        }
    }

    private Sprite LoadEditorSprite(string assetPath, string spriteName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
#endif

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

        EnsurePendingLandingCharacterSelection();

        if (menuTitleText != null)
        {
            string title = L("menu.title", "ECLIPSIDE");
            menuTitleText.text = title;

            if (menuTitleGlowText != null)
            {
                menuTitleGlowText.text = title;
            }

            if (menuTitleShadowText != null)
            {
                menuTitleShadowText.text = title;
            }
        }

        if (menuSubtitleText != null)
        {
            menuSubtitleText.text = L("menu.subtitle", "Descend through fractured biomes, shape your loadout, and survive the eclipse.");
        }

        if (menuStatusText != null)
        {
            SaveFile_Profile profile = SaveManager.Profile;
            string selectedCharacterName = DefaultCharacterId;
            CharacterData selectedCharacter = GameDatabase.Instance != null
                ? GameDatabase.Instance.GetCharacterByID(pendingCharacterSelectionId)
                : null;
            if (selectedCharacter != null)
            {
                selectedCharacterName = selectedCharacter.GetDisplayName();
            }

            menuStatusText.text = L("menu.status_selected", "Selected: {0}", selectedCharacterName);
            RefreshMenuCurrencyBadge(menuGoldIconImage, menuGoldAmountText, CurrencyType.Gold, profile.user_profile.gold, GetMainMenuSprite(7));
            RefreshMenuCurrencyBadge(menuOrbIconImage, menuOrbAmountText, CurrencyType.Orb, profile.user_profile.orbs, GetMainMenuSprite(6));
        }

        if (menuFooterText != null)
        {
            menuFooterText.text = L("menu.footer", "A bold run begins with a deliberate choice.");
        }

        RefreshLandingCharacterSelectorVisuals();
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
        SetLandingButtonTexts("QuitGameButton", L("menu.quit", "Quit Game"), L("menu.quit.subtitle", "Exit Eclipside."));
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

        if (TryInstantiateSettingsPanelFromPrefab())
        {
            return;
        }
        Debug.LogError("[MainMenu] SettingsPanel prefab could not be instantiated. Runtime-generated settings panel fallback is disabled.");
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

        EnsureSettingsTabLayout();
        BindSettingsPanelReferences();
        EnsureSettingsControlsInstance();
        EnsureSettingsControlRows();
        WireSettingsTabButtons();

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

        if (settingsControlsHeaderText != null)
        {
            settingsControlsHeaderText.text = L("settings.controls", "Controls");
        }

        SetControlRebindHint(L("settings.controls.rebind_hint", "Select a control and press a new key."));

        SetButtonLabel(settingsCloseButton, L("common.close", "Close"));
        SetButtonLabel(settingsWindowedButton, L("settings.display.windowed", "Windowed"));
        SetButtonLabel(settingsFullscreenButton, L("settings.display.fullscreen", "Fullscreen"));
        SetButtonLabel(settingsLanguageTabButton, L("settings.language", "Language"));
        SetButtonLabel(settingsDisplayTabButton, L("settings.display", "Display"));
        SetButtonLabel(settingsControlsTabButton, L("settings.controls", "Controls"));

        string selectedCode = LocalizationManager.GetCurrentLanguageCode();
        foreach (KeyValuePair<string, Button> entry in settingsLanguageButtons)
        {
            if (entry.Value == null)
            {
                continue;
            }

            SetButtonLabel(entry.Value, LocalizationManager.GetDisplayNameForCode(entry.Key));
            RefreshSettingsOptionButtonStyle(entry.Value, entry.Key == selectedCode);
        }

        RefreshDisplayModeButtonState(settingsWindowedButton, SaveManager.Settings.general.windowed_mode);
        RefreshDisplayModeButtonState(settingsFullscreenButton, !SaveManager.Settings.general.windowed_mode);
        RefreshControlBindingLabels();
        SetActiveSettingsSection(activeSettingsSection);
        ApplyMenuModalTheme(settingsPanel, "SettingsCard", settingsTitleText, settingsSubtitleText);
        ApplySettingsPanelTheme();
    }

    private void RefreshDisplayModeButtonState(Button button, bool isSelected)
    {
        if (button == null)
        {
            return;
        }

        RefreshSettingsOptionButtonStyle(button, isSelected);
    }

    private void EnsureSettingsControlsInstance()
    {
        if (settingsControls != null)
        {
            return;
        }

        settingsControls = new PlayerControls();
        PlayerControlBindingOverrides.ApplySavedOverrides(settingsControls);
    }

    private void WireSettingsTabButtons()
    {
        if (settingsLanguageTabButton != null)
        {
            settingsLanguageTabButton.onClick.RemoveAllListeners();
            settingsLanguageTabButton.onClick.AddListener(() => SetActiveSettingsSection(SettingsSection.Language));
        }

        if (settingsDisplayTabButton != null)
        {
            settingsDisplayTabButton.onClick.RemoveAllListeners();
            settingsDisplayTabButton.onClick.AddListener(() => SetActiveSettingsSection(SettingsSection.Display));
        }

        if (settingsControlsTabButton != null)
        {
            settingsControlsTabButton.onClick.RemoveAllListeners();
            settingsControlsTabButton.onClick.AddListener(() => SetActiveSettingsSection(SettingsSection.Controls));
        }
    }

    private void EnsureSettingsTabLayout()
    {
        if (settingsPanel == null)
        {
            return;
        }

        Transform cardTransform = settingsPanel.transform.Find("SettingsCard");
        if (cardTransform == null)
        {
            return;
        }

        settingsLanguageArea = cardTransform.Find("LanguageArea")?.gameObject;
        settingsDisplayArea = cardTransform.Find("DisplayArea")?.gameObject;
        settingsControlsArea = cardTransform.Find("ControlsArea")?.gameObject;

        if (settingsDisplayArea == null)
        {
            settingsDisplayArea = CreateUIObject("DisplayArea", cardTransform).gameObject;
        }

        Image displayAreaImage = settingsDisplayArea.GetComponent<Image>();
        if (displayAreaImage == null)
        {
            displayAreaImage = settingsDisplayArea.AddComponent<Image>();
        }
        displayAreaImage.color = new Color(0.13f, 0.16f, 0.19f, 0.98f);

        RectTransform displayAreaRect = settingsDisplayArea.GetComponent<RectTransform>();
        displayAreaRect.anchorMin = new Vector2(0f, 0f);
        displayAreaRect.anchorMax = new Vector2(1f, 1f);
        displayAreaRect.offsetMin = new Vector2(32f, 176f);
        displayAreaRect.offsetMax = new Vector2(-32f, -234f);

        if (settingsCurrentLanguageText != null && settingsLanguageArea != null)
        {
            settingsCurrentLanguageText.transform.SetParent(settingsLanguageArea.transform, false);
            RectTransform currentLanguageRect = settingsCurrentLanguageText.rectTransform;
            currentLanguageRect.anchorMin = new Vector2(0f, 1f);
            currentLanguageRect.anchorMax = new Vector2(1f, 1f);
            currentLanguageRect.pivot = new Vector2(0f, 1f);
            currentLanguageRect.sizeDelta = new Vector2(-40f, 34f);
            currentLanguageRect.anchoredPosition = new Vector2(20f, -18f);
        }

        if (settingsDisplayModeText != null)
        {
            settingsDisplayModeText.transform.SetParent(settingsDisplayArea.transform, false);
            RectTransform displayModeRect = settingsDisplayModeText.rectTransform;
            displayModeRect.anchorMin = new Vector2(0f, 1f);
            displayModeRect.anchorMax = new Vector2(1f, 1f);
            displayModeRect.pivot = new Vector2(0f, 1f);
            displayModeRect.sizeDelta = new Vector2(-40f, 34f);
            displayModeRect.anchoredPosition = new Vector2(20f, -18f);
        }

        if (settingsWindowedButton != null)
        {
            settingsWindowedButton.transform.SetParent(settingsDisplayArea.transform, false);
            RectTransform windowedRect = settingsWindowedButton.GetComponent<RectTransform>();
            windowedRect.anchorMin = new Vector2(0f, 1f);
            windowedRect.anchorMax = new Vector2(0f, 1f);
            windowedRect.pivot = new Vector2(0f, 1f);
            windowedRect.sizeDelta = new Vector2(200f, 50f);
            windowedRect.anchoredPosition = new Vector2(20f, -72f);
        }

        if (settingsFullscreenButton != null)
        {
            settingsFullscreenButton.transform.SetParent(settingsDisplayArea.transform, false);
            RectTransform fullscreenRect = settingsFullscreenButton.GetComponent<RectTransform>();
            fullscreenRect.anchorMin = new Vector2(0f, 1f);
            fullscreenRect.anchorMax = new Vector2(0f, 1f);
            fullscreenRect.pivot = new Vector2(0f, 1f);
            fullscreenRect.sizeDelta = new Vector2(200f, 50f);
            fullscreenRect.anchoredPosition = new Vector2(232f, -72f);
        }

        if (settingsControlsArea == null)
        {
            settingsControlsArea = CreateUIObject("ControlsArea", cardTransform).gameObject;
        }

        Image controlsAreaImage = settingsControlsArea.GetComponent<Image>();
        if (controlsAreaImage == null)
        {
            controlsAreaImage = settingsControlsArea.AddComponent<Image>();
        }
        controlsAreaImage.color = new Color(0.13f, 0.16f, 0.19f, 0.98f);

        RectTransform controlsAreaRect = settingsControlsArea.GetComponent<RectTransform>();
        controlsAreaRect.anchorMin = new Vector2(0f, 0f);
        controlsAreaRect.anchorMax = new Vector2(1f, 1f);
        controlsAreaRect.offsetMin = new Vector2(32f, 176f);
        controlsAreaRect.offsetMax = new Vector2(-32f, -234f);

        settingsControlsHeaderText = FindComponentAtPath<TextMeshProUGUI>(settingsPanel.transform, "SettingsCard/ControlsArea/ControlsHeader");
        if (settingsControlsHeaderText == null)
        {
            settingsControlsHeaderText = CreateTmpText("ControlsHeader", settingsControlsArea.transform, string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.96f, 0.95f, 0.90f, 1f));
        }
        RectTransform controlsHeaderRect = settingsControlsHeaderText.rectTransform;
        controlsHeaderRect.anchorMin = new Vector2(0f, 1f);
        controlsHeaderRect.anchorMax = new Vector2(1f, 1f);
        controlsHeaderRect.pivot = new Vector2(0f, 1f);
        controlsHeaderRect.sizeDelta = new Vector2(-40f, 36f);
        controlsHeaderRect.anchoredPosition = new Vector2(20f, -18f);

        settingsControlsHintText = FindComponentAtPath<TextMeshProUGUI>(settingsPanel.transform, "SettingsCard/ControlsArea/RebindHint");
        if (settingsControlsHintText == null)
        {
            settingsControlsHintText = CreateTmpText("RebindHint", settingsControlsArea.transform, string.Empty, 17f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.74f, 0.82f, 0.86f, 1f));
        }
        RectTransform controlsHintRect = settingsControlsHintText.rectTransform;
        controlsHintRect.anchorMin = new Vector2(0f, 1f);
        controlsHintRect.anchorMax = new Vector2(1f, 1f);
        controlsHintRect.pivot = new Vector2(0f, 1f);
        controlsHintRect.sizeDelta = new Vector2(-40f, 42f);
        controlsHintRect.anchoredPosition = new Vector2(20f, -56f);

        RectTransform controlsViewportRect = FindComponentAtPath<RectTransform>(settingsPanel.transform, "SettingsCard/ControlsArea/ControlsViewport");
        if (controlsViewportRect == null)
        {
            GameObject controlsViewport = CreateUIObject("ControlsViewport", settingsControlsArea.transform);
            Image controlsViewportImage = controlsViewport.AddComponent<Image>();
            controlsViewportImage.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);
            Mask controlsViewportMask = controlsViewport.AddComponent<Mask>();
            controlsViewportMask.showMaskGraphic = true;

            controlsViewportRect = controlsViewport.GetComponent<RectTransform>();
            controlsViewportRect.anchorMin = new Vector2(0f, 0f);
            controlsViewportRect.anchorMax = new Vector2(1f, 1f);
            controlsViewportRect.offsetMin = new Vector2(18f, 18f);
            controlsViewportRect.offsetMax = new Vector2(-18f, -112f);

            ScrollRect controlsScrollRect = controlsViewport.AddComponent<ScrollRect>();
            controlsScrollRect.horizontal = false;
            controlsScrollRect.vertical = true;
            controlsScrollRect.scrollSensitivity = 30f;
            controlsScrollRect.movementType = ScrollRect.MovementType.Clamped;
            controlsScrollRect.viewport = controlsViewportRect;

            Transform existingContentTransform = settingsControlsArea.transform.Find("ControlsList");
            if (existingContentTransform != null)
            {
                existingContentTransform.SetParent(controlsViewport.transform, false);
            }
        }

        settingsControlsListContent = FindComponentAtPath<RectTransform>(settingsPanel.transform, "SettingsCard/ControlsArea/ControlsViewport/ControlsList");
        if (settingsControlsListContent == null)
        {
            GameObject controlsList = CreateUIObject("ControlsList", controlsViewportRect.transform);
            settingsControlsListContent = controlsList.GetComponent<RectTransform>();
        }

        settingsControlsListContent.SetParent(controlsViewportRect.transform, false);
        settingsControlsListContent.anchorMin = new Vector2(0f, 1f);
        settingsControlsListContent.anchorMax = new Vector2(1f, 1f);
        settingsControlsListContent.pivot = new Vector2(0.5f, 1f);
        settingsControlsListContent.anchoredPosition = Vector2.zero;
        settingsControlsListContent.sizeDelta = new Vector2(0f, settingsControlsListContent.sizeDelta.y);

        ScrollRect existingControlsScrollRect = controlsViewportRect.GetComponent<ScrollRect>();
        if (existingControlsScrollRect == null)
        {
            existingControlsScrollRect = controlsViewportRect.gameObject.AddComponent<ScrollRect>();
        }

        existingControlsScrollRect.viewport = controlsViewportRect;
        existingControlsScrollRect.content = settingsControlsListContent;
        existingControlsScrollRect.horizontal = false;
        existingControlsScrollRect.vertical = true;
        existingControlsScrollRect.scrollSensitivity = 30f;
        existingControlsScrollRect.movementType = ScrollRect.MovementType.Clamped;

        VerticalLayoutGroup listLayout = settingsControlsListContent.GetComponent<VerticalLayoutGroup>();
        if (listLayout == null)
        {
            listLayout = settingsControlsListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        listLayout.spacing = 10f;
        listLayout.padding = new RectOffset(0, 0, 0, 0);
        listLayout.childControlHeight = true;
        listLayout.childControlWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.childForceExpandWidth = true;

        ContentSizeFitter listFitter = settingsControlsListContent.GetComponent<ContentSizeFitter>();
        if (listFitter == null)
        {
            listFitter = settingsControlsListContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        Transform tabsTransform = cardTransform.Find("SectionTabs");
        GameObject tabsObject = tabsTransform != null ? tabsTransform.gameObject : CreateUIObject("SectionTabs", cardTransform);
        RectTransform tabsRect = tabsObject.GetComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0f, 1f);
        tabsRect.anchorMax = new Vector2(1f, 1f);
        tabsRect.pivot = new Vector2(0.5f, 1f);
        tabsRect.sizeDelta = new Vector2(-64f, 52f);
        tabsRect.anchoredPosition = new Vector2(0f, -180f);

        HorizontalLayoutGroup tabsLayout = tabsObject.GetComponent<HorizontalLayoutGroup>();
        if (tabsLayout == null)
        {
            tabsLayout = tabsObject.AddComponent<HorizontalLayoutGroup>();
        }
        tabsLayout.spacing = 10f;
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childForceExpandHeight = true;

        settingsLanguageTabButton = FindComponentAtPath<Button>(settingsPanel.transform, "SettingsCard/SectionTabs/LanguageTabButton");
        if (settingsLanguageTabButton == null)
        {
            settingsLanguageTabButton = CreateButton("LanguageTabButton", tabsObject.transform, L("settings.language", "Language"));
        }

        settingsDisplayTabButton = FindComponentAtPath<Button>(settingsPanel.transform, "SettingsCard/SectionTabs/DisplayTabButton");
        if (settingsDisplayTabButton == null)
        {
            settingsDisplayTabButton = CreateButton("DisplayTabButton", tabsObject.transform, L("settings.display", "Display"));
        }

        settingsControlsTabButton = FindComponentAtPath<Button>(settingsPanel.transform, "SettingsCard/SectionTabs/ControlsTabButton");
        if (settingsControlsTabButton == null)
        {
            settingsControlsTabButton = CreateButton("ControlsTabButton", tabsObject.transform, L("settings.controls", "Controls"));
        }
    }

    private void EnsureSettingsControlRows()
    {
        if (settingsControlsListContent == null)
        {
            return;
        }

        EnsureSettingsControlsInstance();

        for (int i = 0; i < SettingsControlBindingEntries.Length; i++)
        {
            ControlBindingEntry entry = SettingsControlBindingEntries[i];
            Transform rowTransform = settingsControlsListContent.Find(entry.Id + "_Row");
            GameObject rowObject = rowTransform != null ? rowTransform.gameObject : CreateUIObject(entry.Id + "_Row", settingsControlsListContent);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 56f);

            HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout == null)
            {
                rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            }
            rowLayout.spacing = 16f;
            rowLayout.padding = new RectOffset(14, 14, 6, 6);
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            Image rowImage = rowObject.GetComponent<Image>();
            if (rowImage == null)
            {
                rowImage = rowObject.AddComponent<Image>();
            }
            rowImage.color = new Color(0.08f, 0.11f, 0.14f, 0.94f);

            TextMeshProUGUI actionLabel = FindComponentAtPath<TextMeshProUGUI>(rowObject.transform, "ActionLabel");
            if (actionLabel == null)
            {
                actionLabel = CreateTmpText("ActionLabel", rowObject.transform, string.Empty, 17f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.94f, 0.92f, 0.87f, 1f));
            }
            LayoutElement actionLayout = actionLabel.GetComponent<LayoutElement>();
            if (actionLayout == null)
            {
                actionLayout = actionLabel.gameObject.AddComponent<LayoutElement>();
            }
            actionLayout.preferredWidth = 280f;
            actionLayout.flexibleWidth = 1f;
            settingsControlActionLabels[entry.Id] = actionLabel;

            Button bindingButton = FindComponentAtPath<Button>(rowObject.transform, "RebindButton");
            if (bindingButton == null)
            {
                bindingButton = CreateButton("RebindButton", rowObject.transform, string.Empty);
            }
            LayoutElement buttonLayout = bindingButton.GetComponent<LayoutElement>();
            if (buttonLayout == null)
            {
                buttonLayout = bindingButton.gameObject.AddComponent<LayoutElement>();
            }
            buttonLayout.preferredWidth = 300f;
            buttonLayout.minWidth = 260f;
            buttonLayout.flexibleWidth = 0f;

            string capturedId = entry.Id;
            bindingButton.onClick.RemoveAllListeners();
            if (entry.Rebindable)
            {
                bindingButton.onClick.AddListener(() => BeginControlRebind(capturedId));
            }
            bindingButton.interactable = entry.Rebindable;
            settingsControlBindingButtons[entry.Id] = bindingButton;
        }

        RefreshControlBindingLabels();
    }

    private void SetActiveSettingsSection(SettingsSection section)
    {
        activeSettingsSection = section;

        if (settingsLanguageArea != null)
        {
            settingsLanguageArea.SetActive(section == SettingsSection.Language);
        }

        if (settingsDisplayArea != null)
        {
            settingsDisplayArea.SetActive(section == SettingsSection.Display);
        }

        if (settingsControlsArea != null)
        {
            settingsControlsArea.SetActive(section == SettingsSection.Controls);
        }

        RefreshSettingsOptionButtonStyle(settingsLanguageTabButton, section == SettingsSection.Language);
        RefreshSettingsOptionButtonStyle(settingsDisplayTabButton, section == SettingsSection.Display);
        RefreshSettingsOptionButtonStyle(settingsControlsTabButton, section == SettingsSection.Controls);

        if (section == SettingsSection.Controls)
        {
            RefreshControlBindingLabels();
        }
    }

    private void BeginControlRebind(string entryId)
    {
        EnsureSettingsControlsInstance();
        ControlBindingEntry entry = FindControlBindingEntry(entryId);
        if (entry == null || !entry.Rebindable)
        {
            return;
        }

        InputAction action = settingsControls.FindAction(entry.ActionName, false);
        if (action == null)
        {
            return;
        }

        int bindingIndex = FindBindingIndexById(action, entry.BindingId);
        if (bindingIndex < 0)
        {
            return;
        }

        CancelActiveSettingsRebind();
        activeRebindEntryId = entryId;
        SetControlRebindHint(L("settings.controls.rebinding", "Listening... press a key (Esc to cancel)."));

        action.Disable();
        activeSettingsRebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation =>
            {
                FinalizeSettingsRebind(action, operation, false, L("settings.controls.rebind_hint", "Select a control and press a new key."));
            })
            .OnComplete(operation =>
            {
                FinalizeSettingsRebind(action, operation, true, L("settings.controls.saved", "Control updated."));
            });

        activeSettingsRebindOperation.Start();
        RefreshControlBindingLabels();
    }

    private void CancelActiveSettingsRebind()
    {
        if (activeSettingsRebindOperation == null)
        {
            return;
        }

        InputActionRebindingExtensions.RebindingOperation operation = activeSettingsRebindOperation;
        activeSettingsRebindOperation = null;
        activeRebindEntryId = null;
        SetControlRebindHint(L("settings.controls.rebind_hint", "Select a control and press a new key."));
        operation.Cancel();
    }

    private void RefreshControlBindingLabels()
    {
        EnsureSettingsControlsInstance();

        for (int i = 0; i < SettingsControlBindingEntries.Length; i++)
        {
            ControlBindingEntry entry = SettingsControlBindingEntries[i];
            if (settingsControlActionLabels.TryGetValue(entry.Id, out TextMeshProUGUI actionLabel) && actionLabel != null)
            {
                actionLabel.text = L(entry.LabelKey, entry.LabelFallback);
            }

            if (settingsControlBindingButtons.TryGetValue(entry.Id, out Button button) && button != null)
            {
                bool isListening = entry.Rebindable && string.Equals(activeRebindEntryId, entry.Id, StringComparison.Ordinal);
                SetButtonLabel(button, isListening
                    ? L("settings.controls.listening", "Listening...")
                    : GetBindingDisplayString(entry));
                RefreshSettingsOptionButtonStyle(button, entry.Rebindable && isListening);
                button.interactable = entry.Rebindable;
            }
        }
    }

    private string GetBindingDisplayString(ControlBindingEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (!entry.Rebindable)
        {
            return L(entry.StaticBindingKey, entry.StaticBindingFallback);
        }

        if (settingsControls == null)
        {
            return L("settings.controls.unbound", "Unbound");
        }

        InputAction action = settingsControls.FindAction(entry.ActionName, false);
        if (action == null)
        {
            return L("settings.controls.unbound", "Unbound");
        }

        int bindingIndex = FindBindingIndexById(action, entry.BindingId);
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return L("settings.controls.unbound", "Unbound");
        }

        InputBinding binding = action.bindings[bindingIndex];
        string path = string.IsNullOrWhiteSpace(binding.effectivePath) ? binding.path : binding.effectivePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return L("settings.controls.unbound", "Unbound");
        }

        return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    private static int FindBindingIndexById(InputAction action, string bindingId)
    {
        if (action == null || string.IsNullOrWhiteSpace(bindingId))
        {
            return -1;
        }

        if (!Guid.TryParse(bindingId, out Guid guid))
        {
            return -1;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].id == guid)
            {
                return i;
            }
        }

        return -1;
    }

    private static ControlBindingEntry FindControlBindingEntry(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return null;
        }

        for (int i = 0; i < SettingsControlBindingEntries.Length; i++)
        {
            ControlBindingEntry entry = SettingsControlBindingEntries[i];
            if (string.Equals(entry.Id, entryId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private void SetControlRebindHint(string value)
    {
        if (settingsControlsHintText != null)
        {
            settingsControlsHintText.text = value;
        }
    }

    private void FinalizeSettingsRebind(
        InputAction action,
        InputActionRebindingExtensions.RebindingOperation operation,
        bool saveOverride,
        string hintText)
    {
        if (action != null)
        {
            action.Enable();
        }

        if (ReferenceEquals(activeSettingsRebindOperation, operation))
        {
            activeSettingsRebindOperation = null;
        }

        activeRebindEntryId = null;
        operation?.Dispose();

        if (saveOverride)
        {
            PlayerControlBindingOverrides.SaveOverrides(settingsControls);
        }

        SetControlRebindHint(hintText);
        RefreshControlBindingLabels();
    }

    private void EnsureGachaPanel()
    {
        if (gachaPanel != null)
        {
            return;
        }

        if (TryInstantiateGachaPanelFromPrefab())
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

        ApplyMenuModalTheme(gachaPanel, "GachaCard", gachaTitleText, gachaSubtitleText);
        ApplyGachaPanelTheme();
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

        ApplyMenuModalTheme(gachaPanel, "GachaCard", gachaTitleText, gachaSubtitleText);
        ApplyGachaPanelTheme();

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

        SetCurrencyButtonLabel(gachaSinglePullButton, L("menu.gacha.single_pull", "1 Pull"), banner.singlePullCost, banner.currencyType);
        SetPullButtonUnavailableState(
            gachaTenPullButton,
            banner.tenPullCost > 0,
            banner.tenPullCost,
            banner.currencyType,
            L("menu.gacha.ten_pull", "10 Pull"));

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
            SetButtonLabel(gachaSinglePullButton, L("menu.gacha.single_pull", "1 Pull"));
            ConfigureButtonCurrencyIcon(gachaSinglePullButton, null);
        }

        if (gachaTenPullButton != null)
        {
            gachaTenPullButton.interactable = false;
            SetButtonLabel(gachaTenPullButton, L("menu.gacha.ten_pull", "10 Pull"));
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
            return reward.characterReference.GetDisplayName();
        }

        if ((reward.type == RewardType.Weapon || reward.type == RewardType.Consumable) && reward.itemReference != null)
        {
            return reward.itemReference.GetDisplayName();
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

        RefreshLandingEffectsVisibility();
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

            LocalizedFontResolver.ApplyTo(label, fallbackTmpFont);

            if (label.name == "Title")
            {
                label.text = title;
            }
            else if (label.name == "Subtitle")
            {
                label.text = subtitle;
            }
        }

        Text[] legacyLabels = button.GetComponentsInChildren<Text>(true);
        foreach (Text legacyLabel in legacyLabels)
        {
            if (legacyLabel == null)
            {
                continue;
            }

            LocalizedFontResolver.ApplyTo(legacyLabel, fallbackFont);

            if (legacyLabel.name == "Title")
            {
                legacyLabel.text = title;
            }
            else if (legacyLabel.name == "Subtitle")
            {
                legacyLabel.text = subtitle;
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
        TextMeshProUGUI header = null;
        if (headerTransform != null)
        {
            header = headerTransform.GetComponent<TextMeshProUGUI>();
            if (header == null)
            {
                header = EnsureTmpTextComponent(headerTransform.gameObject) as TextMeshProUGUI;
                if (header == null)
                {
                    header = headerTransform.gameObject.AddComponent<TextMeshProUGUI>();
                }
            }
        }
        else
        {
            header = CreateTmpText("PrettyHeader", panel.transform, title, 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.96f, 0.88f, 1f));
        }
        LocalizedFontResolver.ApplyTo(header, fallbackTmpFont);
        header.fontStyle = FontStyles.Bold;
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

        foreach (TMPro.TextMeshProUGUI tmp in panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            StyleTmpText(tmp, tmp == header);
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

    private void StyleTmpText(TMPro.TextMeshProUGUI text, bool isHeader = false)
    {
        if (text == null)
        {
            return;
        }

        LocalizedFontResolver.ApplyTo(text, fallbackTmpFont);
        string lowerName = text.name.ToLowerInvariant();
        bool isButtonLabel = text.GetComponentInParent<Button>() != null;
        bool isTitleLike = isHeader || lowerName.Contains("title") || lowerName.Contains("header");
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
        EnsureCharacterPreviewLayout();

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

        ApplyCharacterSelectPanelTheme();
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
        if (profile == null || profile.characters == null || profile.characters.owned_character_ids == null)
        {
            if (selectedCharacterLabel != null)
            {
                selectedCharacterLabel.text = L("menu.character_select.none", "Selected: None");
            }

            if (startRunButton != null)
            {
                startRunButton.interactable = false;
                StyleCharacterSelectOptionButton(startRunButton, true, false);
            }

            if (characterSelectCloseButton != null)
            {
                StyleCharacterSelectOptionButton(characterSelectCloseButton, false, true, true);
            }

            RefreshCharacterPreviewSprite(null);

            return;
        }

        foreach (KeyValuePair<string, Button> entry in characterButtons)
        {
            if (entry.Value == null)
            {
                continue;
            }

            bool isUnlocked = profile.characters.owned_character_ids.Contains(entry.Key);
            bool isSelected = entry.Key == pendingCharacterSelectionId;
            StyleCharacterSelectOptionButton(entry.Value, isSelected, isUnlocked);
            entry.Value.interactable = isUnlocked;
        }

        CharacterData selectedCharacter = GameDatabase.Instance != null
            ? GameDatabase.Instance.GetCharacterByID(pendingCharacterSelectionId)
            : null;
        if (selectedCharacterLabel != null)
        {
            selectedCharacterLabel.text = selectedCharacter != null
                ? L("menu.character_select.selected_format", "Selected: {0} ({1})", selectedCharacter.GetDisplayName(), GetCharacterRarityLabel(selectedCharacter.rarity))
                : L("menu.character_select.none", "Selected: None");
        }

        RefreshCharacterPreviewSprite(selectedCharacter);

        if (startRunButton != null)
        {
            startRunButton.interactable = !string.IsNullOrWhiteSpace(pendingCharacterSelectionId)
                && profile.characters.owned_character_ids.Contains(pendingCharacterSelectionId);
            StyleCharacterSelectOptionButton(startRunButton, true, startRunButton.interactable);
        }

        if (characterSelectCloseButton != null)
        {
            StyleCharacterSelectOptionButton(characterSelectCloseButton, false, true, true);
        }
    }

    private void RefreshCharacterPreviewSprite(CharacterData character)
    {
        if (characterPreviewImage == null)
        {
            return;
        }

        Sprite previewSprite = null;
        if (character != null)
        {
            previewSprite = character.inGameSprite != null ? character.inGameSprite : character.portrait;
        }

        characterPreviewImage.sprite = previewSprite;
        characterPreviewImage.enabled = previewSprite != null;
        characterPreviewImage.color = previewSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        characterPreviewImage.preserveAspect = true;
    }

    private void EnsureDefaultCharacterUnlocked(SaveFile_Profile profile)
    {
        if (!profile.characters.owned_character_ids.Contains(DefaultCharacterId))
        {
            profile.characters.owned_character_ids.Add(DefaultCharacterId);
        }
    }

    private void EnsureLandingCharacterSelectorLayout()
    {
        if (modernMenuRoot == null)
        {
            return;
        }

        Transform selectorTransform = modernMenuRoot.transform.Find("CharacterSelector");
        GameObject selector = selectorTransform != null ? selectorTransform.gameObject : CreateUIObject("CharacterSelector", modernMenuRoot.transform);
        RectTransform selectorRect = selector.GetComponent<RectTransform>();
        selectorRect.anchorMin = new Vector2(0f, 0.5f);
        selectorRect.anchorMax = new Vector2(0f, 0.5f);
        selectorRect.pivot = new Vector2(0f, 0.5f);
        selectorRect.sizeDelta = new Vector2(340f, 470f);
        selectorRect.anchoredPosition = new Vector2(40f, -28f);

        Image selectorImage = selector.GetComponent<Image>();
        if (selectorImage == null)
        {
            selectorImage = selector.AddComponent<Image>();
        }

        selectorImage.sprite = GetMainMenuSprite(5);
        selectorImage.type = Image.Type.Simple;
        selectorImage.color = selectorImage.sprite != null ? Color.white : new Color(0.10f, 0.08f, 0.10f, 0.94f);

        Outline selectorOutline = selector.GetComponent<Outline>();
        if (selectorOutline == null)
        {
            selectorOutline = selector.AddComponent<Outline>();
        }

        selectorOutline.effectColor = new Color(0.88f, 0.55f, 0.18f, 0.32f);
        selectorOutline.effectDistance = new Vector2(2f, -2f);

        Transform previewFrameTransform = selector.transform.Find("PreviewFrame");
        GameObject previewFrame = previewFrameTransform != null ? previewFrameTransform.gameObject : CreateUIObject("PreviewFrame", selector.transform);
        RectTransform previewFrameRect = previewFrame.GetComponent<RectTransform>();
        previewFrameRect.anchorMin = new Vector2(0.5f, 1f);
        previewFrameRect.anchorMax = new Vector2(0.5f, 1f);
        previewFrameRect.pivot = new Vector2(0.5f, 1f);
        previewFrameRect.sizeDelta = new Vector2(260f, 260f);
        previewFrameRect.anchoredPosition = new Vector2(0f, -80f);

        Image previewFrameImage = previewFrame.GetComponent<Image>();
        if (previewFrameImage == null)
        {
            previewFrameImage = previewFrame.AddComponent<Image>();
        }

        previewFrameImage.color = new Color(0.08f, 0.09f, 0.11f, 0.98f);

        Transform previewImageTransform = previewFrame.transform.Find("PreviewImage");
        GameObject previewImageObject = previewImageTransform != null ? previewImageTransform.gameObject : CreateUIObject("PreviewImage", previewFrame.transform);
        menuCharacterPreviewImage = previewImageObject.GetComponent<Image>();
        if (menuCharacterPreviewImage == null)
        {
            menuCharacterPreviewImage = previewImageObject.AddComponent<Image>();
        }

        menuCharacterPreviewImage.preserveAspect = true;
        menuCharacterPreviewImage.raycastTarget = false;
        RectTransform previewImageRect = menuCharacterPreviewImage.rectTransform;
        previewImageRect.anchorMin = new Vector2(0f, 0f);
        previewImageRect.anchorMax = new Vector2(1f, 1f);
        previewImageRect.pivot = new Vector2(0.5f, 0.5f);
        previewImageRect.offsetMin = new Vector2(18f, 18f);
        previewImageRect.offsetMax = new Vector2(-18f, -18f);

        menuCharacterNameText = EnsureOrCreateLandingSelectorText(selector.transform, "CharacterName", 24f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.84f, 0.58f, 1f));
        RectTransform characterNameRect = menuCharacterNameText.rectTransform;
        characterNameRect.anchorMin = new Vector2(0f, 1f);
        characterNameRect.anchorMax = new Vector2(1f, 1f);
        characterNameRect.pivot = new Vector2(0.5f, 1f);
        characterNameRect.sizeDelta = new Vector2(-48f, 40f);
        characterNameRect.anchoredPosition = new Vector2(0f, -304f);

        menuCharacterRarityText = EnsureOrCreateLandingSelectorText(selector.transform, "CharacterRarity", 16f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.86f, 0.74f, 0.52f, 0.95f));
        RectTransform characterRarityRect = menuCharacterRarityText.rectTransform;
        characterRarityRect.anchorMin = new Vector2(0f, 1f);
        characterRarityRect.anchorMax = new Vector2(1f, 1f);
        characterRarityRect.pivot = new Vector2(0.5f, 1f);
        characterRarityRect.sizeDelta = new Vector2(-48f, 28f);
        characterRarityRect.anchoredPosition = new Vector2(0f, -340f);

        menuCharacterHintText = EnsureOrCreateLandingSelectorText(selector.transform, "CharacterHint", 15f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.72f, 0.67f, 0.55f, 0.92f));
        RectTransform characterHintRect = menuCharacterHintText.rectTransform;
        characterHintRect.anchorMin = new Vector2(0f, 1f);
        characterHintRect.anchorMax = new Vector2(1f, 1f);
        characterHintRect.pivot = new Vector2(0.5f, 1f);
        characterHintRect.sizeDelta = new Vector2(-48f, 24f);
        characterHintRect.anchoredPosition = new Vector2(0f, -370f);

        menuCharacterPrevButton = EnsureOrCreateLandingSelectorButton(selector.transform, "PrevCharacterButton", "<", new Vector2(22f, 18f));
        menuCharacterNextButton = EnsureOrCreateLandingSelectorButton(selector.transform, "NextCharacterButton", ">", new Vector2(318f, 18f));
    }

    private TextMeshProUGUI EnsureOrCreateLandingSelectorText(Transform parent, string objectName, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        Transform existing = parent.Find(objectName);
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            text = CreateTmpText(objectName, parent, string.Empty, fontSize, fontStyle, alignment, color);
        }

        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private Button EnsureOrCreateLandingSelectorButton(Transform parent, string objectName, string label, Vector2 anchoredPosition)
    {
        Transform existing = parent.Find(objectName);
        Button button = existing != null ? existing.GetComponent<Button>() : null;
        if (button == null)
        {
            button = CreateButton(objectName, parent, label);
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(0f, 0f);
        buttonRect.pivot = new Vector2(0f, 0f);
        buttonRect.sizeDelta = new Vector2(52f, 44f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetMainMenuSprite(14);
            image.type = Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : new Color(0.10f, 0.08f, 0.10f, 0.96f);
        }

        TextMeshProUGUI labelText = GetComponentAtPath<TextMeshProUGUI>(button.transform, "Label");
        if (labelText != null)
        {
            labelText.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
            labelText.fontSize = 22f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = new Color(0.95f, 0.84f, 0.58f, 1f);
            labelText.alignment = TextAlignmentOptions.Center;
        }

        return button;
    }

    private void RefreshLandingCharacterSelectorVisuals()
    {
        CharacterData selectedCharacter = GameDatabase.Instance != null
            ? GameDatabase.Instance.GetCharacterByID(pendingCharacterSelectionId)
            : null;

        if (menuCharacterNameText != null)
        {
            menuCharacterNameText.text = selectedCharacter != null
                ? selectedCharacter.GetDisplayName()
                : L("menu.character_select.none", "Selected: None");
        }

        if (menuCharacterRarityText != null)
        {
            menuCharacterRarityText.text = selectedCharacter != null
                ? GetCharacterRarityLabel(selectedCharacter.rarity)
                : string.Empty;
        }

        if (menuCharacterHintText != null)
        {
            menuCharacterHintText.text = L("menu.character_select.title", "Select Character");
        }

        if (menuCharacterPreviewImage != null)
        {
            Sprite previewSprite = selectedCharacter != null
                ? (selectedCharacter.inGameSprite != null ? selectedCharacter.inGameSprite : selectedCharacter.portrait)
                : null;
            menuCharacterPreviewImage.sprite = previewSprite;
            menuCharacterPreviewImage.enabled = previewSprite != null;
            menuCharacterPreviewImage.color = previewSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        List<CharacterData> selectableCharacters = GetSelectableLandingCharacters();
        bool canCycle = selectableCharacters.Count > 1;
        if (menuCharacterPrevButton != null)
        {
            menuCharacterPrevButton.interactable = canCycle;
        }

        if (menuCharacterNextButton != null)
        {
            menuCharacterNextButton.interactable = canCycle;
        }
    }

    private void EnsureCharacterPreviewLayout()
    {
        if (characterSelectPanel == null)
        {
            return;
        }

        Transform cardTransform = characterSelectPanel.transform.Find("CharacterSelectCard");
        if (cardTransform == null)
        {
            return;
        }

        Transform previewPanelTransform = cardTransform.Find("PreviewPanel");
        GameObject previewPanel = previewPanelTransform != null ? previewPanelTransform.gameObject : CreateUIObject("PreviewPanel", cardTransform);
        Image previewPanelImage = previewPanel.GetComponent<Image>();
        if (previewPanelImage == null)
        {
            previewPanelImage = previewPanel.AddComponent<Image>();
        }

        previewPanelImage.color = new Color(0.08f, 0.09f, 0.11f, 0.97f);
        previewPanelImage.raycastTarget = false;

        RectTransform previewPanelRect = previewPanel.GetComponent<RectTransform>();
        previewPanelRect.anchorMin = new Vector2(0f, 0f);
        previewPanelRect.anchorMax = new Vector2(0f, 1f);
        previewPanelRect.pivot = new Vector2(0f, 0.5f);
        previewPanelRect.offsetMin = new Vector2(32f, 92f);
        previewPanelRect.offsetMax = new Vector2(252f, -124f);

        Transform previewImageTransform = previewPanel.transform.Find("PreviewImage");
        if (previewImageTransform == null)
        {
            GameObject previewImageObject = CreateUIObject("PreviewImage", previewPanel.transform);
            characterPreviewImage = previewImageObject.AddComponent<Image>();
        }
        else
        {
            characterPreviewImage = previewImageTransform.GetComponent<Image>();
            if (characterPreviewImage == null)
            {
                characterPreviewImage = previewImageTransform.gameObject.AddComponent<Image>();
            }
        }

        characterPreviewImage.raycastTarget = false;
        characterPreviewImage.preserveAspect = true;

        RectTransform previewImageRect = characterPreviewImage.rectTransform;
        previewImageRect.anchorMin = new Vector2(0f, 0f);
        previewImageRect.anchorMax = new Vector2(1f, 1f);
        previewImageRect.pivot = new Vector2(0.5f, 0.5f);
        previewImageRect.offsetMin = new Vector2(18f, 18f);
        previewImageRect.offsetMax = new Vector2(-18f, -18f);
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

        return string.Compare(a.GetDisplayName(), b.GetDisplayName(), System.StringComparison.OrdinalIgnoreCase);
    }

    private string BuildCharacterLabel(CharacterData character, bool isUnlocked)
    {
        string status = isUnlocked
            ? L("common.status.unlocked", "Unlocked")
            : L("common.status.locked", "Locked");
        return $"{character.GetDisplayName()}  [{GetCharacterRarityLabel(character.rarity)}]  -  {status}";
    }

    private void RefreshLocalizedUi()
    {
        fallbackFont = LocalizedFontResolver.ResolveLegacyFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        fallbackTmpFont = LocalizedFontResolver.ResolveTmpFont(pixelMenuFont != null ? pixelMenuFont : TMP_Settings.defaultFontAsset);
        ResolveTrackedPanelReferences();
        RefreshModernMenuContent();

        if (characterSelectTitleLabel != null)
        {
            characterSelectTitleLabel.text = L("menu.character_select.title", "Select Character");
        }

        SetButtonLabel(startRunButton, L("menu.start_run", "Start Run"));

        SetButtonLabel(characterSelectCloseButton, L("common.back", "Back"));
        ApplyCharacterSelectPanelTheme();
        RefreshSettingsPanel();
        RefreshGachaPanel();
        RefreshAuthOnboardingPanel();

        if (achievementPanel != null)
        {
            RefreshTrackedPanelLocalization(achievementPanel, "Achievements");
        }

        if (ChallengePanel != null)
        {
            RefreshTrackedPanelLocalization(ChallengePanel, "Challenges");
        }

        if (missionPanel != null)
        {
            RefreshTrackedPanelLocalization(missionPanel, "Missions");
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

    private void RefreshTrackedPanelLocalization(GameObject panel, string panelLabel)
    {
        if (panel == null)
        {
            return;
        }

        RefreshTrackedPanelChrome(panel, panelLabel);
        PopulateTrackedPanelContent(panel, panelLabel);
    }

    private void RefreshTrackedPanelChrome(GameObject panel, string panelLabel)
    {
        SetTextAtPath(panel.transform, "PrettyCard/GeneratedTitle", GetTrackedPanelTitle(panelLabel));
        SetTextAtPath(panel.transform, "PrettyCard/GeneratedSubtitle", GetTrackedPanelSubtitle(panelLabel));
        SetTextAtPath(panel.transform, "PrettyCard/Title", GetTrackedPanelTitle(panelLabel));
        SetTextAtPath(panel.transform, "PrettyCard/Subtitle", GetTrackedPanelSubtitle(panelLabel));

        Transform closeButtonTransform = panel.transform.Find("PrettyCard/GeneratedCloseButton");
        if (closeButtonTransform != null && closeButtonTransform.TryGetComponent(out Button closeButton))
        {
            SetButtonLabel(closeButton, L("common.close", "Close"));
        }

        Transform alternateCloseButtonTransform = panel.transform.Find("PrettyCard/CloseButton");
        if (alternateCloseButtonTransform != null && alternateCloseButtonTransform.TryGetComponent(out Button alternateCloseButton))
        {
            SetButtonLabel(alternateCloseButton, L("common.close", "Close"));
        }
    }

    private void PopulateTrackedPanelContent(GameObject panel, string panelLabel)
    {
        if (panel == null)
        {
            return;
        }

        Transform content = panel.transform.Find("PrettyCard/GeneratedViewport/GeneratedContent");
        if (content == null)
        {
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        if (panelLabel == "Challenges")
        {
            PopulateChallengePanelContent(panel, content);
        }
        else if (panelLabel == "Missions")
        {
            PopulateMissionPanelContent(panel, content);
        }
        else
        {
            PopulateAchievementPanelContent(content);
        }
    }

    private void PopulateChallengePanelContent(GameObject panel, Transform content)
    {
        EnsureChallengeManagerExists();
        CreateTrackedPanelLabel(content, L("menu.challenges.instructions", "Choose modifiers before starting a run."), 17f, FontStyles.Bold, new Color(0.77f, 0.70f, 0.52f, 1f), 38f);

        foreach (ChallengeType challengeType in System.Enum.GetValues(typeof(ChallengeType)))
        {
            bool isActive = ChallengeManager.Instance != null && ChallengeManager.Instance.IsChallengeActive(challengeType);
            string label = $"{GetChallengeDisplayName(challengeType)}\n{GetChallengeStateLabel(isActive)} - {L("menu.challenges.action.toggle", "Click to toggle")}";
            Button row = CreateTrackedPanelRow(content, label, isActive, 74f);
            ChallengeType capturedType = challengeType;
            row.onClick.AddListener(() =>
            {
                EnsureChallengeManagerExists();
                if (ChallengeManager.Instance == null)
                {
                    return;
                }

                bool nextState = !ChallengeManager.Instance.IsChallengeActive(capturedType);
                ChallengeManager.Instance.ToggleChallenge(capturedType, nextState);
                PopulateTrackedPanelContent(panel, "Challenges");
            });
        }
    }

    private void PopulateAchievementPanelContent(Transform content)
    {
        AchievementManager manager = AchievementManager.Instance;
        IReadOnlyList<AchievementData> achievements = manager != null ? manager.GetAllAchievements() : null;
        if (achievements == null || achievements.Count == 0)
        {
            CreateTrackedPanelLabel(content, L("achievements.empty", "No achievements configured."), 18f, FontStyles.Italic, new Color(0.77f, 0.70f, 0.52f, 1f), 48f);
            return;
        }

        for (int i = 0; i < achievements.Count; i++)
        {
            AchievementData achievement = achievements[i];
            if (achievement == null)
            {
                continue;
            }

            int progress = StatisticsManager.Instance != null ? StatisticsManager.Instance.GetStat(achievement.statKey) : 0;
            int target = Mathf.Max(1, achievement.targetValue);
            int displayProgress = Mathf.Clamp(progress, 0, target);
            bool isComplete = progress >= target;
            string label = $"{achievement.GetTitle()}\n{achievement.GetDescription()}\n{displayProgress}/{target} {GetAchievementStateLabel(isComplete)}";
            CreateTrackedPanelRow(content, label, isComplete, 106f);
        }
    }

    private void PopulateMissionPanelContent(GameObject panel, Transform content)
    {
        DailyTracker tracker = SaveManager.Profile != null ? SaveManager.Profile.daily_tracker : null;
        if (tracker == null)
        {
            CreateTrackedPanelLabel(content, L("missions.empty", "No missions available."), 18f, FontStyles.Italic, new Color(0.77f, 0.70f, 0.52f, 1f), 40f);
            return;
        }

        AddTrackedMissionSection(panel, content, L("missions.daily", "Daily Missions"), tracker.active_daily_missions);
        AddTrackedMissionSection(panel, content, L("missions.weekly", "Weekly Missions"), tracker.active_weekly_missions);
    }

    private void AddTrackedMissionSection(GameObject panel, Transform content, string sectionTitle, List<ActiveMissionEntry> missions)
    {
        CreateTrackedPanelLabel(content, sectionTitle.ToUpperInvariant(), 22f, FontStyles.Bold, new Color(0.92f, 0.75f, 0.28f, 1f), 42f);

        if (missions == null || missions.Count == 0)
        {
            CreateTrackedPanelLabel(content, L("missions.empty", "No missions available."), 18f, FontStyles.Italic, new Color(0.77f, 0.70f, 0.52f, 1f), 40f);
            return;
        }

        for (int i = 0; i < missions.Count; i++)
        {
            ActiveMissionEntry entry = missions[i];
            if (entry == null)
            {
                continue;
            }

            MissionData data = MissionManager.Instance != null ? MissionManager.Instance.GetMissionDataByID(entry.mission_id) : null;
            string missionLine = data != null ? data.GetDescription() : entry.description;
            if (string.IsNullOrWhiteSpace(missionLine))
            {
                missionLine = entry.mission_id;
            }

            string reward = data != null ? $"{CurrencyUiUtility.FormatAmount(data.rewardAmount)} {GetMissionRewardTypeLabel(data.rewardType)}" : string.Empty;
            string state = GetMissionStateLabel(entry);
            string label = string.IsNullOrWhiteSpace(reward)
                ? $"{missionLine}\n{entry.current_progress}/{entry.target_value}  {state}"
                : $"{missionLine}\n{entry.current_progress}/{entry.target_value}  {state}  {reward}";
            Button row = CreateTrackedPanelRow(content, label, entry.is_completed && !entry.is_claimed, 78f);

            if (entry.is_completed && !entry.is_claimed && MissionManager.Instance != null)
            {
                ActiveMissionEntry capturedEntry = entry;
                row.onClick.AddListener(() =>
                {
                    MissionManager.Instance.ClaimMission(capturedEntry);
                    PopulateTrackedPanelContent(panel, "Missions");
                });
            }
        }
    }

    private void EnsureChallengeManagerExists()
    {
        if (ChallengeManager.Instance != null)
        {
            return;
        }

        new GameObject("ChallengeManager").AddComponent<ChallengeManager>();
    }

    private Button CreateTrackedPanelRow(Transform content, string label, bool highlighted, float height)
    {
        GameObject row = CreateUIObject("GeneratedRow", content);
        Image image = row.AddComponent<Image>();
        image.sprite = highlighted ? GetMainMenuSprite(11) : GetMainMenuSprite(5);
        image.color = highlighted ? new Color(0.35f, 0.25f, 0.10f, 0.96f) : new Color(0.09f, 0.10f, 0.13f, 0.96f);
        image.type = Image.Type.Simple;

        Button button = row.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.55f, 1f);
        colors.pressedColor = new Color(0.74f, 0.52f, 0.20f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Outline outline = row.AddComponent<Outline>();
        outline.effectColor = highlighted ? new Color(0.95f, 0.72f, 0.22f, 0.75f) : new Color(0.45f, 0.28f, 0.12f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);

        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        TextMeshProUGUI text = CreateTmpText("Label", row.transform, label, 16f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, highlighted ? new Color(1f, 0.88f, 0.55f, 1f) : new Color(0.91f, 0.88f, 0.78f, 1f));
        text.lineSpacing = -5f;
        text.enableWordWrapping = true;
        LocalizedFontResolver.ApplyTo(text, fallbackTmpFont);
        StretchToParent(text.rectTransform, 22f, 8f);
        return button;
    }

    private TextMeshProUGUI CreateTrackedPanelLabel(Transform content, string label, float fontSize, FontStyles style, Color color, float height)
    {
        TextMeshProUGUI text = CreateTmpText("GeneratedLabel", content, label, fontSize, style, TextAlignmentOptions.MidlineLeft, color);
        text.characterSpacing = 1.5f;
        LocalizedFontResolver.ApplyTo(text, fallbackTmpFont);
        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        return text;
    }

    private string GetTrackedPanelTitle(string panelLabel)
    {
        switch (panelLabel)
        {
            case "Achievements":
                return GetAchievementsTitle();
            case "Challenges":
                return GetChallengesTitle();
            case "Missions":
                return GetMissionsTitle();
            default:
                return panelLabel;
        }
    }

    private string GetTrackedPanelSubtitle(string panelLabel)
    {
        switch (panelLabel)
        {
            case "Achievements":
                return L("menu.achievements.subtitle", "Review long-term progress and milestones.");
            case "Challenges":
                return L("menu.challenges.subtitle", "Enable run modifiers and earn bragging rights.");
            case "Missions":
                return L("menu.missions.subtitle", "Track daily goals and collect rewards.");
            default:
                return string.Empty;
        }
    }

    private string GetChallengeDisplayName(ChallengeType challengeType)
    {
        switch (challengeType)
        {
            case ChallengeType.FragileCrystal:
                return L("menu.challenges.type.fragile_crystal", "Fragile Crystal");
            case ChallengeType.ThePurge:
                return L("menu.challenges.type.the_purge", "The Purge");
            case ChallengeType.EndlessGreed:
                return L("menu.challenges.type.endless_greed", "Endless Greed");
            case ChallengeType.TheGladiator:
                return L("menu.challenges.type.the_gladiator", "The Gladiator");
            case ChallengeType.BloodForPower:
                return L("menu.challenges.type.blood_for_power", "Blood for Power");
            case ChallengeType.Crossfire:
                return L("menu.challenges.type.crossfire", "Crossfire");
            case ChallengeType.TotalConfusion:
                return L("menu.challenges.type.total_confusion", "Total Confusion");
            case ChallengeType.RainOfFire:
                return L("menu.challenges.type.rain_of_fire", "Rain of Fire");
            case ChallengeType.TheUnlucky:
                return L("menu.challenges.type.the_unlucky", "The Unlucky");
            case ChallengeType.LastBreath:
                return L("menu.challenges.type.last_breath", "Last Breath");
            default:
                return challengeType.ToString();
        }
    }

    private string GetChallengeStateLabel(bool isActive)
    {
        return isActive
            ? L("menu.challenges.state.active", "Active")
            : L("menu.challenges.state.inactive", "Inactive");
    }

    private string GetAchievementStateLabel(bool isComplete)
    {
        return isComplete
            ? L("achievements.state.complete", "Complete")
            : L("common.status.locked", "Locked");
    }

    private string GetMissionStateLabel(ActiveMissionEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (entry.is_claimed)
        {
            return L("missions.state.claimed", "Claimed");
        }

        if (entry.is_completed)
        {
            return L("missions.state.ready_to_claim", "Ready to claim");
        }

        return L("missions.state.in_progress", "In progress");
    }

    private string GetMissionRewardTypeLabel(MissionRewardType rewardType)
    {
        switch (rewardType)
        {
            case MissionRewardType.Gold:
                return L("currency.gold", "Gold");
            case MissionRewardType.Orbs:
                return L("currency.orb", "Orbs");
            case MissionRewardType.Ticket:
                return L("currency.ticket", "Tickets");
            default:
                return rewardType.ToString();
        }
    }

    private string GetCharacterRarityLabel(CharacterRarity rarity)
    {
        switch (rarity)
        {
            case CharacterRarity.Default:
                return L("common.rarity.default", "Default");
            case CharacterRarity.Epic:
                return L("common.rarity.epic", "Epic");
            case CharacterRarity.Mythic:
                return L("common.rarity.mythic", "Mythic");
            default:
                return rarity.ToString();
        }
    }

    private string GetCurrencyTypeLabel(CurrencyType currencyType)
    {
        switch (currencyType)
        {
            case CurrencyType.Gold:
                return L("currency.gold", "Gold");
            case CurrencyType.Orb:
                return L("currency.orb", "Orbs");
            case CurrencyType.Key:
                return L("currency.key", "Keys");
            case CurrencyType.XP:
                return L("currency.xp", "XP");
            case CurrencyType.Rupee:
                return L("currency.rupee", "Rupees");
            default:
                return currencyType.ToString();
        }
    }

    private TextMeshProUGUI CreateTmpText(string objectName, Transform parent, string message, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
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

        TMP_Text tmpLabelText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabelText != null)
        {
            LocalizedFontResolver.ApplyTo(tmpLabelText, fallbackTmpFont);
            tmpLabelText.text = label;
            return;
        }

        TMP_Text convertedLabel = EnsureTmpTextInChildren(button.gameObject);
        if (convertedLabel != null)
        {
            convertedLabel.text = label;
        }
    }

    private void SetTextAtPath(Transform root, string relativePath, string value)
    {
        if (root == null)
        {
            return;
        }

        Transform target = root.Find(relativePath);
        if (target == null)
        {
            return;
        }

        if (target.TryGetComponent(out TextMeshProUGUI tmpText))
        {
            LocalizedFontResolver.ApplyTo(tmpText, fallbackTmpFont);
            tmpText.text = value;
            return;
        }

        TMP_Text convertedText = EnsureTmpTextComponent(target.gameObject);
        if (convertedText != null)
        {
            convertedText.text = value;
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

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            RectTransform labelRect = tmpLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;

            if (sprite != null)
            {
                labelRect.offsetMin = new Vector2(14f, 12f);
                labelRect.offsetMax = new Vector2(-42f, -12f);
                tmpLabel.alignment = TextAlignmentOptions.Left;
            }
            else
            {
                labelRect.offsetMin = new Vector2(12f, 12f);
                labelRect.offsetMax = new Vector2(-12f, -12f);
                tmpLabel.alignment = TextAlignmentOptions.Center;
            }
        }

        TMP_Text convertedLabel = tmpLabel != null ? tmpLabel : EnsureTmpTextInChildren(button.gameObject);
        if (convertedLabel != null)
        {
            RectTransform labelRect = convertedLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;

            if (sprite != null)
            {
                labelRect.offsetMin = new Vector2(14f, 12f);
                labelRect.offsetMax = new Vector2(-42f, -12f);
                convertedLabel.alignment = TextAlignmentOptions.Left;
            }
            else
            {
                labelRect.offsetMin = new Vector2(12f, 12f);
                labelRect.offsetMax = new Vector2(-12f, -12f);
                convertedLabel.alignment = TextAlignmentOptions.Center;
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

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string message, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
        text.fontSize = fontSize;
        text.alignment = ConvertAlignment(alignment);
        text.color = Color.white;
        text.text = message;
        text.enableWordWrapping = true;
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

        TextMeshProUGUI labelText = CreateText("Label", buttonObject.transform, label, 18, TextAnchor.MiddleCenter);
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

    private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
            default:
                return TextAlignmentOptions.Center;
        }
    }

    private static FontStyles ConvertFontStyle(FontStyle fontStyle)
    {
        switch (fontStyle)
        {
            case FontStyle.Bold:
                return FontStyles.Bold;
            case FontStyle.Italic:
                return FontStyles.Italic;
            case FontStyle.BoldAndItalic:
                return FontStyles.Bold | FontStyles.Italic;
            default:
                return FontStyles.Normal;
        }
    }

    private void ApplyMenuModalTheme(GameObject panel, string cardName, TextMeshProUGUI titleText, TextMeshProUGUI subtitleText)
    {
        if (panel == null)
        {
            return;
        }

        fallbackFont = fallbackFont != null ? fallbackFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ResolveMainMenuArtReferences();
        fallbackTmpFont = LocalizedFontResolver.ResolveTmpFont(pixelMenuFont != null ? pixelMenuFont : TMP_Settings.defaultFontAsset);

        Image overlay = panel.GetComponent<Image>();
        if (overlay == null)
        {
            overlay = panel.AddComponent<Image>();
        }

        overlay.sprite = menuBackgroundSprite;
        overlay.type = Image.Type.Simple;
        overlay.preserveAspect = false;
        overlay.color = menuBackgroundSprite != null
            ? new Color(0.52f, 0.21f, 0.16f, 0.88f)
            : new Color(0.02f, 0.01f, 0.03f, 0.94f);
        overlay.raycastTarget = true;

        GameObject shade = null;
        Transform shadeTransform = panel.transform.Find("EclipseShade");
        if (shadeTransform != null)
        {
            shade = shadeTransform.gameObject;
        }
        else
        {
            shade = CreateUIObject("EclipseShade", panel.transform);
        }

        RectTransform shadeRect = shade.GetComponent<RectTransform>();
        StretchToParent(shadeRect);
        shade.transform.SetAsFirstSibling();

        Image shadeImage = shade.GetComponent<Image>();
        if (shadeImage == null)
        {
            shadeImage = shade.AddComponent<Image>();
        }

        shadeImage.color = new Color(0f, 0f, 0f, 0.68f);
        shadeImage.raycastTarget = false;

        Transform cardTransform = panel.transform.Find(cardName);
        if (cardTransform == null)
        {
            return;
        }

        GameObject card = cardTransform.gameObject;
        Image cardImage = card.GetComponent<Image>();
        if (cardImage == null)
        {
            cardImage = card.AddComponent<Image>();
        }

        cardImage.color = new Color32(16, 8, 5, 238);

        Outline cardOutline = card.GetComponent<Outline>();
        if (cardOutline == null)
        {
            cardOutline = card.AddComponent<Outline>();
        }

        cardOutline.effectColor = new Color(0.88f, 0.55f, 0.18f, 0.72f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        Shadow cardShadow = card.GetComponent<Shadow>();
        if (cardShadow == null)
        {
            cardShadow = card.AddComponent<Shadow>();
        }

        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        cardShadow.effectDistance = new Vector2(0f, -6f);

        Transform headerBandTransform = card.transform.Find("ModalHeaderBand");
        GameObject headerBand = headerBandTransform != null ? headerBandTransform.gameObject : CreateUIObject("ModalHeaderBand", card.transform);
        headerBand.transform.SetAsFirstSibling();

        Image headerImage = headerBand.GetComponent<Image>();
        if (headerImage == null)
        {
            headerImage = headerBand.AddComponent<Image>();
        }

        headerImage.sprite = GetMainMenuSprite(5);
        headerImage.type = Image.Type.Simple;
        headerImage.color = headerImage.sprite != null
            ? new Color(0.92f, 0.76f, 0.48f, 1f)
            : new Color(0.14f, 0.08f, 0.05f, 0.96f);
        headerImage.raycastTarget = false;

        RectTransform headerRect = headerBand.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(24f, -84f);
        headerRect.offsetMax = new Vector2(-24f, -24f);

        if (titleText != null)
        {
            titleText.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
            titleText.color = new Color(1f, 0.83f, 0.48f, 1f);
            titleText.characterSpacing = 3f;
            titleText.fontStyle = FontStyles.Bold;
            ApplyTitleGoldGradient(titleText);

            Shadow titleShadow = titleText.GetComponent<Shadow>();
            if (titleShadow == null)
            {
                titleShadow = titleText.gameObject.AddComponent<Shadow>();
            }

            titleShadow.effectColor = new Color(0.05f, 0.01f, 0f, 0.95f);
            titleShadow.effectDistance = new Vector2(0f, -3f);
            titleText.transform.SetAsLastSibling();
        }

        if (subtitleText != null)
        {
            subtitleText.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
            subtitleText.color = new Color(0.78f, 0.69f, 0.48f, 0.96f);
            subtitleText.characterSpacing = 1.2f;
            subtitleText.transform.SetAsLastSibling();
        }
    }

    private void StyleMenuSectionPanel(Transform sectionTransform, int spriteIndex, Color fillColor)
    {
        if (sectionTransform == null)
        {
            return;
        }

        Image image = sectionTransform.GetComponent<Image>();
        if (image == null)
        {
            image = sectionTransform.gameObject.AddComponent<Image>();
        }

        image.sprite = GetMainMenuSprite(spriteIndex);
        image.type = Image.Type.Simple;
        image.color = fillColor;

        Outline outline = sectionTransform.GetComponent<Outline>();
        if (outline == null)
        {
            outline = sectionTransform.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.43f, 0.26f, 0.11f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private void RefreshSettingsOptionButtonStyle(Button button, bool isSelected)
    {
        ApplyMenuSpriteButtonStyle(
            button,
            isSelected ? 0 : 5,
            isSelected ? new Color(0.93f, 0.76f, 0.28f, 1f) : new Color(0.18f, 0.11f, 0.08f, 0.98f),
            isSelected ? new Color(0.16f, 0.09f, 0.04f, 1f) : new Color(0.96f, 0.88f, 0.60f, 1f),
            18,
            FontStyle.Bold);
    }

    private void ApplySettingsPanelTheme()
    {
        if (settingsPanel == null)
        {
            return;
        }

        Transform card = settingsPanel.transform.Find("SettingsCard");
        Transform languageArea = card != null ? card.Find("LanguageArea") : null;
        Transform displayArea = card != null ? card.Find("DisplayArea") : null;
        Transform controlsArea = card != null ? card.Find("ControlsArea") : null;
        Transform divider = card != null ? card.Find("Divider") : null;

        StyleMenuSectionPanel(languageArea, 11, new Color(0.10f, 0.12f, 0.15f, 0.97f));
        StyleMenuSectionPanel(displayArea, 11, new Color(0.10f, 0.12f, 0.15f, 0.97f));
        StyleMenuSectionPanel(controlsArea, 11, new Color(0.10f, 0.12f, 0.15f, 0.97f));

        if (divider != null && divider.TryGetComponent(out Image dividerImage))
        {
            dividerImage.color = new Color(0.93f, 0.76f, 0.28f, 0.92f);
        }

        RefreshSettingsOptionButtonStyle(settingsLanguageTabButton, activeSettingsSection == SettingsSection.Language);
        RefreshSettingsOptionButtonStyle(settingsDisplayTabButton, activeSettingsSection == SettingsSection.Display);
        RefreshSettingsOptionButtonStyle(settingsControlsTabButton, activeSettingsSection == SettingsSection.Controls);

        foreach (KeyValuePair<string, Button> entry in settingsLanguageButtons)
        {
            RefreshSettingsOptionButtonStyle(entry.Value, entry.Key == LocalizationManager.GetCurrentLanguageCode());
        }

        RefreshSettingsOptionButtonStyle(settingsWindowedButton, SaveManager.Settings.general.windowed_mode);
        RefreshSettingsOptionButtonStyle(settingsFullscreenButton, !SaveManager.Settings.general.windowed_mode);
        foreach (KeyValuePair<string, Button> entry in settingsControlBindingButtons)
        {
            if (entry.Value == null)
            {
                continue;
            }

            bool isListening = string.Equals(activeRebindEntryId, entry.Key, StringComparison.Ordinal);
            RefreshSettingsOptionButtonStyle(entry.Value, isListening);
        }

        ApplyMenuSpriteButtonStyle(settingsCloseButton, 5, new Color(0.18f, 0.11f, 0.08f, 0.98f), new Color(0.96f, 0.88f, 0.60f, 1f), 18, FontStyle.Bold);
    }

    private void ApplyCharacterSelectPanelTheme()
    {
        if (characterSelectPanel == null)
        {
            return;
        }

        fallbackFont = fallbackFont != null ? fallbackFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ResolveMainMenuArtReferences();

        Image overlay = characterSelectPanel.GetComponent<Image>();
        if (overlay == null)
        {
            overlay = characterSelectPanel.AddComponent<Image>();
        }

        overlay.sprite = menuBackgroundSprite;
        overlay.type = Image.Type.Simple;
        overlay.preserveAspect = false;
        overlay.color = menuBackgroundSprite != null
            ? new Color(0.52f, 0.21f, 0.16f, 0.88f)
            : new Color(0.02f, 0.01f, 0.03f, 0.94f);
        overlay.raycastTarget = true;

        Transform shadeTransform = characterSelectPanel.transform.Find("EclipseShade");
        GameObject shade = shadeTransform != null ? shadeTransform.gameObject : CreateUIObject("EclipseShade", characterSelectPanel.transform);
        StretchToParent(shade.GetComponent<RectTransform>());
        shade.transform.SetAsFirstSibling();

        Image shadeImage = shade.GetComponent<Image>();
        if (shadeImage == null)
        {
            shadeImage = shade.AddComponent<Image>();
        }

        shadeImage.color = new Color(0f, 0f, 0f, 0.68f);
        shadeImage.raycastTarget = false;

        Transform cardTransform = characterSelectPanel.transform.Find("CharacterSelectCard");
        if (cardTransform == null)
        {
            return;
        }

        EnsureCharacterPreviewLayout();

        RectTransform cardRect = cardTransform as RectTransform;
        if (cardRect != null)
        {
            cardRect.anchorMin = new Vector2(0.28f, 0.5f);
            cardRect.anchorMax = new Vector2(0.28f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(700f, 620f);
            cardRect.anchoredPosition = Vector2.zero;
        }

        Image cardImage = cardTransform.GetComponent<Image>();
        if (cardImage == null)
        {
            cardImage = cardTransform.gameObject.AddComponent<Image>();
        }

        cardImage.color = new Color32(16, 8, 5, 238);

        Outline cardOutline = cardTransform.GetComponent<Outline>();
        if (cardOutline == null)
        {
            cardOutline = cardTransform.gameObject.AddComponent<Outline>();
        }

        cardOutline.effectColor = new Color(0.88f, 0.55f, 0.18f, 0.72f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        Shadow cardShadow = cardTransform.GetComponent<Shadow>();
        if (cardShadow == null)
        {
            cardShadow = cardTransform.gameObject.AddComponent<Shadow>();
        }

        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        cardShadow.effectDistance = new Vector2(0f, -6f);

        Transform headerBandTransform = cardTransform.Find("ModalHeaderBand");
        GameObject headerBand = headerBandTransform != null ? headerBandTransform.gameObject : CreateUIObject("ModalHeaderBand", cardTransform);
        headerBand.transform.SetAsFirstSibling();

        Image headerImage = headerBand.GetComponent<Image>();
        if (headerImage == null)
        {
            headerImage = headerBand.AddComponent<Image>();
        }

        headerImage.sprite = GetMainMenuSprite(5);
        headerImage.type = Image.Type.Simple;
        headerImage.color = headerImage.sprite != null
            ? new Color(0.92f, 0.76f, 0.48f, 1f)
            : new Color(0.14f, 0.08f, 0.05f, 0.96f);
        headerImage.raycastTarget = false;

        RectTransform headerRect = headerBand.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(24f, -84f);
        headerRect.offsetMax = new Vector2(-24f, -24f);

        Transform viewportTransform = cardTransform.Find("CharacterViewport");
        StyleMenuSectionPanel(viewportTransform, 11, new Color(0.08f, 0.09f, 0.11f, 0.97f));
        if (viewportTransform is RectTransform viewportRect)
        {
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(272f, 92f);
            viewportRect.offsetMax = new Vector2(-24f, -124f);
        }

        Transform previewTransform = cardTransform.Find("PreviewPanel");
        StyleMenuSectionPanel(previewTransform, 11, new Color(0.08f, 0.09f, 0.11f, 0.97f));

        if (characterSelectTitleLabel != null)
        {
            characterSelectTitleLabel.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
            characterSelectTitleLabel.fontStyle = FontStyles.Bold;
            characterSelectTitleLabel.fontSize = 30f;
            characterSelectTitleLabel.alignment = TextAlignmentOptions.Left;
            characterSelectTitleLabel.color = new Color(0.98f, 0.86f, 0.56f, 1f);
            RectTransform titleRect = characterSelectTitleLabel.rectTransform;
            titleRect.offsetMin = new Vector2(272f, -72f);
            titleRect.offsetMax = new Vector2(-24f, -18f);

            Shadow titleShadow = characterSelectTitleLabel.GetComponent<Shadow>();
            if (titleShadow == null)
            {
                titleShadow = characterSelectTitleLabel.gameObject.AddComponent<Shadow>();
            }

            titleShadow.effectColor = new Color(0.08f, 0.02f, 0f, 0.95f);
            titleShadow.effectDistance = new Vector2(0f, -2f);
        }

        if (selectedCharacterLabel != null)
        {
            selectedCharacterLabel.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
            selectedCharacterLabel.fontStyle = FontStyles.Bold;
            selectedCharacterLabel.fontSize = 18f;
            selectedCharacterLabel.alignment = TextAlignmentOptions.Left;
            selectedCharacterLabel.color = new Color(0.88f, 0.79f, 0.60f, 1f);
            RectTransform selectedRect = selectedCharacterLabel.rectTransform;
            selectedRect.offsetMin = new Vector2(272f, -112f);
            selectedRect.offsetMax = new Vector2(-24f, -74f);
        }

        StyleCharacterSelectOptionButton(characterSelectCloseButton, false, true, true);
        StyleCharacterSelectOptionButton(startRunButton, true, startRunButton != null && startRunButton.interactable);
    }

    private void StyleCharacterSelectOptionButton(Button button, bool isSelected, bool isAvailable)
    {
        StyleCharacterSelectOptionButton(button, isSelected, isAvailable, false);
    }

    private void StyleCharacterSelectOptionButton(Button button, bool isSelected, bool isAvailable, bool secondary)
    {
        if (button == null)
        {
            return;
        }

        int spriteIndex = secondary ? 5 : isSelected ? 0 : 11;
        Color fillColor = !isAvailable
            ? new Color(0.20f, 0.18f, 0.18f, 0.92f)
            : secondary
                ? new Color(0.18f, 0.11f, 0.08f, 0.98f)
                : isSelected
                    ? new Color(0.93f, 0.76f, 0.28f, 1f)
                    : new Color(0.19f, 0.12f, 0.08f, 0.98f);
        Color labelColor = !isAvailable
            ? new Color(0.52f, 0.50f, 0.46f, 1f)
            : secondary
                ? new Color(0.96f, 0.88f, 0.60f, 1f)
                : isSelected
                    ? new Color(0.13f, 0.08f, 0.03f, 1f)
                    : new Color(0.96f, 0.88f, 0.60f, 1f);

        ApplyMenuSpriteButtonStyle(button, spriteIndex, fillColor, labelColor, secondary ? 18 : 17, FontStyle.Bold);

        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.22f, 0.20f, 0.20f, 0.94f);
        button.colors = colors;
    }

    private void ApplyGachaPanelTheme()
    {
        if (gachaPanel == null)
        {
            return;
        }

        Transform card = gachaPanel.transform.Find("GachaCard");
        if (card == null)
        {
            return;
        }

        StyleMenuSectionPanel(card.Find("WalletPanel"), 5, new Color(0.12f, 0.09f, 0.07f, 0.96f));
        StyleMenuSectionPanel(card.Find("MeteorIndexPanel"), 5, new Color(0.12f, 0.09f, 0.07f, 0.96f));
        StyleMenuSectionPanel(card.Find("MeteorFrame"), 11, new Color(0.12f, 0.11f, 0.10f, 0.98f));
        StyleMenuSectionPanel(card.Find("ResultArea"), 5, new Color(0.10f, 0.10f, 0.10f, 0.96f));
        StyleMenuSectionPanel(card.Find("GachaRewardsPanel"), 11, new Color(0.11f, 0.10f, 0.12f, 0.98f));
        StyleMenuSectionPanel(card.Find("GachaRewardsPanel/RewardsViewport"), 5, new Color(0.08f, 0.09f, 0.11f, 0.96f));

        if (gachaMeteorNameText != null)
        {
            gachaMeteorNameText.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
            gachaMeteorNameText.characterSpacing = 1.8f;
            gachaMeteorNameText.color = new Color(1f, 0.94f, 0.82f, 1f);
        }

        if (gachaRewardsTitleText != null)
        {
            gachaRewardsTitleText.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
            gachaRewardsTitleText.characterSpacing = 2f;
            ApplyTitleGoldGradient(gachaRewardsTitleText);
        }

        ApplyMenuSpriteButtonStyle(gachaPrevButton, 5, new Color(0.18f, 0.11f, 0.08f, 0.98f), new Color(0.95f, 0.78f, 0.30f, 1f), 28, FontStyle.Bold);
        ApplyMenuSpriteButtonStyle(gachaNextButton, 5, new Color(0.18f, 0.11f, 0.08f, 0.98f), new Color(0.95f, 0.78f, 0.30f, 1f), 28, FontStyle.Bold);
        ApplyMenuSpriteButtonStyle(gachaRewardsButton, 11, new Color(0.16f, 0.12f, 0.09f, 0.98f), new Color(0.96f, 0.88f, 0.60f, 1f), 18, FontStyle.Bold);
        ApplyMenuSpriteButtonStyle(gachaSinglePullButton, 0, new Color(0.93f, 0.76f, 0.28f, 1f), new Color(0.13f, 0.08f, 0.03f, 1f), 18, FontStyle.Bold);
        ApplyMenuSpriteButtonStyle(gachaTenPullButton, 11, new Color(0.19f, 0.12f, 0.08f, 0.98f), new Color(0.96f, 0.88f, 0.60f, 1f), 18, FontStyle.Bold);
        ApplyMenuSpriteButtonStyle(gachaCloseButton, 5, new Color(0.18f, 0.11f, 0.08f, 0.98f), new Color(0.96f, 0.88f, 0.60f, 1f), 18, FontStyle.Bold);
        ApplyMenuSpriteButtonStyle(gachaRewardsCloseButton, 5, new Color(0.18f, 0.11f, 0.08f, 0.98f), new Color(0.96f, 0.88f, 0.60f, 1f), 18, FontStyle.Bold);
    }

    private void ApplyMenuSpriteButtonStyle(Button button, int spriteIndex, Color fillColor, Color labelColor, int fontSize, FontStyle fontStyle)
    {
        ApplyGachaButtonStyle(button, fillColor, labelColor, fontSize, fontStyle);

        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetMainMenuSprite(spriteIndex);
            image.type = Image.Type.Simple;
            image.color = fillColor;
        }

        Shadow shadow = button.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = button.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
        shadow.effectDistance = new Vector2(0f, -2f);
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

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            LocalizedFontResolver.ApplyTo(tmpLabel, fallbackTmpFont);
            tmpLabel.fontSize = fontSize;
            tmpLabel.fontStyle = fontStyle == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
            tmpLabel.alignment = TextAlignmentOptions.Center;
            tmpLabel.color = labelColor;
            tmpLabel.enableAutoSizing = true;
            tmpLabel.fontSizeMin = Mathf.Max(10, fontSize - 6);
            tmpLabel.fontSizeMax = fontSize;
        }

        TMP_Text convertedLabel = tmpLabel != null ? tmpLabel : EnsureTmpTextInChildren(button.gameObject);
        if (convertedLabel != null)
        {
            LocalizedFontResolver.ApplyTo(convertedLabel, fallbackTmpFont);
            convertedLabel.fontSize = fontSize;
            convertedLabel.fontStyle = fontStyle == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
            convertedLabel.alignment = TextAlignmentOptions.Center;
            convertedLabel.color = labelColor;
            convertedLabel.enableAutoSizing = true;
            convertedLabel.fontSizeMin = Mathf.Max(10, fontSize - 6);
            convertedLabel.fontSizeMax = fontSize;
        }
    }

    private TMP_Text EnsureTmpTextInChildren(GameObject root)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text tmpText = root.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            LocalizedFontResolver.ApplyTo(tmpText, fallbackTmpFont);
            return tmpText;
        }

        Text legacyText = root.GetComponentInChildren<Text>(true);
        return legacyText != null ? EnsureTmpTextComponent(legacyText.gameObject) : null;
    }

    private TMP_Text EnsureTmpTextComponent(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        TMP_Text tmpText = target.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            LocalizedFontResolver.ApplyTo(tmpText, fallbackTmpFont);
            return tmpText;
        }

        Text legacyText = target.GetComponent<Text>();
        if (legacyText == null)
        {
            return null;
        }

        Transform existingTmpTransform = target.transform.Find("TMPLabel");
        TextMeshProUGUI convertedText = existingTmpTransform != null
            ? existingTmpTransform.GetComponent<TextMeshProUGUI>()
            : null;

        if (convertedText == null)
        {
            GameObject tmpObject = existingTmpTransform != null
                ? existingTmpTransform.gameObject
                : CreateUIObject("TMPLabel", target.transform);
            convertedText = tmpObject.GetComponent<TextMeshProUGUI>();
            if (convertedText == null)
            {
                convertedText = tmpObject.AddComponent<TextMeshProUGUI>();
            }
        }

        RectTransform legacyRect = legacyText.rectTransform;
        RectTransform tmpRect = convertedText.rectTransform;
        tmpRect.anchorMin = Vector2.zero;
        tmpRect.anchorMax = Vector2.one;
        tmpRect.pivot = legacyRect.pivot;
        tmpRect.anchoredPosition = Vector2.zero;
        tmpRect.sizeDelta = Vector2.zero;
        tmpRect.offsetMin = Vector2.zero;
        tmpRect.offsetMax = Vector2.zero;

        CopyLegacyTextSettings(legacyText, convertedText);
        legacyText.enabled = false;

        return convertedText;
    }

    private void CopyLegacyTextSettings(Text legacyText, TextMeshProUGUI tmpText)
    {
        if (legacyText == null || tmpText == null)
        {
            return;
        }

        tmpText.font = LocalizedFontResolver.ResolveTmpFont(fallbackTmpFont != null ? fallbackTmpFont : TMP_Settings.defaultFontAsset);
        tmpText.text = legacyText.text;
        tmpText.fontSize = legacyText.fontSize;
        tmpText.fontStyle = ConvertFontStyle(legacyText.fontStyle);
        tmpText.alignment = ConvertAlignment(legacyText.alignment);
        tmpText.color = legacyText.color;
        tmpText.raycastTarget = legacyText.raycastTarget;
        tmpText.richText = legacyText.supportRichText;
        tmpText.lineSpacing = legacyText.lineSpacing;
        tmpText.enableWordWrapping = legacyText.horizontalOverflow != HorizontalWrapMode.Overflow;
        tmpText.enableAutoSizing = legacyText.resizeTextForBestFit;
        tmpText.fontSizeMin = legacyText.resizeTextMinSize;
        tmpText.fontSizeMax = legacyText.resizeTextMaxSize > 0 ? legacyText.resizeTextMaxSize : legacyText.fontSize;
        tmpText.overflowMode = legacyText.horizontalOverflow == HorizontalWrapMode.Overflow
            ? TextOverflowModes.Overflow
            : TextOverflowModes.Truncate;
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
