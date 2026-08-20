using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a charge animation before rewind and leaves chromatic neon echoes while rewinding.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerRewindVfx : MonoBehaviour
{
    private const float MinimumDuration = 0.01f;
    private const float MovementEpsilon = 0.0001f;

    [Header("Pre-Rewind Animation")]
    [SerializeField] private Texture2D preparationTexture;
    [SerializeField, Min(1)] private int preparationFrameCount = 29;
    [SerializeField, Min(MinimumDuration)] private float preparationFrameDuration = 0.04f;
    [SerializeField] private Vector2 preparationOffset = new Vector2(0f, 0.1f);
    [SerializeField, Min(0.01f)] private float preparationScale = 1.1f;
    [SerializeField] private int preparationSortingOrderOffset = 10;

    [Header("Rewinding Player Appearance")]
    [SerializeField] private Shader rewindShader;
    [SerializeField] private Color playerNeonGreen = new Color32(132, 255, 0, 255);
    [SerializeField] private Color playerNeonCyan = new Color32(0, 255, 255, 255);
    [SerializeField, Range(0.1f, 1f)] private float rewindingPlayerAlpha = 0.55f;
    [SerializeField, Min(0.1f)] private float playerColorPulseSpeed = 12f;

    [Header("Full-Screen Rewind Filter")]
    [SerializeField] private Shader screenFilterShader;
    [SerializeField, Range(0f, 1f)] private float screenFilterIntensity = 0.35f;
    [SerializeField, Min(0.1f)] private float screenFilterFadeSpeed = 9f;
    [SerializeField] private int screenFilterSortingOrder = 4000;

    [Header("Neon Rewind Afterimages")]
    [SerializeField, Min(MinimumDuration)] private float afterimageInterval = 0.03f;
    [SerializeField, Min(MinimumDuration)] private float afterimageLifetime = 0.24f;
    [SerializeField, Min(0.01f)] private float afterimageScale = 1.08f;
    [SerializeField, Min(0f)] private float chromaticSeparation = 0.04f;
    [SerializeField] private Color neonGreen = new Color32(132, 255, 0, 128);
    [SerializeField] private Color neonCyan = new Color32(0, 255, 255, 112);
    [SerializeField] private Color neonMagenta = new Color32(255, 0, 225, 97);
    [SerializeField] private int afterimageSortingOrderOffset = -1;
    [SerializeField, Range(8, 32)] private int poolSize = 18;

    private PlayerController playerController;
    private SpriteRenderer playerRenderer;
    private SpriteRenderer preparationRenderer;
    private Sprite[] preparationFrames;
    private Transform afterimageRoot;
    private AfterimageSlot[] afterimageSlots;
    private Vector3 previousPosition;
    private float spawnTimer;
    private int nextSlotIndex;
    private int colorSequence;
    private Color colorBeforeRewind;
    private Material materialBeforeRewind;
    private Material rewindMaterial;
    private bool isPlayerAppearanceModified;
    private GameObject screenFilterCanvasObject;
    private Material screenFilterMaterial;
    private float screenFilterStrength;

    private sealed class AfterimageSlot
    {
        public GameObject Root;
        public Transform Transform;
        public SpriteRenderer PrimaryRenderer;
        public SpriteRenderer SecondaryRenderer;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Vector3 BaseScale;
        public float Age;
        public bool IsActive;
    }

    public float PreparationDurationSeconds =>
        Mathf.Max(MinimumDuration, preparationFrameDuration) *
        Mathf.Max(1, preparationFrameCount);

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerRenderer = GetComponent<SpriteRenderer>();

        if (rewindShader != null)
        {
            rewindMaterial = new Material(rewindShader)
            {
                name = $"{name} Rewind Neon Material"
            };
        }

        CreatePreparationFrames();
        CreatePreparationRenderer();
        CreateAfterimagePool();
        CreateScreenFilter();
        previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        UpdateAfterimages(deltaTime);

        Vector3 currentPosition = transform.position;
        bool isReturning = playerController != null && playerController.IsReturning;
        UpdatePlayerAppearance(isReturning);
        UpdateScreenFilter(isReturning, Time.unscaledDeltaTime);

        if (isReturning &&
            Vector3.SqrMagnitude(currentPosition - previousPosition) > MovementEpsilon)
        {
            spawnTimer -= deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnAfterimage();
                spawnTimer = Mathf.Max(MinimumDuration, afterimageInterval);
            }
        }
        else if (!isReturning)
        {
            spawnTimer = 0f;
        }

        previousPosition = currentPosition;
    }

    private void UpdatePlayerAppearance(bool isReturning)
    {
        if (!isReturning)
        {
            RestorePlayerAppearance();
            return;
        }

        if (!isPlayerAppearanceModified)
        {
            colorBeforeRewind = playerRenderer.color;
            materialBeforeRewind = playerRenderer.sharedMaterial;
            if (rewindMaterial != null)
            {
                playerRenderer.sharedMaterial = rewindMaterial;
            }
            isPlayerAppearanceModified = true;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * playerColorPulseSpeed) + 1f) * 0.5f;
        Color neonColor = Color.Lerp(playerNeonGreen, playerNeonCyan, pulse);
        neonColor.a = rewindingPlayerAlpha;
        playerRenderer.color = neonColor;
    }

    private void RestorePlayerAppearance()
    {
        if (!isPlayerAppearanceModified)
        {
            return;
        }

        playerRenderer.color = colorBeforeRewind;
        playerRenderer.sharedMaterial = materialBeforeRewind;
        isPlayerAppearanceModified = false;
    }

    private void CreateScreenFilter()
    {
        if (screenFilterShader == null)
        {
            return;
        }

        screenFilterMaterial = new Material(screenFilterShader)
        {
            name = $"{name} Rewind Screen Filter Material"
        };

        screenFilterCanvasObject = new GameObject(
            $"{name} Rewind Screen Filter",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        Canvas canvas = screenFilterCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = screenFilterSortingOrder;

        GameObject overlayObject = new GameObject(
            "Animated Rewind Filter",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        overlayObject.transform.SetParent(screenFilterCanvasObject.transform, false);

        RectTransform overlayRectTransform = overlayObject.GetComponent<RectTransform>();
        overlayRectTransform.anchorMin = Vector2.zero;
        overlayRectTransform.anchorMax = Vector2.one;
        overlayRectTransform.offsetMin = Vector2.zero;
        overlayRectTransform.offsetMax = Vector2.zero;

        RawImage overlayImage = overlayObject.GetComponent<RawImage>();
        overlayImage.material = screenFilterMaterial;
        overlayImage.color = Color.white;
        overlayImage.raycastTarget = false;

        screenFilterMaterial.SetFloat("_Intensity", 0f);
        screenFilterCanvasObject.SetActive(false);
    }

    private void UpdateScreenFilter(bool isReturning, float deltaTime)
    {
        if (screenFilterCanvasObject == null || screenFilterMaterial == null)
        {
            return;
        }

        if (isReturning && !screenFilterCanvasObject.activeSelf)
        {
            screenFilterCanvasObject.SetActive(true);
        }

        float targetStrength = isReturning ? screenFilterIntensity : 0f;
        screenFilterStrength = Mathf.MoveTowards(
            screenFilterStrength,
            targetStrength,
            screenFilterFadeSpeed * deltaTime);
        screenFilterMaterial.SetFloat("_Intensity", screenFilterStrength);

        if (!isReturning && screenFilterStrength <= 0.001f)
        {
            screenFilterCanvasObject.SetActive(false);
        }
    }

    public void SetPreparationProgress(float normalizedProgress)
    {
        if (preparationRenderer == null ||
            preparationFrames == null || preparationFrames.Length == 0)
        {
            return;
        }

        if (!preparationRenderer.enabled)
        {
            preparationRenderer.sharedMaterial = playerRenderer.sharedMaterial;
            preparationRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            preparationRenderer.sortingOrder =
                playerRenderer.sortingOrder + preparationSortingOrderOffset;
            preparationRenderer.maskInteraction = playerRenderer.maskInteraction;
            preparationRenderer.enabled = true;
        }

        int frameIndex = Mathf.Min(
            preparationFrames.Length - 1,
            Mathf.FloorToInt(Mathf.Clamp01(normalizedProgress) *
                preparationFrames.Length));
        preparationRenderer.sprite = preparationFrames[frameIndex];
    }

    public void HidePreparation()
    {
        if (preparationRenderer != null)
        {
            preparationRenderer.enabled = false;
        }
    }

    private void CreatePreparationFrames()
    {
        if (preparationTexture == null || preparationFrameCount < 1)
        {
            return;
        }

        int frameWidth = preparationTexture.width / preparationFrameCount;
        int frameHeight = preparationTexture.height;

        if (frameWidth < 1)
        {
            return;
        }

        preparationFrames = new Sprite[preparationFrameCount];

        for (int i = 0; i < preparationFrames.Length; i++)
        {
            preparationFrames[i] = Sprite.Create(
                preparationTexture,
                new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            preparationFrames[i].name = $"Rewind Preparation {i + 1}";
        }
    }

    private void CreatePreparationRenderer()
    {
        GameObject effectObject = new GameObject("Rewind Preparation Effect");
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.localPosition = preparationOffset;
        effectObject.transform.localScale = Vector3.one * preparationScale;

        preparationRenderer = effectObject.AddComponent<SpriteRenderer>();
        preparationRenderer.enabled = false;
    }

    private void CreateAfterimagePool()
    {
        GameObject rootObject = new GameObject($"{name} Rewind Afterimages");
        afterimageRoot = rootObject.transform;

        int safePoolSize = Mathf.Clamp(poolSize, 8, 32);
        afterimageSlots = new AfterimageSlot[safePoolSize];

        for (int i = 0; i < afterimageSlots.Length; i++)
        {
            GameObject slotObject = new GameObject($"Rewind Afterimage {i + 1}");
            slotObject.transform.SetParent(afterimageRoot, false);

            SpriteRenderer primary = CreateEchoRenderer("Cyan-Green Echo", slotObject.transform);
            SpriteRenderer secondary = CreateEchoRenderer("Magenta Echo", slotObject.transform);
            slotObject.SetActive(false);

            afterimageSlots[i] = new AfterimageSlot
            {
                Root = slotObject,
                Transform = slotObject.transform,
                PrimaryRenderer = primary,
                SecondaryRenderer = secondary
            };
        }
    }

    private static SpriteRenderer CreateEchoRenderer(string objectName, Transform parent)
    {
        GameObject rendererObject = new GameObject(objectName);
        rendererObject.transform.SetParent(parent, false);
        return rendererObject.AddComponent<SpriteRenderer>();
    }

    private void SpawnAfterimage()
    {
        if (afterimageSlots == null || afterimageSlots.Length == 0 ||
            playerRenderer.sprite == null)
        {
            return;
        }

        AfterimageSlot slot = afterimageSlots[nextSlotIndex];
        nextSlotIndex = (nextSlotIndex + 1) % afterimageSlots.Length;

        float colorBlend = Mathf.PingPong(colorSequence * 0.37f, 1f);
        colorSequence++;

        slot.PrimaryColor = Color.Lerp(neonGreen, neonCyan, colorBlend);
        slot.SecondaryColor = Color.Lerp(neonMagenta, neonCyan, 1f - colorBlend);
        slot.Age = 0f;
        slot.IsActive = true;
        slot.BaseScale = Vector3.Scale(
            transform.lossyScale,
            Vector3.one * afterimageScale);

        slot.Transform.SetPositionAndRotation(transform.position, transform.rotation);
        slot.Transform.localScale = slot.BaseScale;
        slot.PrimaryRenderer.transform.localPosition =
            new Vector3(-chromaticSeparation, 0f, 0f);
        slot.SecondaryRenderer.transform.localPosition =
            new Vector3(chromaticSeparation, 0f, 0f);

        ConfigureEchoRenderer(slot.PrimaryRenderer, slot.PrimaryColor, 0);
        ConfigureEchoRenderer(slot.SecondaryRenderer, slot.SecondaryColor, -1);
        slot.Root.SetActive(true);
    }

    private void ConfigureEchoRenderer(
        SpriteRenderer echoRenderer,
        Color color,
        int additionalSortingOffset)
    {
        echoRenderer.sprite = playerRenderer.sprite;
        echoRenderer.sharedMaterial = playerRenderer.sharedMaterial;
        echoRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        echoRenderer.sortingOrder = playerRenderer.sortingOrder +
            afterimageSortingOrderOffset + additionalSortingOffset;
        echoRenderer.maskInteraction = playerRenderer.maskInteraction;
        echoRenderer.flipX = playerRenderer.flipX;
        echoRenderer.flipY = playerRenderer.flipY;
        echoRenderer.color = color;
    }

    private void UpdateAfterimages(float deltaTime)
    {
        if (afterimageSlots == null)
        {
            return;
        }

        float safeLifetime = Mathf.Max(MinimumDuration, afterimageLifetime);

        foreach (AfterimageSlot slot in afterimageSlots)
        {
            if (!slot.IsActive)
            {
                continue;
            }

            slot.Age += deltaTime;
            float remaining = 1f - Mathf.Clamp01(slot.Age / safeLifetime);

            if (remaining <= 0f)
            {
                slot.IsActive = false;
                slot.Root.SetActive(false);
                continue;
            }

            slot.PrimaryRenderer.color = FadeColor(slot.PrimaryColor, remaining);
            slot.SecondaryRenderer.color = FadeColor(slot.SecondaryColor, remaining);
            slot.Transform.localScale = slot.BaseScale * (1f + (1f - remaining) * 0.08f);
        }
    }

    private static Color FadeColor(Color color, float remaining)
    {
        color.a *= remaining * remaining;
        return color;
    }

    private void OnDisable()
    {
        RestorePlayerAppearance();

        screenFilterStrength = 0f;
        if (screenFilterMaterial != null)
        {
            screenFilterMaterial.SetFloat("_Intensity", 0f);
        }
        if (screenFilterCanvasObject != null)
        {
            screenFilterCanvasObject.SetActive(false);
        }

        if (preparationRenderer != null)
        {
            preparationRenderer.enabled = false;
        }

        if (afterimageSlots == null)
        {
            return;
        }

        foreach (AfterimageSlot slot in afterimageSlots)
        {
            slot.IsActive = false;
            slot.Root.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (rewindMaterial != null)
        {
            Destroy(rewindMaterial);
        }

        if (screenFilterMaterial != null)
        {
            Destroy(screenFilterMaterial);
        }

        if (screenFilterCanvasObject != null)
        {
            Destroy(screenFilterCanvasObject);
        }

        if (afterimageRoot != null)
        {
            Destroy(afterimageRoot.gameObject);
        }

        if (preparationFrames == null)
        {
            return;
        }

        foreach (Sprite frame in preparationFrames)
        {
            if (frame != null)
            {
                Destroy(frame);
            }
        }
    }

    private void OnValidate()
    {
        preparationFrameCount = Mathf.Max(1, preparationFrameCount);
        preparationFrameDuration = Mathf.Max(MinimumDuration, preparationFrameDuration);
        preparationScale = Mathf.Max(0.01f, preparationScale);
        rewindingPlayerAlpha = Mathf.Clamp01(rewindingPlayerAlpha);
        playerColorPulseSpeed = Mathf.Max(0.1f, playerColorPulseSpeed);
        screenFilterIntensity = Mathf.Clamp01(screenFilterIntensity);
        screenFilterFadeSpeed = Mathf.Max(0.1f, screenFilterFadeSpeed);
        afterimageInterval = Mathf.Max(MinimumDuration, afterimageInterval);
        afterimageLifetime = Mathf.Max(MinimumDuration, afterimageLifetime);
        afterimageScale = Mathf.Max(0.01f, afterimageScale);
        chromaticSeparation = Mathf.Max(0f, chromaticSeparation);
        poolSize = Mathf.Clamp(poolSize, 8, 32);
    }
}
