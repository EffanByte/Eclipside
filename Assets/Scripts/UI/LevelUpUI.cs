using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelUpUI : MonoBehaviour
{
    private const string PixelMenuFontAssetPath = "Assets/Fonts/PixelifySans.asset";
    private const string MainMenuSpriteSheetAssetPath = "Assets/UI/main menu sprite sheet.png";

    [Header("Optional Art Overrides")]
    [SerializeField] private Sprite[] mainMenuSpriteSheetSprites;
    [SerializeField] private TMP_FontAsset pixelMenuFont;

    private readonly List<StatType> possibleUpgrades = new List<StatType>
    {
        StatType.BaseDamage,
        StatType.MagicDamage,
        StatType.HeavyDamage,
        StatType.MaxHealth,
        StatType.Speed,
        StatType.AttackSpeed,
    };

    private readonly List<StatType> activeOptions = new List<StatType>();

    private GameObject panel;
    private GameObject cardRoot;
    private RectTransform buttonsContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI promptText;
    private TMP_FontAsset fallbackFont;

    private void OnValidate()
    {
#if UNITY_EDITOR
        ResolveArtReferences();
#endif
    }

    private void Start()
    {
        panel = gameObject;
        ResolveArtReferences();
        BuildUi();
        SetPanelVisible(false);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnLevelUp += ShowLevelUpOptions;
        }
    }

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += RefreshLocalizedTexts;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedTexts;
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnLevelUp -= ShowLevelUpOptions;
        }
    }

    private void ShowLevelUpOptions()
    {
        Time.timeScale = 0f;
        activeOptions.Clear();
        activeOptions.AddRange(GetRandomOptions(3));

        RebuildUpgradeButtons();
        RefreshLocalizedTexts();
        SetPanelVisible(true);
        UINavigationUtility.FocusFirstSelectable(buttonsContainer);
    }

    private void SelectUpgrade(StatType stat)
    {
        PlayerController.Instance.ApplyPermanentUpgrade(stat);
        SetPanelVisible(false);
        Time.timeScale = 1f;
    }

    private void BuildUi()
    {
        HideLegacyChildren();

        Image overlay = gameObject.GetComponent<Image>();
        if (overlay == null)
        {
            overlay = gameObject.AddComponent<Image>();
        }

        overlay.color = new Color(0f, 0f, 0f, 0.82f);

        RectTransform overlayRect = GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject shade = CreateUiObject("EclipseShade", transform);
        Image shadeImage = shade.AddComponent<Image>();
        shadeImage.color = new Color(0.18f, 0.04f, 0.02f, 0.32f);
        StretchToParent(shade.GetComponent<RectTransform>());

        cardRoot = CreateUiObject("LevelUpCard", transform);
        Image cardImage = cardRoot.AddComponent<Image>();
        cardImage.color = new Color32(16, 8, 5, 238);
        if (TryGetSprite(11, out Sprite cardSprite))
        {
            cardImage.sprite = cardSprite;
            cardImage.type = Image.Type.Simple;
            cardImage.color = new Color(0.18f, 0.11f, 0.08f, 0.98f);
        }

        Outline cardOutline = cardRoot.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.88f, 0.55f, 0.18f, 0.72f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        Shadow cardShadow = cardRoot.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        cardShadow.effectDistance = new Vector2(0f, -6f);

        RectTransform cardRect = cardRoot.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1180f, 560f);
        cardRect.anchoredPosition = new Vector2(0f, -18f);

        GameObject headerBand = CreateUiObject("HeaderBand", cardRoot.transform);
        Image headerImage = headerBand.AddComponent<Image>();
        if (TryGetSprite(5, out Sprite headerSprite))
        {
            headerImage.sprite = headerSprite;
            headerImage.type = Image.Type.Simple;
            headerImage.color = new Color(0.92f, 0.76f, 0.48f, 1f);
        }
        else
        {
            headerImage.color = new Color(0.14f, 0.08f, 0.05f, 0.96f);
        }

        RectTransform headerRect = headerBand.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(24f, -84f);
        headerRect.offsetMax = new Vector2(-24f, -24f);

        titleText = CreateText("Title", cardRoot.transform, 36f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.83f, 0.48f, 1f));
        titleText.characterSpacing = 3f;
        ApplyGoldGradient(titleText);
        AddShadow(titleText.gameObject, new Color(0.05f, 0.01f, 0f, 0.95f), new Vector2(0f, -3f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(-220f, 56f);
        titleRect.anchoredPosition = new Vector2(52f, -28f);

        subtitleText = CreateText("Subtitle", cardRoot.transform, 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.78f, 0.69f, 0.48f, 0.96f));
        subtitleText.characterSpacing = 1.2f;
        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(-220f, 34f);
        subtitleRect.anchoredPosition = new Vector2(56f, -88f);

        promptText = CreateText("Prompt", cardRoot.transform, 17f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.92f, 0.84f, 0.62f, 0.96f));
        promptText.characterSpacing = 1.4f;
        RectTransform promptRect = promptText.rectTransform;
        promptRect.anchorMin = new Vector2(0f, 1f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.pivot = new Vector2(0.5f, 1f);
        promptRect.sizeDelta = new Vector2(-120f, 28f);
        promptRect.anchoredPosition = new Vector2(0f, -138f);

        GameObject optionsArea = CreateUiObject("OptionsArea", cardRoot.transform);
        Image optionsAreaImage = optionsArea.AddComponent<Image>();
        optionsAreaImage.color = new Color(0.08f, 0.09f, 0.11f, 0.97f);
        if (TryGetSprite(11, out Sprite optionsSprite))
        {
            optionsAreaImage.sprite = optionsSprite;
            optionsAreaImage.type = Image.Type.Simple;
            optionsAreaImage.color = new Color(0.08f, 0.09f, 0.11f, 0.97f);
        }

        Outline optionsOutline = optionsArea.AddComponent<Outline>();
        optionsOutline.effectColor = new Color(0.43f, 0.26f, 0.11f, 0.55f);
        optionsOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform optionsRect = optionsArea.GetComponent<RectTransform>();
        optionsRect.anchorMin = new Vector2(0f, 0f);
        optionsRect.anchorMax = new Vector2(1f, 1f);
        optionsRect.offsetMin = new Vector2(42f, 34f);
        optionsRect.offsetMax = new Vector2(-42f, -176f);

        GameObject buttonRow = CreateUiObject("ButtonsRow", optionsArea.transform);
        buttonsContainer = buttonRow.GetComponent<RectTransform>();
        buttonsContainer.anchorMin = new Vector2(0f, 0f);
        buttonsContainer.anchorMax = new Vector2(1f, 1f);
        buttonsContainer.offsetMin = new Vector2(26f, 24f);
        buttonsContainer.offsetMax = new Vector2(-26f, -24f);

        HorizontalLayoutGroup layout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private void RebuildUpgradeButtons()
    {
        if (buttonsContainer == null)
        {
            return;
        }

        for (int i = buttonsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonsContainer.GetChild(i).gameObject);
        }

        for (int i = 0; i < activeOptions.Count; i++)
        {
            CreateUpgradeButton(activeOptions[i]);
        }
    }

    private void CreateUpgradeButton(StatType stat)
    {
        GameObject buttonObject = CreateUiObject(stat + "Button", buttonsContainer);
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 0f;
        layout.preferredHeight = 260f;
        layout.flexibleWidth = 1f;

        Image image = buttonObject.AddComponent<Image>();
        if (TryGetSprite(11, out Sprite frameSprite))
        {
            image.sprite = frameSprite;
            image.type = Image.Type.Simple;
            image.color = new Color(0.19f, 0.12f, 0.08f, 0.98f);
        }
        else
        {
            image.color = new Color(0.19f, 0.12f, 0.08f, 0.98f);
        }

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.93f, 0.76f, 0.28f, 1f);
        colors.pressedColor = new Color(0.74f, 0.56f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.22f, 0.20f, 0.20f, 0.94f);
        button.colors = colors;
        button.onClick.AddListener(() => SelectUpgrade(stat));

        buttonObject.AddComponent<SelectableFocusOutline>();
        AddShadow(buttonObject, new Color(0f, 0f, 0f, 0.38f), new Vector2(0f, -4f));

        GameObject accent = CreateUiObject("AccentBand", buttonObject.transform);
        Image accentImage = accent.AddComponent<Image>();
        accentImage.color = GetUpgradeAccentColor(stat);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.offsetMin = new Vector2(22f, -26f);
        accentRect.offsetMax = new Vector2(-22f, -18f);

        TextMeshProUGUI nameText = CreateText("Name", buttonObject.transform, 29f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.98f, 0.95f, 0.88f, 1f));
        nameText.characterSpacing = 1.6f;
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(24f, -88f);
        nameRect.offsetMax = new Vector2(-24f, -34f);
        nameText.text = GetUpgradeName(stat);

        TextMeshProUGUI descText = CreateText("Description", buttonObject.transform, 17f, FontStyles.Bold, TextAlignmentOptions.Top, new Color(0.84f, 0.78f, 0.66f, 0.96f));
        descText.enableWordWrapping = true;
        descText.characterSpacing = 0.8f;
        RectTransform descRect = descText.rectTransform;
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.offsetMin = new Vector2(24f, 34f);
        descRect.offsetMax = new Vector2(-24f, -104f);
        descText.text = GetUpgradeDescription(stat);
    }

    private void RefreshLocalizedTexts()
    {
        if (titleText != null)
        {
            titleText.text = L("levelup.title", "Level Up");
        }

        if (subtitleText != null)
        {
            subtitleText.text = L("levelup.subtitle", "The eclipse bends for no one. Choose one upgrade before the next wave crashes in.");
        }

        if (promptText != null)
        {
            promptText.text = L("levelup.prompt", "Choose One Blessing");
        }

        if (buttonsContainer == null)
        {
            return;
        }

        for (int i = 0; i < activeOptions.Count && i < buttonsContainer.childCount; i++)
        {
            Transform button = buttonsContainer.GetChild(i);
            TextMeshProUGUI nameText = button.Find("Name")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = button.Find("Description")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null)
            {
                nameText.text = GetUpgradeName(activeOptions[i]);
            }

            if (descText != null)
            {
                descText.text = GetUpgradeDescription(activeOptions[i]);
            }
        }
    }

    private List<StatType> GetRandomOptions(int count)
    {
        List<StatType> pool = new List<StatType>(possibleUpgrades);
        List<StatType> selected = new List<StatType>();

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0)
            {
                break;
            }

            int index = Random.Range(0, pool.Count);
            selected.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return selected;
    }

    private string GetUpgradeName(StatType stat)
    {
        return stat switch
        {
            StatType.BaseDamage => L("levelup.base_damage.title", "Melee Damage"),
            StatType.MagicDamage => L("levelup.magic_damage.title", "Magic Damage"),
            StatType.HeavyDamage => L("levelup.heavy_damage.title", "Heavy Damage"),
            StatType.MaxHealth => L("levelup.max_health.title", "Max Health"),
            StatType.Speed => L("levelup.speed.title", "Move Speed"),
            StatType.AttackSpeed => L("levelup.attack_speed.title", "Attack Speed"),
            _ => stat.ToString()
        };
    }

    private string GetUpgradeDescription(StatType stat)
    {
        return stat switch
        {
            StatType.BaseDamage => L("levelup.base_damage.desc", "+10% to your melee weapon damage."),
            StatType.MagicDamage => L("levelup.magic_damage.desc", "+10% to your magic weapon damage."),
            StatType.HeavyDamage => L("levelup.heavy_damage.desc", "+10% to your heavy weapon damage."),
            StatType.MaxHealth => L("levelup.max_health.desc", "+1 maximum heart and a matching heal."),
            StatType.Speed => L("levelup.speed.desc", "+5% movement speed for the rest of the run."),
            StatType.AttackSpeed => L("levelup.attack_speed.desc", "+5% attack speed for all weapons."),
            _ => stat.ToString()
        };
    }

    private Color GetUpgradeAccentColor(StatType stat)
    {
        return stat switch
        {
            StatType.BaseDamage => new Color(0.86f, 0.34f, 0.18f, 1f),
            StatType.MagicDamage => new Color(0.28f, 0.52f, 0.88f, 1f),
            StatType.HeavyDamage => new Color(0.72f, 0.44f, 0.18f, 1f),
            StatType.MaxHealth => new Color(0.76f, 0.20f, 0.24f, 1f),
            StatType.Speed => new Color(0.24f, 0.66f, 0.44f, 1f),
            StatType.AttackSpeed => new Color(0.82f, 0.70f, 0.24f, 1f),
            _ => new Color(0.92f, 0.76f, 0.28f, 1f)
        };
    }

    private void ApplyGoldGradient(TextMeshProUGUI text)
    {
        text.colorGradient = new VertexGradient(
            new Color32(255, 218, 135, 255),
            new Color32(255, 205, 112, 255),
            new Color32(132, 69, 34, 255),
            new Color32(108, 49, 25, 255));
    }

    private void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
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

    private void SetPanelVisible(bool visible)
    {
        panel.SetActive(visible);
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = LocalizedFontResolver.ResolveTmpFont(fallbackFont != null ? fallbackFont : TMP_Settings.defaultFontAsset);
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        return text;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created;
    }

    private void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private string L(string key, string fallback, params object[] args)
    {
        return LocalizationManager.GetString(LocalizationManager.DefaultTable, key, fallback, args);
    }

    private bool TryGetSprite(int index, out Sprite sprite)
    {
        sprite = null;
        return mainMenuSpriteSheetSprites != null
            && index >= 0
            && index < mainMenuSpriteSheetSprites.Length
            && (sprite = mainMenuSpriteSheetSprites[index]) != null;
    }

    private void ResolveArtReferences()
    {
        fallbackFont = LocalizedFontResolver.ResolveTmpFont(pixelMenuFont != null ? pixelMenuFont : TMP_Settings.defaultFontAsset);

#if UNITY_EDITOR
        if (pixelMenuFont == null)
        {
            pixelMenuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelMenuFontAssetPath);
        }

        LocalizedFontResolver.EnsureEditorLocaleFontFallbacks(pixelMenuFont, TMP_Settings.defaultFontAsset);

        if (mainMenuSpriteSheetSprites == null || mainMenuSpriteSheetSprites.Length < 12)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(MainMenuSpriteSheetAssetPath);
            List<Sprite> sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            if (sprites.Count > 0)
            {
                sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
                mainMenuSpriteSheetSprites = sprites.ToArray();
            }
        }

        fallbackFont = LocalizedFontResolver.ResolveTmpFont(pixelMenuFont != null ? pixelMenuFont : TMP_Settings.defaultFontAsset);
#endif
    }
}
