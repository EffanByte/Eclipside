using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerStatusEffectHud : MonoBehaviour
{
    private const float IconSize = 52f;
    private const string PixelMenuFontAssetPath = "Assets/Fonts/PixelifySans.asset";

    [System.Serializable]
    private struct StatusSpriteBinding
    {
        public StatusType statusType;
        public Sprite sprite;
    }

    private sealed class StatusSlot
    {
        public GameObject Root;
        public Image IconImage;
        public Image BackgroundImage;
        public Image TimerLine;
        public TextMeshProUGUI Label;
    }

    [SerializeField] private List<StatusSpriteBinding> statusSprites = new List<StatusSpriteBinding>();
    [SerializeField] private TMP_FontAsset pixelFont;

    private static PlayerStatusEffectHud instance;

    private readonly Dictionary<StatusType, StatusSlot> slots = new Dictionary<StatusType, StatusSlot>();
    private readonly Dictionary<StatusType, Sprite> spriteLookup = new Dictionary<StatusType, Sprite>();
    private readonly StatusType[] displayedStatuses =
    {
        StatusType.Burn,
        StatusType.Poison,
        StatusType.Freeze,
        StatusType.Confusion,
        StatusType.Fragile
    };

    private StatusManager targetStatusManager;
    private RectTransform container;
    private TMP_FontAsset fallbackFont;

    public static void EnsureExists(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = FindFirstObjectByType<PlayerStatusEffectHud>(FindObjectsInactive.Include);
        }

        if (instance == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[StatusHud] Could not create debuff HUD because no Canvas was found.");
                return;
            }

            GameObject root = new GameObject("PlayerStatusEffectHud", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            instance = root.AddComponent<PlayerStatusEffectHud>();
        }

        instance.Bind(player.GetStatusManager());
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveFont();
        RebuildSpriteLookup();
        BuildUi();
    }

    private void Update()
    {
        if (targetStatusManager == null && PlayerController.Instance != null)
        {
            Bind(PlayerController.Instance.GetStatusManager());
        }

        RefreshSlots();
    }

    private void OnDisable()
    {
        Unbind();
    }

    public void Bind(StatusManager statusManager)
    {
        if (targetStatusManager == statusManager)
        {
            RefreshSlots();
            return;
        }

        Unbind();
        targetStatusManager = statusManager;

        if (targetStatusManager != null)
        {
            targetStatusManager.OnStatusApplied += HandleStatusChanged;
            targetStatusManager.OnStatusRemoved += HandleStatusChanged;
        }

        RefreshSlots();
    }

    private void Unbind()
    {
        if (targetStatusManager == null)
        {
            return;
        }

        targetStatusManager.OnStatusApplied -= HandleStatusChanged;
        targetStatusManager.OnStatusRemoved -= HandleStatusChanged;
        targetStatusManager = null;
    }

    private void HandleStatusChanged(StatusType _)
    {
        RefreshSlots();
    }

    private void ResolveFont()
    {
        fallbackFont = LocalizedFontResolver.ResolveTmpFont(pixelFont != null ? pixelFont : TMP_Settings.defaultFontAsset);
#if UNITY_EDITOR
        if (fallbackFont == null)
        {
            fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelMenuFontAssetPath);
        }

        LocalizedFontResolver.EnsureEditorLocaleFontFallbacks(pixelFont, TMP_Settings.defaultFontAsset);
