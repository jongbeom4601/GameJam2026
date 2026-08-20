using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a non-interactive top-screen gauge for the player's remaining loop time.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerTimeGaugeUI : MonoBehaviour
{
    private const int LavaFrameCount = 6;

    [Header("Gauge Assets")]
    [SerializeField] private Sprite gaugeFrame;
    [SerializeField] private Texture2D lavaTexture;

    [Header("Layout")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Tooltip("The gauge always stretches across the full screen. Y controls its thickness; X is unused.")]
    [SerializeField] private Vector2 gaugeSize = new Vector2(0f, 96f);
    [SerializeField] private Vector2 topOffset = Vector2.zero;
    [SerializeField, Min(0f)] private float horizontalPadding = 12f;
    [SerializeField, Min(0f)] private float verticalPadding = 10f;

    [Header("Lava Fill")]
    [SerializeField] private Color fillColor = new Color32(249, 146, 82, 255);
    [Tooltip("Full width of the enlarged, rotated lava frame before cropping.")]
    [SerializeField, Min(1f)] private float lavaEdgeWidth = 192f;

    [Tooltip("Width of the right-facing animated surface that remains visible after cropping.")]
    [SerializeField, Min(1f)] private float lavaVisibleSurfaceWidth = 96f;

    [SerializeField, Min(1f)] private float lavaFramesPerSecond = 8f;

    [Header("Rendering")]
    [SerializeField] private int sortingOrder = 200;

    private PlayerController timeSource;
    private GameObject canvasObject;
    private RectTransform gaugeRectTransform;
    private RectTransform solidFillRectTransform;
    private RectTransform lavaEdgeMaskRectTransform;
    private RectTransform lavaEdgeRectTransform;
    private Image solidFillImage;
    private Image lavaEdgeImage;
    private Sprite[] lavaFrames;
    private float fullFillWidth;
    private float fullFillHeight;
    private float lavaAnimationTime;

    private void Awake()
    {
        timeSource = GetComponent<PlayerController>();
        CreateLavaFrames();
        CreateGauge();
        UpdateGauge(1f);
    }

    private void OnEnable()
    {
        if (canvasObject != null)
        {
            canvasObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (timeSource == null || lavaEdgeImage == null)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, timeSource.ReturnDurationSeconds);
        float elapsedProgress = Mathf.Clamp01(timeSource.ElapsedReturnSeconds / duration);
        float remainingProgress = timeSource.IsReturning
            ? timeSource.ReturnProgress
            : 1f - elapsedProgress;

        lavaAnimationTime += Time.unscaledDeltaTime;
        UpdateGauge(remainingProgress);
    }

    private void OnDisable()
    {
        if (canvasObject != null)
        {
            canvasObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        if (lavaFrames == null)
        {
            return;
        }

        foreach (Sprite frame in lavaFrames)
        {
            if (frame != null)
            {
                Destroy(frame);
            }
        }
    }

    private void CreateLavaFrames()
    {
        if (lavaTexture == null || lavaTexture.width < LavaFrameCount)
        {
            return;
        }

        int frameWidth = lavaTexture.width / LavaFrameCount;
        int frameHeight = lavaTexture.height;
        lavaFrames = new Sprite[LavaFrameCount];

        for (int i = 0; i < LavaFrameCount; i++)
        {
            lavaFrames[i] = Sprite.Create(
                lavaTexture,
                new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            lavaFrames[i].name = $"Lava Gauge Edge {i + 1}";
        }
    }

    private void CreateGauge()
    {
        if (gaugeFrame == null || lavaFrames == null || lavaFrames.Length == 0)
        {
            Debug.LogWarning("Time gauge assets are not assigned.", this);
            return;
        }

        canvasObject = new GameObject(
            "Time Loop Gauge Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        Camera renderCamera = Camera.main;
        if (renderCamera == null)
        {
            renderCamera = FindAnyObjectByType<Camera>();
        }

        if (renderCamera != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = renderCamera;
            canvas.planeDistance = Mathf.Max(
                0.1f,
                renderCamera.nearClipPlane + 0.01f);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.pixelPerfect = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject gaugeObject = new GameObject("Time Loop Gauge", typeof(RectTransform));
        gaugeObject.transform.SetParent(canvasObject.transform, false);

        gaugeRectTransform = gaugeObject.GetComponent<RectTransform>();
        gaugeRectTransform.anchorMin = new Vector2(0f, 1f);
        gaugeRectTransform.anchorMax = new Vector2(1f, 1f);
        gaugeRectTransform.pivot = new Vector2(0.5f, 1f);
        gaugeRectTransform.anchoredPosition = topOffset;
        gaugeRectTransform.sizeDelta = new Vector2(0f, gaugeSize.y);

        Canvas.ForceUpdateCanvases();

        CreateFrame(gaugeObject.transform);
        CreateFill(gaugeObject.transform);
    }

    private void CreateFrame(Transform gaugeTransform)
    {
        GameObject frameObject = new GameObject(
            "Gauge Frame",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        frameObject.transform.SetParent(gaugeTransform, false);

        StretchToParent(frameObject.GetComponent<RectTransform>());

        Image frameImage = frameObject.GetComponent<Image>();
        frameImage.sprite = gaugeFrame;
        frameImage.type = Image.Type.Sliced;
        frameImage.preserveAspect = false;
        frameImage.raycastTarget = false;
    }

    private void CreateFill(Transform gaugeTransform)
    {
        GameObject fillAreaObject = new GameObject(
            "Lava Fill Area",
            typeof(RectTransform),
            typeof(RectMask2D));
        fillAreaObject.transform.SetParent(gaugeTransform, false);

        RectTransform fillAreaRectTransform = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRectTransform.anchorMin = Vector2.zero;
        fillAreaRectTransform.anchorMax = Vector2.one;
        fillAreaRectTransform.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        fillAreaRectTransform.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);

        fullFillWidth = GetAvailableFillWidth();
        fullFillHeight = Mathf.Max(1f, gaugeSize.y - verticalPadding * 2f);

        GameObject solidFillObject = new GameObject(
            "Solid Orange Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        solidFillObject.transform.SetParent(fillAreaObject.transform, false);

        solidFillRectTransform = solidFillObject.GetComponent<RectTransform>();
        solidFillRectTransform.anchorMin = new Vector2(0f, 0f);
        solidFillRectTransform.anchorMax = new Vector2(0f, 1f);
        solidFillRectTransform.pivot = new Vector2(0f, 0.5f);
        solidFillRectTransform.anchoredPosition = Vector2.zero;

        solidFillImage = solidFillObject.GetComponent<Image>();
        solidFillImage.color = fillColor;
        solidFillImage.raycastTarget = false;

        GameObject lavaEdgeMaskObject = new GameObject(
            "Lava Edge Crop",
            typeof(RectTransform),
            typeof(RectMask2D));
        lavaEdgeMaskObject.transform.SetParent(fillAreaObject.transform, false);

        lavaEdgeMaskRectTransform = lavaEdgeMaskObject.GetComponent<RectTransform>();
        lavaEdgeMaskRectTransform.anchorMin = new Vector2(0f, 0.5f);
        lavaEdgeMaskRectTransform.anchorMax = new Vector2(0f, 0.5f);
        lavaEdgeMaskRectTransform.pivot = new Vector2(0f, 0.5f);
        lavaEdgeMaskRectTransform.sizeDelta = new Vector2(
            lavaVisibleSurfaceWidth,
            fullFillHeight);

        GameObject lavaEdgeObject = new GameObject(
            "Animated Lava Edge",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        lavaEdgeObject.transform.SetParent(lavaEdgeMaskObject.transform, false);

        lavaEdgeRectTransform = lavaEdgeObject.GetComponent<RectTransform>();
        lavaEdgeRectTransform.anchorMin = new Vector2(0f, 0.5f);
        lavaEdgeRectTransform.anchorMax = new Vector2(0f, 0.5f);
        lavaEdgeRectTransform.pivot = new Vector2(0.5f, 0.5f);
        lavaEdgeRectTransform.sizeDelta = new Vector2(fullFillHeight, lavaEdgeWidth);
        lavaEdgeRectTransform.anchoredPosition = new Vector2(
            lavaVisibleSurfaceWidth - lavaEdgeWidth * 0.5f,
            0f);
        lavaEdgeRectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);

        lavaEdgeImage = lavaEdgeObject.GetComponent<Image>();
        lavaEdgeImage.sprite = lavaFrames[0];
        lavaEdgeImage.preserveAspect = false;
        lavaEdgeImage.raycastTarget = false;
    }

    private void UpdateGauge(float remainingProgress)
    {
        if (solidFillRectTransform == null || lavaEdgeMaskRectTransform == null ||
            lavaEdgeImage == null || lavaFrames == null)
        {
            return;
        }

        fullFillWidth = GetAvailableFillWidth();

        float clampedRemaining = Mathf.Clamp01(remainingProgress);
        float remainingWidth = fullFillWidth * clampedRemaining;
        float solidWidth = Mathf.Max(0f, remainingWidth - lavaVisibleSurfaceWidth);

        solidFillRectTransform.sizeDelta = new Vector2(solidWidth, 0f);
        lavaEdgeMaskRectTransform.anchoredPosition = new Vector2(
            remainingWidth - lavaVisibleSurfaceWidth,
            0f);

        int frameIndex = Mathf.FloorToInt(lavaAnimationTime * lavaFramesPerSecond) % lavaFrames.Length;
        lavaEdgeImage.sprite = lavaFrames[frameIndex];

        bool isVisible = clampedRemaining > 0.001f;
        solidFillImage.enabled = isVisible;
        lavaEdgeImage.enabled = isVisible;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private float GetAvailableFillWidth()
    {
        float gaugeWidth = gaugeRectTransform != null
            ? gaugeRectTransform.rect.width
            : referenceResolution.x;

        return Mathf.Max(0f, gaugeWidth - horizontalPadding * 2f);
    }

    private void OnValidate()
    {
        referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
        gaugeSize.x = 0f;
        gaugeSize.y = Mathf.Max(1f, gaugeSize.y);
        horizontalPadding = Mathf.Max(0f, horizontalPadding);
        verticalPadding = Mathf.Max(0f, verticalPadding);
        lavaEdgeWidth = Mathf.Max(1f, lavaEdgeWidth);
        lavaVisibleSurfaceWidth = Mathf.Clamp(
            lavaVisibleSurfaceWidth,
            1f,
            lavaEdgeWidth);
        lavaFramesPerSecond = Mathf.Max(1f, lavaFramesPerSecond);
    }
}
