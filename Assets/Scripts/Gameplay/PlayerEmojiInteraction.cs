using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Gives the player a cursor-hover and click reaction without moving its physics transform.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerEmojiInteraction : MonoBehaviour
{
    private const int EmojiColumns = 5;
    private const float MinimumDuration = 0.01f;

    [Header("Cursor")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Vector2 defaultCursorHotspot = new Vector2(4f, 4f);
    [SerializeField] private Vector2 hoverCursorHotspot = new Vector2(4f, 4f);
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Header("Sound")]
    [SerializeField] private AudioClip expressionSound;
    [SerializeField, Range(0f, 1f)] private float expressionSoundVolume = 1f;

    [Header("Click Area")]
    [Tooltip("World-space interaction area. A size of 1 x 1 matches one tile.")]
    [SerializeField] private Vector2 interactionAreaSize = Vector2.one;
    [SerializeField] private Vector2 interactionAreaOffset = Vector2.zero;

    [Header("Reaction Textures")]
    [SerializeField] private Texture2D speechBubbleTexture;
    [SerializeField] private Texture2D emojiTexture;
    [SerializeField, Min(1)] private int bubbleCellSize = 64;
    [SerializeField, Min(1)] private int emojiCellSize = 32;
    [Tooltip("One-based row counted from the top of the emoji sheet.")]
    [SerializeField, Min(1)] private int firstEmojiRowFromTop = 6;
    [SerializeField, Min(1)] private int emojiRowCount = 2;

    [Header("Placement")]
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0f, 1.1f);
    [SerializeField, Min(0.01f)] private float bubbleScale = 2.4f;
    [SerializeField] private Vector2 emojiOffset = new Vector2(0f, 0.06f);
    [SerializeField, Min(0.01f)] private float emojiScale = 1.6f;

    [Header("Animation")]
    [SerializeField, Min(MinimumDuration)] private float reactionDuration = 0.9f;
    [SerializeField, Min(MinimumDuration)] private float bubbleExpandDuration = 0.16f;
    [SerializeField, Min(MinimumDuration)] private float bubbleShrinkDuration = 0.2f;
    [SerializeField, Min(MinimumDuration)] private float twitchDuration = 0.28f;
    [SerializeField, Min(0f)] private float twitchDistance = 0.065f;
    [SerializeField, Min(0.5f)] private float twitchCycles = 2.5f;

    [Header("Rendering")]
    [SerializeField] private int bubbleSortingOrderOffset = 1000;
    [SerializeField] private int emojiSortingOrderOffset = 1001;

    private Collider2D hitCollider;
    private SpriteRenderer playerRenderer;
    private PlayerController playerController;
    private PlayerIdleBounce idleBounce;
    private Camera worldCamera;
    private Sprite bubbleSprite;
    private readonly List<Sprite> emojiSprites = new List<Sprite>();
    private Transform bubbleRoot;
    private Transform emojiTransform;
    private Transform twitchTransform;
    private SpriteRenderer bubbleRenderer;
    private SpriteRenderer emojiRenderer;
    private SpriteRenderer twitchRenderer;
    private Coroutine reactionCoroutine;
    private bool isHovered;
    private bool cursorWasManaged;
    private bool playerRendererWasEnabled;
    private bool isReactionPlaying;
    private AudioSource expressionAudioSource;

    private void Awake()
    {
        hitCollider = GetComponent<Collider2D>();
        playerRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        idleBounce = GetComponent<PlayerIdleBounce>();
        CreateAudioSource();

        CreateReactionSprites();
        CreateReactionVisuals();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || hitCollider == null ||
            (playerController != null && playerController.IsReturning))
        {
            SetHovered(false);
            return;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;

            if (worldCamera == null)
            {
                worldCamera = FindAnyObjectByType<Camera>();
            }
        }

        if (worldCamera == null)
        {
            SetHovered(false);
            return;
        }

        bool isPointerOverUi = EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();
        Vector2 screenPosition = mouse.position.ReadValue();
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y,
                -worldCamera.transform.position.z));
        bool pointerIsOverPlayer = !isPointerOverUi &&
            IsInsideInteractionArea(worldPosition);

        SetHovered(pointerIsOverPlayer);

        if (pointerIsOverPlayer && mouse.leftButton.wasPressedThisFrame)
        {
            PlayReaction();
        }
    }

    private bool IsInsideInteractionArea(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        Vector2 centeredPosition = new Vector2(localPosition.x, localPosition.y) -
            interactionAreaOffset;
        Vector2 halfSize = interactionAreaSize * 0.5f;

        return Mathf.Abs(centeredPosition.x) <= halfSize.x &&
            Mathf.Abs(centeredPosition.y) <= halfSize.y;
    }

    private void SetHovered(bool shouldBeHovered)
    {
        if (isHovered == shouldBeHovered)
        {
            return;
        }

        isHovered = shouldBeHovered;

        if (isHovered)
        {
            cursorWasManaged = GameCursorController.ShowWorldCursor(
                this,
                hoverCursor,
                hoverCursorHotspot);

            if (!cursorWasManaged)
            {
                Cursor.SetCursor(hoverCursor, hoverCursorHotspot, cursorMode);
                Cursor.visible = true;
            }
        }
        else
        {
            if (cursorWasManaged)
            {
                GameCursorController.HideWorldCursor(this);
            }
            else
            {
                Cursor.SetCursor(defaultCursor, defaultCursorHotspot, cursorMode);
                Cursor.visible = true;
            }

            cursorWasManaged = false;
        }
    }

    private void PlayReaction()
    {
        PlayExpressionSound();

        if (bubbleSprite == null || emojiSprites.Count == 0 ||
            bubbleRoot == null || emojiRenderer == null)
        {
            return;
        }

        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            FinishReaction();
        }

        emojiRenderer.sprite = emojiSprites[Random.Range(0, emojiSprites.Count)];
        PrepareReactionRenderers();
        reactionCoroutine = StartCoroutine(AnimateReaction());
    }

    private void CreateAudioSource()
    {
        expressionAudioSource = gameObject.AddComponent<AudioSource>();
        expressionAudioSource.playOnAwake = false;
        expressionAudioSource.loop = false;
        expressionAudioSource.spatialBlend = 0f;
        SfxMuteToggle.RegisterSoundEffect(expressionAudioSource);
    }

    private void PlayExpressionSound()
    {
        if (expressionAudioSource == null || expressionSound == null)
        {
            return;
        }

        expressionAudioSource.PlayOneShot(
            expressionSound,
            expressionSoundVolume);
    }

    private IEnumerator AnimateReaction()
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(MinimumDuration, reactionDuration);
        float safeExpandDuration = Mathf.Min(
            Mathf.Max(MinimumDuration, bubbleExpandDuration),
            safeDuration);
        float safeShrinkDuration = Mathf.Min(
            Mathf.Max(MinimumDuration, bubbleShrinkDuration),
            safeDuration);

        while (elapsed < safeDuration)
        {
            if (playerController != null && playerController.IsReturning)
            {
                break;
            }

            elapsed += Time.deltaTime;
            float bubbleFactor;

            if (elapsed < safeExpandDuration)
            {
                bubbleFactor = EaseOutBack(elapsed / safeExpandDuration);
            }
            else if (elapsed > safeDuration - safeShrinkDuration)
            {
                float shrinkProgress =
                    (elapsed - (safeDuration - safeShrinkDuration)) /
                    safeShrinkDuration;
                bubbleFactor = 1f - SmoothStep01(shrinkProgress);
            }
            else
            {
                bubbleFactor = 1f;
            }

            bubbleRoot.localScale = Vector3.one *
                (bubbleScale * Mathf.Max(0f, bubbleFactor));
            UpdateEmojiDrop(elapsed, safeExpandDuration);
            UpdateTwitch(elapsed);
            RefreshTwitchRenderer();

            yield return null;
        }

        FinishReaction();
        reactionCoroutine = null;
    }

    private void UpdateEmojiDrop(float elapsed, float expandDuration)
    {
        float dropDuration = Mathf.Max(MinimumDuration, expandDuration * 1.9f);
        float progress = Mathf.Clamp01(elapsed / dropDuration);
        float baseY = emojiOffset.y;
        float y;

        if (progress < 0.45f)
        {
            y = Mathf.Lerp(baseY, baseY + 0.09f,
                SmoothStep01(progress / 0.45f));
        }
        else if (progress < 0.78f)
        {
            y = Mathf.Lerp(baseY + 0.09f, baseY - 0.025f,
                SmoothStep01((progress - 0.45f) / 0.33f));
        }
        else
        {
            y = Mathf.Lerp(baseY - 0.025f, baseY,
                SmoothStep01((progress - 0.78f) / 0.22f));
        }

        emojiTransform.localPosition = new Vector3(emojiOffset.x, y, 0f);
        float emojiPop = Mathf.Clamp01(elapsed / 0.08f);
        emojiTransform.localScale = Vector3.one * (emojiScale * emojiPop);
    }

    private void UpdateTwitch(float elapsed)
    {
        float safeTwitchDuration = Mathf.Max(MinimumDuration, twitchDuration);
        float progress = Mathf.Clamp01(elapsed / safeTwitchDuration);
        float damping = 1f - progress;
        float x = Mathf.Sin(progress * Mathf.PI * 2f * twitchCycles) *
            twitchDistance * damping;
        twitchTransform.localPosition = new Vector3(x, 0f, 0f);
    }

    private void PrepareReactionRenderers()
    {
        if (idleBounce != null)
        {
            idleBounce.SetSuspended(true);
        }

        playerRendererWasEnabled = playerRenderer.enabled;
        isReactionPlaying = true;
        playerRenderer.enabled = false;

        RefreshTwitchRenderer();
        twitchRenderer.enabled = true;
        twitchTransform.localPosition = Vector3.zero;

        bubbleRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        bubbleRenderer.sortingOrder =
            playerRenderer.sortingOrder + bubbleSortingOrderOffset;
        emojiRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        emojiRenderer.sortingOrder =
            playerRenderer.sortingOrder + emojiSortingOrderOffset;

        bubbleRoot.localPosition = bubbleOffset;
        bubbleRoot.localScale = Vector3.zero;
        emojiTransform.localPosition = emojiOffset;
        emojiTransform.localScale = Vector3.zero;
        bubbleRoot.gameObject.SetActive(true);
    }

    private void RefreshTwitchRenderer()
    {
        twitchRenderer.sprite = playerRenderer.sprite;
        twitchRenderer.sharedMaterial = playerRenderer.sharedMaterial;
        twitchRenderer.color = playerRenderer.color;
        twitchRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        twitchRenderer.sortingOrder = playerRenderer.sortingOrder;
        twitchRenderer.maskInteraction = playerRenderer.maskInteraction;
        twitchRenderer.flipX = playerRenderer.flipX;
        twitchRenderer.flipY = playerRenderer.flipY;
    }

    private void FinishReaction()
    {
        bool wasReactionPlaying = isReactionPlaying;

        if (bubbleRoot != null)
        {
            bubbleRoot.gameObject.SetActive(false);
        }

        if (twitchRenderer != null)
        {
            twitchRenderer.enabled = false;
        }

        if (twitchTransform != null)
        {
            twitchTransform.localPosition = Vector3.zero;
        }

        if (isReactionPlaying && playerRenderer != null)
        {
            playerRenderer.enabled = playerRendererWasEnabled;
        }

        isReactionPlaying = false;

        if (wasReactionPlaying && idleBounce != null)
        {
            idleBounce.SetSuspended(false);
        }
    }

    private void CreateReactionSprites()
    {
        if (speechBubbleTexture != null &&
            speechBubbleTexture.width >= bubbleCellSize &&
            speechBubbleTexture.height >= bubbleCellSize)
        {
            bubbleSprite = Sprite.Create(
                speechBubbleTexture,
                new Rect(
                    0f,
                    speechBubbleTexture.height - bubbleCellSize,
                    bubbleCellSize,
                    bubbleCellSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            bubbleSprite.name = "Player Speech Bubble";
        }

        if (emojiTexture == null || emojiCellSize < 1)
        {
            return;
        }

        Color32[] emojiPixels = emojiTexture.GetPixels32();

        for (int rowOffset = 0; rowOffset < emojiRowCount; rowOffset++)
        {
            int rowFromTop = firstEmojiRowFromTop - 1 + rowOffset;
            int y = emojiTexture.height - (rowFromTop + 1) * emojiCellSize;

            if (y < 0 || y + emojiCellSize > emojiTexture.height)
            {
                continue;
            }

            for (int column = 0; column < EmojiColumns; column++)
            {
                int x = column * emojiCellSize;
                if (x + emojiCellSize > emojiTexture.width)
                {
                    break;
                }

                if (!CellHasVisiblePixels(
                    emojiPixels,
                    emojiTexture.width,
                    x,
                    y,
                    emojiCellSize))
                {
                    continue;
                }

                Sprite emojiSprite = Sprite.Create(
                    emojiTexture,
                    new Rect(x, y, emojiCellSize, emojiCellSize),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                emojiSprite.name =
                    $"Player Emoji Row {rowFromTop + 1} Column {column + 1}";
                emojiSprites.Add(emojiSprite);
            }
        }
    }

    private static bool CellHasVisiblePixels(
        Color32[] pixels,
        int textureWidth,
        int startX,
        int startY,
        int cellSize)
    {
        const byte VisibleAlphaThreshold = 8;
        const int MinimumVisiblePixelCount = 4;
        int visiblePixelCount = 0;

        for (int y = startY; y < startY + cellSize; y++)
        {
            int rowStart = y * textureWidth;

            for (int x = startX; x < startX + cellSize; x++)
            {
                if (pixels[rowStart + x].a <= VisibleAlphaThreshold)
                {
                    continue;
                }

                visiblePixelCount++;
                if (visiblePixelCount >= MinimumVisiblePixelCount)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CreateReactionVisuals()
    {
        GameObject twitchObject = new GameObject("Player Click Twitch Visual");
        twitchObject.transform.SetParent(transform, false);
        twitchTransform = twitchObject.transform;
        twitchRenderer = twitchObject.AddComponent<SpriteRenderer>();
        twitchRenderer.enabled = false;

        GameObject bubbleObject = new GameObject("Player Emoji Bubble");
        bubbleObject.transform.SetParent(transform, false);
        bubbleRoot = bubbleObject.transform;
        bubbleRenderer = bubbleObject.AddComponent<SpriteRenderer>();
        bubbleRenderer.sprite = bubbleSprite;

        GameObject emojiObject = new GameObject("Random Emoji");
        emojiObject.transform.SetParent(bubbleRoot, false);
        emojiTransform = emojiObject.transform;
        emojiRenderer = emojiObject.AddComponent<SpriteRenderer>();

        bubbleObject.SetActive(false);
    }

    private void OnDisable()
    {
        SetHovered(false);

        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            reactionCoroutine = null;
        }

        FinishReaction();
    }

    private void OnDestroy()
    {
        if (bubbleSprite != null)
        {
            Destroy(bubbleSprite);
        }

        foreach (Sprite emojiSprite in emojiSprites)
        {
            if (emojiSprite != null)
            {
                Destroy(emojiSprite);
            }
        }
    }

    private static float EaseOutBack(float value)
    {
        float t = Mathf.Clamp01(value) - 1f;
        const float Overshoot = 1.70158f;
        return 1f + (Overshoot + 1f) * t * t * t + Overshoot * t * t;
    }

    private static float SmoothStep01(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }

    private void OnValidate()
    {
        bubbleCellSize = Mathf.Max(1, bubbleCellSize);
        emojiCellSize = Mathf.Max(1, emojiCellSize);
        firstEmojiRowFromTop = Mathf.Max(1, firstEmojiRowFromTop);
        emojiRowCount = Mathf.Max(1, emojiRowCount);
        interactionAreaSize.x = Mathf.Max(0.01f, interactionAreaSize.x);
        interactionAreaSize.y = Mathf.Max(0.01f, interactionAreaSize.y);
        bubbleScale = Mathf.Max(0.01f, bubbleScale);
        emojiScale = Mathf.Max(0.01f, emojiScale);
        reactionDuration = Mathf.Max(MinimumDuration, reactionDuration);
        bubbleExpandDuration = Mathf.Max(MinimumDuration, bubbleExpandDuration);
        bubbleShrinkDuration = Mathf.Max(MinimumDuration, bubbleShrinkDuration);
        twitchDuration = Mathf.Max(MinimumDuration, twitchDuration);
        twitchDistance = Mathf.Max(0f, twitchDistance);
        twitchCycles = Mathf.Max(0.5f, twitchCycles);
        expressionSoundVolume = Mathf.Clamp01(expressionSoundVolume);
    }
}
