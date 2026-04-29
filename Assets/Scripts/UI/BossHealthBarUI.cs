using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    private const string BossCanvasObjectName = "BossHealthCanvas";

    [Header("Sprites")]
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Sprite backgroundSprite;

    [Header("Layout")]
    [SerializeField] private Vector2 frameSize = new Vector2(700f, 92f);
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -52f);
    [SerializeField] private Vector2 innerPadding = new Vector2(40f, 28f);

    private Canvas canvasRoot;
    private GameObject rootObject;
    private Image frameImage;
    private Image backgroundImage;
    private Image fillImage;
    private BossBase trackedBoss;

    public void Configure(Sprite fill, Sprite frame, Sprite background)
    {
        fillSprite = fill;
        frameSprite = frame;
        backgroundSprite = background;
        ApplyConfiguredSprites();
    }

    private void OnEnable()
    {
        EnsureBuilt();
        Hide();
        BossBase.OnBossHealthChanged += HandleBossHealthChanged;
        BossBase.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        BossBase.OnBossHealthChanged -= HandleBossHealthChanged;
        BossBase.OnBossDefeated -= HandleBossDefeated;
    }

    private void EnsureBuilt()
    {
        EnsureCanvasRoot();

        if (rootObject != null)
        {
            return;
        }

        rootObject = new GameObject("BossHealthBarRoot", typeof(RectTransform));
        rootObject.transform.SetParent(canvasRoot.transform, false);

        RectTransform frameRect = rootObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 1f);
        frameRect.anchorMax = new Vector2(0.5f, 1f);
        frameRect.pivot = new Vector2(0.5f, 1f);
        frameRect.sizeDelta = frameSize;
        frameRect.anchoredPosition = anchoredPosition;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(rootObject.transform, false);
        backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.raycastTarget = false;

        RectTransform bgRect = backgroundObject.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = innerPadding;
        bgRect.offsetMax = new Vector2(-innerPadding.x, -innerPadding.y);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);
        fillImage = fillObject.GetComponent<Image>();
        fillImage.sprite = fillSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;
        fillImage.color = fillSprite != null ? Color.white : new Color(0.73f, 0.08f, 0.09f, 1f);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObject.transform.SetParent(rootObject.transform, false);
        frameImage = frameObject.GetComponent<Image>();
        frameImage.type = Image.Type.Simple;
        frameImage.preserveAspect = false;
        frameImage.raycastTarget = false;

        RectTransform frameImageRect = frameObject.GetComponent<RectTransform>();
        frameImageRect.anchorMin = Vector2.zero;
        frameImageRect.anchorMax = Vector2.one;
        frameImageRect.offsetMin = Vector2.zero;
        frameImageRect.offsetMax = Vector2.zero;

        ApplyConfiguredSprites();
    }

    private void HandleBossHealthChanged(BossBase boss, float currentHealth, float maxHealth)
    {
        EnsureBuilt();
        trackedBoss = boss;

        if (rootObject != null && !rootObject.activeSelf)
        {
            rootObject.SetActive(true);
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    private void EnsureCanvasRoot()
    {
        if (canvasRoot != null)
        {
            return;
        }

        Transform existingCanvas = transform.Find(BossCanvasObjectName);
        GameObject canvasObject = existingCanvas != null
            ? existingCanvas.gameObject
            : new GameObject(BossCanvasObjectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        if (canvasObject.transform.parent != transform)
        {
            canvasObject.transform.SetParent(transform, false);
        }

        canvasRoot = canvasObject.GetComponent<Canvas>();
        canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRoot.sortingOrder = 250;
        canvasRoot.overrideSorting = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;
    }

    private void HandleBossDefeated(BossBase boss)
    {
        if (trackedBoss != null && boss != trackedBoss)
        {
            return;
        }

        trackedBoss = null;
        Hide();
    }

    private void Hide()
    {
        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    private void ApplyConfiguredSprites()
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.color = backgroundSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.85f);
        }

        if (fillImage != null)
        {
            fillImage.sprite = fillSprite;
            fillImage.color = fillSprite != null ? Color.white : new Color(0.73f, 0.08f, 0.09f, 1f);
        }

        if (frameImage != null)
        {
            frameImage.sprite = frameSprite;
            frameImage.color = frameSprite != null ? Color.white : new Color(0.10f, 0.06f, 0.04f, 0.95f);
        }
    }
}
