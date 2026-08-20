using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Splits the current player sprite into fixed legs and a gently bouncing upper body while idle.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerIdleBounce : MonoBehaviour
{
    private const float MovementEpsilon = 0.0001f;

    [Header("Idle Detection")]
    [SerializeField, Min(0f)] private float idleDelay = 0.15f;

    [Header("Body Split")]
    [SerializeField, Range(0.2f, 0.7f)] private float legHeightRatio = 0.42f;
    [Tooltip("Overlapping pixels hide a gap when the upper body moves upward.")]
    [SerializeField, Range(0, 8)] private int seamOverlapPixels = 4;

    [Header("Upper-Body Bounce")]
    [SerializeField, Min(0f)] private float bounceDistance = 0.028f;
    [SerializeField, Min(0.1f)] private float bounceBeatsPerSecond = 2f;
    [SerializeField] private float downwardOffset = -0.006f;

    private SpriteRenderer sourceRenderer;
    private PlayerController playerController;
    private SpriteRenderer legsRenderer;
    private SpriteRenderer upperBodyRenderer;
    private Transform legsTransform;
    private Transform upperBodyTransform;
    private readonly Dictionary<Sprite, SplitSpritePair> splitSpriteCache =
        new Dictionary<Sprite, SplitSpritePair>();
    private SplitSpritePair activePair;
    private Vector3 previousPosition;
    private float idleTime;
    private float bounceTime;
    private bool isBounceActive;
    private bool isSuspended;
    private bool sourceRendererWasEnabled;

    private sealed class SplitSpritePair
    {
        public Sprite Legs;
        public Sprite UpperBody;
        public Vector2 LegsLocalPosition;
        public Vector2 UpperBodyLocalPosition;
    }

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        CreateBodyRenderers();
        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        idleTime = 0f;
        bounceTime = 0f;
    }

    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        float movedSqrDistance = (currentPosition - previousPosition).sqrMagnitude;
        previousPosition = currentPosition;

        if (isSuspended ||
            (playerController != null && playerController.IsReturning) ||
            movedSqrDistance > MovementEpsilon * MovementEpsilon)
        {
            idleTime = 0f;
            bounceTime = 0f;
            DisableBounce();
            return;
        }

        idleTime += Time.deltaTime;
        if (idleTime < idleDelay)
        {
            DisableBounce();
            return;
        }

        if (!EnableBounceForCurrentSprite())
        {
            return;
        }

        bounceTime += Time.deltaTime;
        float beat = Mathf.Sin(
            bounceTime * Mathf.PI * 2f * bounceBeatsPerSecond);
        float bounceOffset = downwardOffset +
            (beat * 0.5f + 0.5f) * bounceDistance;

        legsTransform.localPosition = activePair.LegsLocalPosition;
        upperBodyTransform.localPosition =
            activePair.UpperBodyLocalPosition + Vector2.up * bounceOffset;
        RefreshRendererAppearance();
    }

    public void SetSuspended(bool shouldSuspend)
    {
        isSuspended = shouldSuspend;

        if (isSuspended)
        {
            idleTime = 0f;
            bounceTime = 0f;
            DisableBounce();
        }
        else
        {
            previousPosition = transform.position;
        }
    }

    private bool EnableBounceForCurrentSprite()
    {
        Sprite sourceSprite = sourceRenderer.sprite;
        if (sourceSprite == null)
        {
            DisableBounce();
            return false;
        }

        if (!splitSpriteCache.TryGetValue(sourceSprite, out activePair))
        {
            activePair = CreateSplitSpritePair(sourceSprite);
            if (activePair == null)
            {
                DisableBounce();
                return false;
            }

            splitSpriteCache.Add(sourceSprite, activePair);
        }

        if (!isBounceActive)
        {
            if (!sourceRenderer.enabled)
            {
                return false;
            }

            sourceRendererWasEnabled = sourceRenderer.enabled;
            sourceRenderer.enabled = false;
            legsRenderer.enabled = true;
            upperBodyRenderer.enabled = true;
            isBounceActive = true;
        }

        legsRenderer.sprite = activePair.Legs;
        upperBodyRenderer.sprite = activePair.UpperBody;
        return true;
    }

    private SplitSpritePair CreateSplitSpritePair(Sprite sourceSprite)
    {
        Rect sourceRect = sourceSprite.rect;
        float pixelsPerUnit = sourceSprite.pixelsPerUnit;
        int sourceHeight = Mathf.RoundToInt(sourceRect.height);
        int splitHeight = Mathf.Clamp(
            Mathf.RoundToInt(sourceHeight * legHeightRatio),
            1,
            sourceHeight - 1);
        int overlap = Mathf.Clamp(
            seamOverlapPixels,
            0,
            Mathf.Min(splitHeight - 1, sourceHeight - splitHeight - 1));
        float legsHeight = splitHeight + overlap;
        float upperStart = splitHeight - overlap;
        float upperHeight = sourceHeight - upperStart;

        Sprite legs = Sprite.Create(
            sourceSprite.texture,
            new Rect(sourceRect.x, sourceRect.y, sourceRect.width, legsHeight),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        legs.name = $"{sourceSprite.name} Fixed Legs";

        Sprite upperBody = Sprite.Create(
            sourceSprite.texture,
            new Rect(
                sourceRect.x,
                sourceRect.y + upperStart,
                sourceRect.width,
                upperHeight),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        upperBody.name = $"{sourceSprite.name} Bouncing Upper Body";

        Vector2 originalPivot = sourceSprite.pivot;
        Vector2 legsCenter = new Vector2(sourceRect.width * 0.5f, legsHeight * 0.5f);
        Vector2 upperCenter = new Vector2(
            sourceRect.width * 0.5f,
            upperStart + upperHeight * 0.5f);

        return new SplitSpritePair
        {
            Legs = legs,
            UpperBody = upperBody,
            LegsLocalPosition = (legsCenter - originalPivot) / pixelsPerUnit,
            UpperBodyLocalPosition = (upperCenter - originalPivot) / pixelsPerUnit
        };
    }

    private void CreateBodyRenderers()
    {
        GameObject legsObject = new GameObject("Idle Bounce Fixed Legs");
        legsObject.transform.SetParent(transform, false);
        legsTransform = legsObject.transform;
        legsRenderer = legsObject.AddComponent<SpriteRenderer>();
        legsRenderer.enabled = false;

        GameObject upperBodyObject = new GameObject("Idle Bounce Upper Body");
        upperBodyObject.transform.SetParent(transform, false);
        upperBodyTransform = upperBodyObject.transform;
        upperBodyRenderer = upperBodyObject.AddComponent<SpriteRenderer>();
        upperBodyRenderer.enabled = false;
    }

    private void RefreshRendererAppearance()
    {
        CopyRendererAppearance(legsRenderer, 0);
        CopyRendererAppearance(upperBodyRenderer, 1);
    }

    private void CopyRendererAppearance(SpriteRenderer targetRenderer, int sortingOffset)
    {
        targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        targetRenderer.color = sourceRenderer.color;
        targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        targetRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOffset;
        targetRenderer.maskInteraction = sourceRenderer.maskInteraction;
        targetRenderer.flipX = sourceRenderer.flipX;
        targetRenderer.flipY = sourceRenderer.flipY;
    }

    private void DisableBounce()
    {
        if (!isBounceActive)
        {
            return;
        }

        legsRenderer.enabled = false;
        upperBodyRenderer.enabled = false;
        sourceRenderer.enabled = sourceRendererWasEnabled;
        isBounceActive = false;
    }

    private void OnDisable()
    {
        DisableBounce();
    }

    private void OnDestroy()
    {
        foreach (SplitSpritePair pair in splitSpriteCache.Values)
        {
            if (pair.Legs != null)
            {
                Destroy(pair.Legs);
            }

            if (pair.UpperBody != null)
            {
                Destroy(pair.UpperBody);
            }
        }

        splitSpriteCache.Clear();
    }

    private void OnValidate()
    {
        idleDelay = Mathf.Max(0f, idleDelay);
        legHeightRatio = Mathf.Clamp(legHeightRatio, 0.2f, 0.7f);
        seamOverlapPixels = Mathf.Clamp(seamOverlapPixels, 0, 8);
        bounceDistance = Mathf.Max(0f, bounceDistance);
        bounceBeatsPerSecond = Mathf.Max(0.1f, bounceBeatsPerSecond);
    }
}