#endif
    }

    private void RebuildSpriteLookup()
    {
        spriteLookup.Clear();
        for (int i = 0; i < statusSprites.Count; i++)
        {
            if (statusSprites[i].sprite != null)
            {
                spriteLookup[statusSprites[i].statusType] = statusSprites[i].sprite;
            }
        }
    }

    private void BuildUi()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(420f, 72f);
        rootRect.anchoredPosition = new Vector2(24f, -24f);

        container = new GameObject("StatusRow", typeof(RectTransform)).GetComponent<RectTransform>();
        container.transform.SetParent(transform, false);
        container.anchorMin = new Vector2(0f, 1f);
        container.anchorMax = new Vector2(0f, 1f);
        container.pivot = new Vector2(0f, 1f);
        container.sizeDelta = new Vector2(420f, 64f);
        container.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        for (int i = 0; i < displayedStatuses.Length; i++)
        {
            slots[displayedStatuses[i]] = CreateSlot(displayedStatuses[i]);
        }
    }

    private StatusSlot CreateSlot(StatusType statusType)
    {
        GameObject root = new GameObject(statusType + "Slot", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        root.transform.SetParent(container, false);
        root.SetActive(false);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = IconSize;
        layout.preferredHeight = IconSize;

        Image background = root.GetComponent<Image>();
        background.color = GetFallbackColor(statusType);

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(IconSize, IconSize);

        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(root.transform, false);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(40f, 40f);

        Image iconImage = icon.GetComponent<Image>();
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(root.transform, false);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = LocalizedFontResolver.ResolveTmpFont(fallbackFont != null ? fallbackFont : TMP_Settings.defaultFontAsset);
        label.fontSize = 15f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.98f, 0.95f, 0.88f, 1f);
        label.text = GetStatusLabel(statusType);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -4f);

        GameObject lineObject = new GameObject("TimerLine", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(root.transform, false);
        Image timerLine = lineObject.GetComponent<Image>();
        timerLine.color = new Color(0f, 0f, 0f, 0.78f);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 1f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.sizeDelta = new Vector2(-8f, 4f);
        lineRect.anchoredPosition = new Vector2(0f, -4f);

        return new StatusSlot
        {
            Root = root,
            IconImage = iconImage,
            BackgroundImage = background,
            TimerLine = timerLine,
            Label = label
        };
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < displayedStatuses.Length; i++)
        {
            StatusType statusType = displayedStatuses[i];
            StatusSlot slot = slots[statusType];

            bool active = targetStatusManager != null && targetStatusManager.HasStatus(statusType);
            slot.Root.SetActive(active);
            if (!active)
            {
                continue;
            }

            Sprite iconSprite = null;
            spriteLookup.TryGetValue(statusType, out iconSprite);
            slot.IconImage.sprite = iconSprite;
            slot.IconImage.enabled = iconSprite != null;
            slot.Label.gameObject.SetActive(iconSprite == null);
            slot.Label.text = GetStatusLabel(statusType);
            slot.BackgroundImage.color = GetFallbackColor(statusType);

            float total = targetStatusManager.GetTotalDuration(statusType);
            float remaining = targetStatusManager.GetRemainingDuration(statusType);
            float normalized = total > 0.001f ? Mathf.Clamp01(remaining / total) : 0f;
            float travel = IconSize - 12f;
            slot.TimerLine.rectTransform.anchoredPosition = new Vector2(0f, -4f - ((1f - normalized) * travel));
        }
    }

    private string GetStatusLabel(StatusType statusType)
    {
        return statusType switch
        {
            StatusType.Burn => "BRN",
            StatusType.Poison => "PSN",
            StatusType.Freeze => "FRZ",
            StatusType.Confusion => "CNF",
            StatusType.Fragile => "FRG",
            _ => "--"
        };
    }

    private Color GetFallbackColor(StatusType statusType)
    {
        return statusType switch
        {
            StatusType.Burn => new Color(0.82f, 0.30f, 0.14f, 0.92f),
            StatusType.Poison => new Color(0.24f, 0.56f, 0.22f, 0.92f),
            StatusType.Freeze => new Color(0.22f, 0.54f, 0.76f, 0.92f),
            StatusType.Confusion => new Color(0.48f, 0.28f, 0.70f, 0.92f),
            StatusType.Fragile => new Color(0.58f, 0.46f, 0.24f, 0.92f),
            _ => new Color(0.24f, 0.24f, 0.24f, 0.92f)
        };
    }
}
