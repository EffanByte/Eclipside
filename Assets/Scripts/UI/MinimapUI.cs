using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MinimapUI : MonoBehaviour
{
    [Header("HUD Placement")]
    [SerializeField] private string minimapMaskPath = "SpecialContainer/EclipseOuter";
    [SerializeField] private string minimapImagePath = "SpecialContainer/EclipseOuter/EclipseInside";
    [SerializeField] private float minimapInset = 10f;

    [Header("Camera")]
    [SerializeField, Min(64)] private int textureSize = 256;
    [SerializeField, Min(1f)] private float orthographicSize = 8f;
    [SerializeField] private float cameraZ = -10f;
    [SerializeField] private Color backgroundColor = new Color(0.035f, 0.055f, 0.06f, 1f);

    [Header("Visuals")]
    [SerializeField] private Color borderColor = new Color(0.02f, 0.02f, 0.02f, 0.92f);
    [SerializeField] private Color markerColor = new Color(0.18f, 0.85f, 1f, 1f);
    [SerializeField, Min(2f)] private float markerSize = 12f;

    private const string BorderName = "Minimap Border";
    private const string MarkerName = "Minimap Player Marker";
    private const string ImageName = "Minimap Texture Image";
    private const int MinTextureSize = 64;

    private Camera minimapCamera;
    private RawImage minimapImage;
    private RenderTexture minimapTexture;
    private Transform target;
    private bool initialized;
    private float nextTargetSearchTime;

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (minimapCamera != null)
        {
            minimapCamera.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (minimapCamera != null)
        {
            minimapCamera.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (target == null && Time.unscaledTime >= nextTargetSearchTime)
        {
            nextTargetSearchTime = Time.unscaledTime + 0.5f;
            FindTarget();
        }

        if (target == null || minimapCamera == null)
        {
            return;
        }

        Vector3 targetPosition = target.position;
        minimapCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, cameraZ);
        minimapCamera.orthographicSize = orthographicSize;
    }

    private void OnDestroy()
    {
        if (minimapImage != null && minimapImage.texture == minimapTexture)
        {
            minimapImage.texture = null;
        }

        if (minimapCamera != null)
        {
            Destroy(minimapCamera.gameObject);
        }

        if (minimapTexture != null)
        {
            minimapTexture.Release();
            Destroy(minimapTexture);
        }
    }

    private void Initialize()
    {
        RectTransform maskTransform = FindRectTransform(minimapMaskPath);
        RectTransform imageTransform = FindRectTransform(minimapImagePath);

        if (maskTransform == null || imageTransform == null)
        {
            return;
        }

        PrepareMask(maskTransform);
        CreateRenderTexture();
        CreateBorder(maskTransform);
        PrepareImage(maskTransform, imageTransform);
        CreatePlayerMarker(maskTransform);
        CreateCamera();
        FindTarget();

        initialized = true;
    }

    private RectTransform FindRectTransform(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found as RectTransform : null;
    }

    private void PrepareMask(RectTransform maskTransform)
    {
        Mask mask = maskTransform.GetComponent<Mask>();
        if (mask == null)
        {
            mask = maskTransform.gameObject.AddComponent<Mask>();
        }

        mask.showMaskGraphic = false;

        Graphic maskGraphic = maskTransform.GetComponent<Graphic>();
        if (maskGraphic != null)
        {
            maskGraphic.raycastTarget = false;
        }
    }

    private void CreateRenderTexture()
    {
        if (minimapTexture != null)
        {
            return;
        }

        int safeTextureSize = Mathf.Max(MinTextureSize, textureSize);
        minimapTexture = new RenderTexture(safeTextureSize, safeTextureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "Minimap Render Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1
        };
        minimapTexture.Create();
    }

    private void CreateBorder(RectTransform maskTransform)
    {
        RectTransform borderTransform = maskTransform.Find(BorderName) as RectTransform;
        if (borderTransform == null)
        {
            GameObject borderObject = new GameObject(BorderName, typeof(RectTransform), typeof(CanvasRenderer), typeof(CircleGraphic));
            borderObject.layer = maskTransform.gameObject.layer;
            borderTransform = borderObject.GetComponent<RectTransform>();
            borderTransform.SetParent(maskTransform, false);
        }

        borderTransform.SetAsFirstSibling();
        StretchToParent(borderTransform, Vector2.zero);

        CircleGraphic border = borderTransform.GetComponent<CircleGraphic>();
        border.color = borderColor;
        border.raycastTarget = false;
    }

    private void PrepareImage(RectTransform maskTransform, RectTransform imageTransform)
    {
        imageTransform.SetParent(maskTransform, false);
        imageTransform.SetAsLastSibling();
        StretchToParent(imageTransform, Vector2.one * minimapInset);

        foreach (CircleGraphic circle in imageTransform.GetComponents<CircleGraphic>())
        {
            circle.enabled = false;
        }

        Graphic existingGraphic = imageTransform.GetComponent<Graphic>();
        if (existingGraphic != null)
        {
            existingGraphic.raycastTarget = false;
        }

        SpecialMeterFill specialMeter = imageTransform.GetComponent<SpecialMeterFill>();
        if (specialMeter != null)
        {
            Destroy(specialMeter);
        }

        RectTransform rawImageTransform = imageTransform.Find(ImageName) as RectTransform;
        if (rawImageTransform == null)
        {
            GameObject rawImageObject = new GameObject(ImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            rawImageObject.layer = imageTransform.gameObject.layer;
            rawImageTransform = rawImageObject.GetComponent<RectTransform>();
            rawImageTransform.SetParent(imageTransform, false);
        }

        rawImageTransform.SetAsLastSibling();
        StretchToParent(rawImageTransform, Vector2.zero);

        minimapImage = rawImageTransform.GetComponent<RawImage>();
        if (minimapImage == null)
        {
            minimapImage = rawImageTransform.gameObject.AddComponent<RawImage>();
        }

        minimapImage.texture = minimapTexture;
        minimapImage.color = Color.white;
        minimapImage.raycastTarget = false;
    }

    private void CreatePlayerMarker(RectTransform maskTransform)
    {
        RectTransform markerTransform = maskTransform.Find(MarkerName) as RectTransform;
        if (markerTransform == null)
        {
            GameObject markerObject = new GameObject(MarkerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(CircleGraphic), typeof(Outline));
            markerObject.layer = maskTransform.gameObject.layer;
            markerTransform = markerObject.GetComponent<RectTransform>();
            markerTransform.SetParent(maskTransform, false);
        }

        markerTransform.SetAsLastSibling();
        markerTransform.anchorMin = new Vector2(0.5f, 0.5f);
        markerTransform.anchorMax = new Vector2(0.5f, 0.5f);
        markerTransform.pivot = new Vector2(0.5f, 0.5f);
        markerTransform.anchoredPosition = Vector2.zero;
        markerTransform.sizeDelta = Vector2.one * markerSize;

        CircleGraphic marker = markerTransform.GetComponent<CircleGraphic>();
        marker.color = markerColor;
        marker.raycastTarget = false;

        Outline outline = markerTransform.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;
    }

    private void StretchToParent(RectTransform rectTransform, Vector2 inset)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = inset;
        rectTransform.offsetMax = -inset;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private void CreateCamera()
    {
        if (minimapCamera != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Minimap Camera");
        minimapCamera = cameraObject.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = orthographicSize;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = backgroundColor;
        minimapCamera.targetTexture = minimapTexture;
        minimapCamera.allowHDR = false;
        minimapCamera.allowMSAA = false;
        minimapCamera.depth = -100f;

        int uiLayer = LayerMask.NameToLayer("UI");
        minimapCamera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : Physics.DefaultRaycastLayers;
    }

    private void FindTarget()
    {
        if (PlayerController.Instance != null)
        {
            target = PlayerController.Instance.transform;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        target = player != null ? player.transform : null;
    }
}
