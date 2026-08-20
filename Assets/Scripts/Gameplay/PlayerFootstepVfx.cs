using UnityEngine;

/// <summary>
/// Alternates two sprite animations at the player's feet based on actual distance moved.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerFootstepVfx : MonoBehaviour
{
    private const float MinimumDistance = 0.01f;
    private const float MinimumFrameDuration = 0.01f;
    private const float MovementEpsilon = 0.0001f;
    private const float IdleResetDelay = 0.1f;

    [Header("Alternating Effects")]
    [SerializeField] private Sprite[] firstEffectFrames;
    [SerializeField] private Sprite[] secondEffectFrames;

    [Header("Timing")]
    [Tooltip("Actual world distance the player travels between dust effects.")]
    [SerializeField, Min(MinimumDistance)] private float spawnDistance = 0.8f;

    [Tooltip("Seconds each animation frame remains visible.")]
    [SerializeField, Min(MinimumFrameDuration)] private float frameDuration = 0.06f;

    [Header("Placement")]
    [SerializeField] private Vector2 footOffset = new Vector2(0f, -0.48f);
    [SerializeField, Min(0.01f)] private float effectScale = 1f;

    [Header("Rendering")]
    [Tooltip("Added to the player's sprite sorting order so the dust remains visible.")]
    [SerializeField] private int sortingOrderOffset = 1;

    [Tooltip("Used instead when the player moves downward, placing the dust behind the player.")]
    [SerializeField] private int downwardSortingOrderOffset = -1;

    [SerializeField, Range(2, 8)] private int poolSize = 4;

    private SpriteRenderer playerSpriteRenderer;
    private PlayerController playerController;
    private Transform effectRoot;
    private PlaybackSlot[] playbackSlots;
    private Vector3 previousPosition;
    private float accumulatedDistance;
    private float idleTime;
    private int nextSlotIndex;
    private bool useFirstEffect = true;

    private sealed class PlaybackSlot
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Sprite[] Frames;
        public int FrameIndex;
        public float FrameTime;
        public bool IsPlaying;
    }

    private void Awake()
    {
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        CreatePool();
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        accumulatedDistance = spawnDistance;
        idleTime = IdleResetDelay;
    }

    private void LateUpdate()
    {
        if (playerController != null && playerController.IsReturning)
        {
            StopAllEffects();
            previousPosition = transform.position;
            accumulatedDistance = 0f;
            idleTime = 0f;
            return;
        }

        UpdateAnimations(Time.deltaTime);

        Vector3 currentPosition = transform.position;
        Vector2 movementDelta = currentPosition - previousPosition;
        float movedDistance = movementDelta.magnitude;
        previousPosition = currentPosition;

        if (movedDistance <= MovementEpsilon)
        {
            idleTime += Time.deltaTime;
            if (idleTime >= IdleResetDelay)
            {
                accumulatedDistance = spawnDistance;
            }

            return;
        }

        idleTime = 0f;
        accumulatedDistance += movedDistance;

        if (accumulatedDistance < spawnDistance)
        {
            return;
        }

        accumulatedDistance %= Mathf.Max(MinimumDistance, spawnDistance);
        SpawnAlternatingEffect(movementDelta);
    }

    private void OnDisable()
    {
        StopAllEffects();
    }

    private void OnDestroy()
    {
        if (effectRoot != null)
        {
            Destroy(effectRoot.gameObject);
        }
    }

    private void CreatePool()
    {
        GameObject rootObject = new GameObject($"{name} Footstep VFX");
        effectRoot = rootObject.transform;

        int safePoolSize = Mathf.Clamp(poolSize, 2, 8);
        playbackSlots = new PlaybackSlot[safePoolSize];

        for (int i = 0; i < safePoolSize; i++)
        {
            GameObject effectObject = new GameObject($"Footstep Effect {i + 1}");
            effectObject.transform.SetParent(effectRoot, false);

            SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
            effectRenderer.enabled = false;

            playbackSlots[i] = new PlaybackSlot
            {
                Transform = effectObject.transform,
                Renderer = effectRenderer
            };
        }
    }

    private void SpawnAlternatingEffect(Vector2 movementDirection)
    {
        Sprite[] frames = useFirstEffect ? firstEffectFrames : secondEffectFrames;
        useFirstEffect = !useFirstEffect;

        if (frames == null || frames.Length == 0 || playbackSlots == null)
        {
            return;
        }

        PlaybackSlot slot = playbackSlots[nextSlotIndex];
        nextSlotIndex = (nextSlotIndex + 1) % playbackSlots.Length;

        Vector3 footWorldPosition = transform.TransformPoint(
            new Vector3(footOffset.x, footOffset.y, 0f));
        Vector3 playerScale = transform.lossyScale;

        slot.Transform.position = footWorldPosition;
        slot.Transform.rotation = Quaternion.identity;
        slot.Transform.localScale = new Vector3(
            Mathf.Abs(playerScale.x) * effectScale,
            Mathf.Abs(playerScale.y) * effectScale,
            1f);

        slot.Renderer.sharedMaterial = playerSpriteRenderer.sharedMaterial;
        slot.Renderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
        bool isMovingDown = movementDirection.y < -MovementEpsilon;
        int activeSortingOffset = isMovingDown
            ? downwardSortingOrderOffset
            : sortingOrderOffset;
        slot.Renderer.sortingOrder = playerSpriteRenderer.sortingOrder + activeSortingOffset;
        slot.Renderer.maskInteraction = playerSpriteRenderer.maskInteraction;
        slot.Renderer.color = Color.white;
        slot.Renderer.flipX = movementDirection.x < -MovementEpsilon;
        slot.Renderer.flipY = false;

        slot.Frames = frames;
        slot.FrameIndex = 0;
        slot.FrameTime = 0f;
        slot.IsPlaying = true;
        slot.Renderer.sprite = frames[0];
        slot.Renderer.enabled = true;
    }

    private void UpdateAnimations(float deltaTime)
    {
        if (playbackSlots == null)
        {
            return;
        }

        float safeFrameDuration = Mathf.Max(MinimumFrameDuration, frameDuration);

        foreach (PlaybackSlot slot in playbackSlots)
        {
            if (!slot.IsPlaying)
            {
                continue;
            }

            slot.FrameTime += deltaTime;

            while (slot.FrameTime >= safeFrameDuration)
            {
                slot.FrameTime -= safeFrameDuration;
                slot.FrameIndex++;

                if (slot.FrameIndex >= slot.Frames.Length)
                {
                    slot.IsPlaying = false;
                    slot.Renderer.enabled = false;
                    break;
                }

                slot.Renderer.sprite = slot.Frames[slot.FrameIndex];
            }
        }
    }

    private void StopAllEffects()
    {
        if (playbackSlots == null)
        {
            return;
        }

        foreach (PlaybackSlot slot in playbackSlots)
        {
            slot.IsPlaying = false;
            slot.Renderer.enabled = false;
        }
    }

    private void OnValidate()
    {
        spawnDistance = Mathf.Max(MinimumDistance, spawnDistance);
        frameDuration = Mathf.Max(MinimumFrameDuration, frameDuration);
        effectScale = Mathf.Max(0.01f, effectScale);
        poolSize = Mathf.Clamp(poolSize, 2, 8);
    }
}
