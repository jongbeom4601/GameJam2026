using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Highlights the stage-two box and its target hole while alternating E key icons.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxMovement))]
public sealed class StageTwoBoxTutorial : MonoBehaviour
{
    private const float TwoPi = Mathf.PI * 2f;

    [Header("Tutorial Target")]
    [SerializeField] private GameObject blackHoleTarget;
    [SerializeField, Min(0.01f)] private float completionDistance = 0.15f;

    [Header("Keyboard Prompt")]
    [SerializeField] private Texture2D keyboardTexture;
    [SerializeField, Min(1)] private int keyCellSize = 32;
    [SerializeField, Min(1)] private int eKeyColumnFromLeft = 15;
    [SerializeField, Min(1)] private int releasedRowFromTop = 1;
    [SerializeField, Min(1)] private int pressedRowFromTop = 8;
    [SerializeField, Min(0.05f)] private float keyFrameInterval = 0.42f;
    [SerializeField] private Vector2 keyPromptOffset = new Vector2(1.05f, 0.2f);
    [SerializeField, Min(0.01f)] private float keyPromptScale = 0.9f;
    [SerializeField, Min(0f)] private float pressedKeyDrop = 0.08f;
    [SerializeField] private int keySortingOrderOffset = 1000;

    [Header("Yellow Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.95f;
    [SerializeField, Min(0.1f)] private float pulseCyclesPerSecond = 1.35f;
    [SerializeField, Min(1f)] private float boxHighlightScale = 1.38f;
    [SerializeField, Min(1f)] private float holeHighlightScale = 1.32f;

    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();

    private SpriteRenderer boxRenderer;
    private SpriteRenderer holeRenderer;
    private SpriteRenderer boxHighlight;
    private SpriteRenderer boxCover;
    private SpriteRenderer holeHighlight;
    private SpriteRenderer holeCover;
    private SpriteRenderer keyPromptRenderer;
    private Sprite releasedKeySprite;
    private Sprite pressedKeySprite;
    private float animationElapsed;
    private bool isCompleted;

    private void Start()
    {
        boxRenderer = FindPrimaryBoxRenderer();
        holeRenderer = blackHoleTarget != null
            ? blackHoleTarget.GetComponent<SpriteRenderer>()
            : null;

        if (boxRenderer == null || holeRenderer == null)
        {
            Debug.LogWarning("Stage 2 box tutorial targets are not assigned.", this);
            isCompleted = true;
            return;
        }

        if (HasReachedTargetHole())
        {
            isCompleted = true;
            return;
        }

        CreateBoxHighlight();
        CreateHoleHighlight();
        CreateKeyPrompt();
    }

    private void LateUpdate()
    {
        if (isCompleted)
        {
            return;
        }

        if (HasReachedTargetHole())
        {
            CompleteTutorial();
            return;
        }

        animationElapsed += Time.unscaledDeltaTime;
        SyncOutlinedObject(boxRenderer, boxHighlight, boxCover, 1, 2);
        SyncOutlinedObject(holeRenderer, holeHighlight, holeCover, 10, 11);
        UpdateHighlightPulse();
        UpdateKeyPrompt();
    }

    private SpriteRenderer FindPrimaryBoxRenderer()
    {
        SpriteRenderer selectedRenderer = null;
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer candidate in renderers)
        {
            if (candidate == null || candidate.sprite == null ||
                candidate.name.Contains("Interaction Outline"))
            {
                continue;
            }

            if (selectedRenderer == null ||
                candidate.sortingOrder > selectedRenderer.sortingOrder)
            {
                selectedRenderer = candidate;
            }
        }

        return selectedRenderer;
    }

    private bool HasReachedTargetHole()
    {
        return blackHoleTarget != null &&
            Vector2.Distance(transform.position, blackHoleTarget.transform.position) <=
            Mathf.Max(0.01f, completionDistance);
    }

    private void CreateBoxHighlight()
    {
        boxHighlight = CreateVisualLayer(
            boxRenderer,
            "Tutorial Box Yellow Outline",
            boxHighlightScale,
            1);
        boxCover = CreateVisualLayer(
            boxRenderer,
            "Tutorial Box Original Cover",
            1f,
            2);
        SyncOutlinedObject(boxRenderer, boxHighlight, boxCover, 1, 2);
    }

    private void CreateHoleHighlight()
    {
        holeHighlight = CreateVisualLayer(
            holeRenderer,
            "Tutorial Target Hole Yellow Outline",
            holeHighlightScale,
            10);
        holeCover = CreateVisualLayer(
            holeRenderer,
            "Tutorial Target Hole Original Cover",
            1f,
            11);
        SyncOutlinedObject(holeRenderer, holeHighlight, holeCover, 10, 11);
    }

    private SpriteRenderer CreateVisualLayer(
        SpriteRenderer source,
        string objectName,
        float scale,
        int sortingOrderOffset)
    {
        if (source == null || source.sprite == null)
        {
            return null;
        }

        GameObject visualObject = new GameObject(objectName);
        generatedObjects.Add(visualObject);
        visualObject.transform.SetParent(source.transform, false);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one * scale;

        SpriteRenderer visualRenderer = visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = source.sprite;
        visualRenderer.sharedMaterial = source.sharedMaterial;
        visualRenderer.sortingLayerID = source.sortingLayerID;
        visualRenderer.sortingOrder = source.sortingOrder + sortingOrderOffset;
        visualRenderer.maskInteraction = source.maskInteraction;
        visualRenderer.flipX = source.flipX;
        visualRenderer.flipY = source.flipY;
        visualRenderer.color = highlightColor;
        return visualRenderer;
    }

    private static void SyncOutlinedObject(
        SpriteRenderer source,
        SpriteRenderer outline,
        SpriteRenderer cover,
        int outlineSortingOffset,
        int coverSortingOffset)
    {
        if (source == null)
        {
            return;
        }

        if (outline != null)
        {
            outline.sprite = source.sprite;
            outline.sharedMaterial = source.sharedMaterial;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder + outlineSortingOffset;
            outline.maskInteraction = source.maskInteraction;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;
        }

        if (cover == null)
        {
            return;
        }

        cover.sprite = source.sprite;
        cover.sharedMaterial = source.sharedMaterial;
        cover.sortingLayerID = source.sortingLayerID;
        cover.sortingOrder = source.sortingOrder + coverSortingOffset;
        cover.maskInteraction = source.maskInteraction;
        cover.flipX = source.flipX;
        cover.flipY = source.flipY;
        cover.color = source.color;
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
        keyPromptRenderer.sharedMaterial = boxRenderer.sharedMaterial;
        keyPromptRenderer.sortingLayerID = boxRenderer.sortingLayerID;
        keyPromptRenderer.sortingOrder =
            boxRenderer.sortingOrder + keySortingOrderOffset;
        keyPromptRenderer.maskInteraction = boxRenderer.maskInteraction;
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
        SetRendererAlpha(boxHighlight, alpha);
        SetRendererAlpha(holeHighlight, alpha);
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
        keyPromptRenderer.sortingLayerID = boxRenderer.sortingLayerID;
        keyPromptRenderer.sortingOrder =
            boxRenderer.sortingOrder + keySortingOrderOffset;
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
        completionDistance = Mathf.Max(0.01f, completionDistance);
        keyCellSize = Mathf.Max(1, keyCellSize);
        eKeyColumnFromLeft = Mathf.Max(1, eKeyColumnFromLeft);
        releasedRowFromTop = Mathf.Max(1, releasedRowFromTop);
        pressedRowFromTop = Mathf.Max(1, pressedRowFromTop);
        keyFrameInterval = Mathf.Max(0.05f, keyFrameInterval);
        keyPromptScale = Mathf.Max(0.01f, keyPromptScale);
        pressedKeyDrop = Mathf.Max(0f, pressedKeyDrop);
        pulseCyclesPerSecond = Mathf.Max(0.1f, pulseCyclesPerSecond);
        boxHighlightScale = Mathf.Max(1f, boxHighlightScale);
        holeHighlightScale = Mathf.Max(1f, holeHighlightScale);
        minimumAlpha = Mathf.Clamp01(minimumAlpha);
        maximumAlpha = Mathf.Max(minimumAlpha, Mathf.Clamp01(maximumAlpha));
    }
}
