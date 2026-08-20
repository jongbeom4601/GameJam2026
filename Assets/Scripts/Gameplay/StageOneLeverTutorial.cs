using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Highlights the first lever and its ladder while alternating released/pressed E key icons.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(InteractObject))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class StageOneLeverTutorial : MonoBehaviour
{
    private const float TwoPi = Mathf.PI * 2f;

    [Header("Tutorial Target")]
    [SerializeField] private GameObject ladderTarget;

    [Header("Keyboard Prompt")]
    [SerializeField] private Texture2D keyboardTexture;
    [SerializeField, Min(1)] private int keyCellSize = 32;
    [SerializeField, Min(1)] private int eKeyColumnFromLeft = 15;
    [SerializeField, Min(1)] private int releasedRowFromTop = 1;
    [SerializeField, Min(1)] private int pressedRowFromTop = 8;
    [SerializeField, Min(0.05f)] private float keyFrameInterval = 0.42f;
    [SerializeField] private Vector2 keyPromptOffset = new Vector2(0f, 1.15f);
    [SerializeField, Min(0.01f)] private float keyPromptScale = 0.9f;
    [SerializeField, Min(0f)] private float pressedKeyDrop = 0.08f;
    [SerializeField] private int keySortingOrderOffset = 1000;

    [Header("Yellow Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.95f;
    [SerializeField, Min(0.1f)] private float pulseCyclesPerSecond = 1.35f;
    [SerializeField, Min(1f)] private float leverHighlightScale = 1.38f;
    [SerializeField, Min(1f)] private float ladderHighlightScale = 1.2f;

    private readonly List<SpriteRenderer> ladderHighlights =
        new List<SpriteRenderer>();
    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();

    private InteractObject interactionState;
    private SpriteRenderer playerRenderer;
    private SpriteRenderer leverRenderer;
    private SpriteRenderer leverHighlight;
    private SpriteRenderer leverCover;
    private SpriteRenderer keyPromptRenderer;
    private Sprite releasedKeySprite;
    private Sprite pressedKeySprite;
    private float animationElapsed;
    private bool isCompleted;

    private void Start()
    {
        interactionState = GetComponent<InteractObject>();
        leverRenderer = GetComponent<SpriteRenderer>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        playerRenderer = playerObject != null
            ? playerObject.GetComponentInChildren<SpriteRenderer>()
            : null;

        if (interactionState == null || leverRenderer == null ||
            interactionState.exInteract)
        {
            isCompleted = true;
            return;
        }

        CreateLeverHighlight();
        CreateLadderHighlights();
        CreateKeyPrompt();
    }

    private void LateUpdate()
    {
        if (isCompleted)
        {
            return;
        }

        if (interactionState == null || interactionState.exInteract)
        {
            CompleteTutorial();
            return;
        }

        animationElapsed += Time.unscaledDeltaTime;
        SyncLeverCover();
        UpdateHighlightPulse();
        UpdateKeyPrompt();
    }

    private void CreateLeverHighlight()
    {
        leverHighlight = CreateHighlightRenderer(
            leverRenderer,
            "Tutorial Lever Yellow Outline",
            leverHighlightScale,
            1);
        leverCover = CreateHighlightRenderer(
            leverRenderer,
            "Tutorial Lever Original Cover",
            1f,
            2);
        SyncLeverCover();
    }

    private void CreateLadderHighlights()
    {
        if (ladderTarget == null)
        {
            return;
        }

        SpriteRenderer[] targetRenderers =
            ladderTarget.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null || targetRenderer.sprite == null ||
                !targetRenderer.enabled || !targetRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            SpriteRenderer highlight = CreateHighlightRenderer(
                targetRenderer,
                $"Tutorial Ladder Highlight - {targetRenderer.name}",
                ladderHighlightScale,
                5);
            if (highlight != null)
            {
                ladderHighlights.Add(highlight);
            }
        }
    }

    private SpriteRenderer CreateHighlightRenderer(
        SpriteRenderer source,
        string objectName,
        float scale,
        int sortingOrderOffset)
    {
        if (source == null || source.sprite == null)
        {
            return null;
        }

        GameObject highlightObject = new GameObject(objectName);
        generatedObjects.Add(highlightObject);
        highlightObject.transform.SetParent(source.transform, false);
        highlightObject.transform.localPosition = Vector3.zero;
        highlightObject.transform.localRotation = Quaternion.identity;
        highlightObject.transform.localScale = Vector3.one * scale;

        SpriteRenderer highlight = highlightObject.AddComponent<SpriteRenderer>();
        highlight.sprite = source.sprite;
        highlight.sharedMaterial = source.sharedMaterial;
        highlight.sortingLayerID = source.sortingLayerID;
        highlight.sortingOrder = source.sortingOrder + sortingOrderOffset;
        highlight.maskInteraction = source.maskInteraction;
        highlight.flipX = source.flipX;
        highlight.flipY = source.flipY;
        highlight.color = highlightColor;
        return highlight;
    }

    private void SyncLeverCover()
    {
        if (leverRenderer == null)
        {
            return;
        }

        bool playerOverlapsLever =
            playerRenderer != null &&
            playerRenderer.enabled &&
            playerRenderer.sortingLayerID == leverRenderer.sortingLayerID &&
            playerRenderer.bounds.Intersects(leverRenderer.bounds);
        int highlightSortingOrder = playerOverlapsLever
            ? playerRenderer.sortingOrder - 1
            : leverRenderer.sortingOrder + 1;
        int coverSortingOrder = playerOverlapsLever
            ? playerRenderer.sortingOrder - 1
            : leverRenderer.sortingOrder + 2;

        if (leverHighlight != null)
        {
            leverHighlight.sprite = leverRenderer.sprite;
            leverHighlight.sortingLayerID = leverRenderer.sortingLayerID;
            leverHighlight.sortingOrder = highlightSortingOrder;
            leverHighlight.flipX = leverRenderer.flipX;
            leverHighlight.flipY = leverRenderer.flipY;
        }

        if (leverCover == null)
        {
            return;
        }

        leverCover.sprite = leverRenderer.sprite;
        leverCover.sharedMaterial = leverRenderer.sharedMaterial;
        leverCover.sortingLayerID = leverRenderer.sortingLayerID;
        leverCover.sortingOrder = coverSortingOrder;
        leverCover.maskInteraction = leverRenderer.maskInteraction;
        leverCover.flipX = leverRenderer.flipX;
        leverCover.flipY = leverRenderer.flipY;
        leverCover.color = leverRenderer.color;
    }

    private void CreateKeyPrompt()
    {
        releasedKeySprite = CreateKeySprite(releasedRowFromTop, "Released E Key");
        pressedKeySprite = CreateKeySprite(pressedRowFromTop, "Pressed E Key");
        if (releasedKeySprite == null || pressedKeySprite == null)
        {
            return;
        }

        GameObject promptObject = new GameObject("Tutorial E Key Prompt");
        generatedObjects.Add(promptObject);
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = keyPromptOffset;
        promptObject.transform.localScale = Vector3.one * keyPromptScale;

        keyPromptRenderer = promptObject.AddComponent<SpriteRenderer>();
        keyPromptRenderer.sprite = releasedKeySprite;
        keyPromptRenderer.sharedMaterial = leverRenderer.sharedMaterial;
        keyPromptRenderer.sortingLayerID = leverRenderer.sortingLayerID;
        keyPromptRenderer.sortingOrder =
            leverRenderer.sortingOrder + keySortingOrderOffset;
        keyPromptRenderer.maskInteraction = leverRenderer.maskInteraction;
    }

    private Sprite CreateKeySprite(int rowFromTop, string spriteName)
    {
        if (keyboardTexture == null)
        {
            return null;
        }

        int x = (eKeyColumnFromLeft - 1) * keyCellSize;
        int y = keyboardTexture.height - rowFromTop * keyCellSize;
        if (x < 0 || y < 0 ||
            x + keyCellSize > keyboardTexture.width ||
            y + keyCellSize > keyboardTexture.height)
        {
            Debug.LogWarning($"{spriteName} is outside the keyboard texture.", this);
            return null;
        }

        Sprite keySprite = Sprite.Create(
            keyboardTexture,
            new Rect(x, y, keyCellSize, keyCellSize),
            new Vector2(0.5f, 0.5f),
            keyCellSize,
            0,
            SpriteMeshType.FullRect);
        keySprite.name = spriteName;
        return keySprite;
    }

    private void UpdateHighlightPulse()
    {
        float wave = (Mathf.Sin(
            animationElapsed * pulseCyclesPerSecond * TwoPi) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minimumAlpha, maximumAlpha, wave);

        SetRendererAlpha(leverHighlight, alpha);
        foreach (SpriteRenderer ladderHighlight in ladderHighlights)
        {
            SetRendererAlpha(ladderHighlight, alpha * 0.78f);
        }
    }

    private void UpdateKeyPrompt()
    {
        if (keyPromptRenderer == null)
        {
            return;
        }

        bool showPressed = Mathf.FloorToInt(
            animationElapsed / Mathf.Max(0.05f, keyFrameInterval)) % 2 == 1;
        keyPromptRenderer.sprite = showPressed
            ? pressedKeySprite
            : releasedKeySprite;
        keyPromptRenderer.transform.localPosition =
            (Vector3)keyPromptOffset +
            Vector3.down * (showPressed ? pressedKeyDrop : 0f);
        keyPromptRenderer.transform.localScale = Vector3.one *
            keyPromptScale * (showPressed ? 0.94f : 1f);
    }

    private void SetRendererAlpha(SpriteRenderer target, float alpha)
    {
        if (target == null)
        {
            return;
        }

        Color color = highlightColor;
        color.a *= Mathf.Clamp01(alpha);
        target.color = color;
    }

    private void CompleteTutorial()
    {
        isCompleted = true;
        foreach (GameObject generatedObject in generatedObjects)
        {
            if (generatedObject != null)
            {
                generatedObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (releasedKeySprite != null)
        {
            Destroy(releasedKeySprite);
        }

        if (pressedKeySprite != null)
        {
            Destroy(pressedKeySprite);
        }
    }

    private void OnValidate()
    {
        keyCellSize = Mathf.Max(1, keyCellSize);
        eKeyColumnFromLeft = Mathf.Max(1, eKeyColumnFromLeft);
        releasedRowFromTop = Mathf.Max(1, releasedRowFromTop);
        pressedRowFromTop = Mathf.Max(1, pressedRowFromTop);
        keyFrameInterval = Mathf.Max(0.05f, keyFrameInterval);
        keyPromptScale = Mathf.Max(0.01f, keyPromptScale);
        pressedKeyDrop = Mathf.Max(0f, pressedKeyDrop);
        pulseCyclesPerSecond = Mathf.Max(0.1f, pulseCyclesPerSecond);
        leverHighlightScale = Mathf.Max(1f, leverHighlightScale);
        ladderHighlightScale = Mathf.Max(1f, ladderHighlightScale);
        minimumAlpha = Mathf.Clamp01(minimumAlpha);
        maximumAlpha = Mathf.Max(minimumAlpha, Mathf.Clamp01(maximumAlpha));
    }
}
