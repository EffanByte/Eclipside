using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BiomeTitleOverlay : MonoBehaviour
{
    private static BiomeTitleOverlay instance;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 4f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Style")]
    [SerializeField] private float verticalOffset = 24f;
    [SerializeField] private float titleFontSize = 104f;
    [SerializeField] private float shadowFontSizeOffset = 6f;
    [SerializeField] private Color titleColor = new Color(0.98f, 0.95f, 0.88f, 1f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.92f);
    [SerializeField] private Vector2 shadowOffset = new Vector2(6f, -6f);

    private CanvasGroup canvasGroup;
    private RectTransform titleRoot;
    private TextMeshProUGUI shadowText;
    private TextMeshProUGUI mainText;
    private Coroutine activeRoutine;
    private BiomeData activeBiome;

    public static void Show(string biomeName)
    {
        if (string.IsNullOrWhiteSpace(biomeName))
        {
            return;
        }

        EnsureInstance();
        if (instance != null)
        {
            instance.activeBiome = null;
            instance.PlayTitle(biomeName);
        }
    }

    public static void Show(BiomeData biome)
    {
        if (biome == null || string.IsNullOrWhiteSpace(biome.biomeName))
        {
            return;
        }

        EnsureInstance();
        if (instance != null)
        {
            instance.activeBiome = biome;
            instance.PlayTitle(biome.GetDisplayName());
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<BiomeTitleOverlay>();
        if (instance != null)
        {
            instance.EnsureUiBuilt();
            return;
        }

        GameObject root = new GameObject("BiomeTitleOverlay", typeof(RectTransform));
        instance = root.AddComponent<BiomeTitleOverlay>();
        instance.EnsureUiBuilt();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureUiBuilt();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnEnable()
    {
        LocalizationManager.EnsureExists();
        LocalizationManager.LanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= HandleLanguageChanged;
    }

    private void EnsureUiBuilt()
    {
        if (canvasGroup != null && mainText != null && shadowText != null)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2500;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            GraphicRaycaster raycaster = gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
        {
            StretchRect(rootRect, Vector2.zero, Vector2.zero);
        }

        titleRoot = CreateRect("TitleRoot", transform);
        titleRoot.anchorMin = new Vector2(0.5f, 0.5f);
        titleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        titleRoot.pivot = new Vector2(0.5f, 0.5f);
        titleRoot.sizeDelta = new Vector2(1640f, 240f);
        titleRoot.anchoredPosition = new Vector2(0f, verticalOffset);

        shadowText = CreateText("ShadowText", titleRoot, shadowColor, titleFontSize + shadowFontSizeOffset);
        shadowText.rectTransform.anchoredPosition = shadowOffset;

        mainText = CreateText("MainText", titleRoot, titleColor, titleFontSize);
    }

    private void PlayTitle(string biomeName)
    {
        EnsureUiBuilt();

        mainText.text = biomeName.ToUpperInvariant();
        shadowText.text = mainText.text;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(AnimateTitle());
    }

    private void HandleLanguageChanged()
    {
        if (activeBiome == null || mainText == null || shadowText == null)
        {
            return;
        }

        string localizedName = activeBiome.GetDisplayName();
        if (string.IsNullOrWhiteSpace(localizedName))
        {
            return;
        }

        mainText.text = localizedName.ToUpperInvariant();
        shadowText.text = mainText.text;
    }

    private IEnumerator AnimateTitle()
    {
        canvasGroup.alpha = 0f;
        SetScale(0.94f);

        yield return FadeAndScale(0f, 1f, 0.94f, 1f, fadeInDuration);
        yield return WaitForSecondsRealtimeSafe(holdDuration);
        yield return FadeAndScale(1f, 0f, 1f, 1.04f, fadeOutDuration);

        canvasGroup.alpha = 0f;
        activeRoutine = null;
    }

    private IEnumerator FadeAndScale(float startAlpha, float endAlpha, float startScale, float endScale, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = endAlpha;
            SetScale(endScale);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
            SetScale(Mathf.Lerp(startScale, endScale, eased));
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        SetScale(endScale);
    }

    private IEnumerator WaitForSecondsRealtimeSafe(float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetScale(float scale)
    {
        if (titleRoot != null)
        {
            titleRoot.localScale = Vector3.one * scale;
        }
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, Color color, float fontSize)
    {
        RectTransform rect = CreateRect(objectName, parent);
        StretchRect(rect, Vector2.zero, Vector2.zero);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = ResolveFont();
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = LocalizedFontResolver.ResolveTmpFont(TMP_Settings.defaultFontAsset);
        if (font != null)
        {
            return font;
        }

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject.GetComponent<RectTransform>();
    }

    private void StretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }
}
