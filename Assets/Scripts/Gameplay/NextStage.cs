using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays a short player celebration before loading the next stage.
/// </summary>
public sealed class NextStage : MonoBehaviour
{
    private const float MinimumDuration = 0.01f;
    private const float CrownPixelsPerUnit = 32f;

    [SerializeField] private string sceneName;

    [Header("Clear Celebration")]
    [SerializeField, Min(MinimumDuration)] private float celebrationDuration = 2f;
    [SerializeField, Min(MinimumDuration)] private float jumpDuration = 0.45f;
    [SerializeField, Min(0f)] private float jumpHeight = 0.22f;

    [Header("Crown Animation")]
    [SerializeField] private Texture2D crownTexture;
    [SerializeField, Min(1)] private int crownColumns = 8;
    [SerializeField, Min(1)] private int crownFrameCount = 37;
    [SerializeField, Min(MinimumDuration)] private float crownFrameDuration = 0.04f;
    [SerializeField] private Vector2 crownOffset = new Vector2(0f, 0.95f);
    [SerializeField, Min(0.01f)] private float crownScale = 1.25f;
    [SerializeField] private int crownSortingOrderOffset = 1000;

    [Header("Clear Sound")]
    [SerializeField] private AudioClip successSound;
    [SerializeField, Range(0f, 1f)] private float successSoundVolume = 1f;

    private bool isCelebrating;
    private AudioSource audioSource;
    private Sprite[] crownFrames;
    private SpriteRenderer originalPlayerRenderer;
    private Transform celebrationMotionTransform;
    private SpriteRenderer celebrationPlayerRenderer;
    private SpriteRenderer crownRenderer;

    private void Awake()
    {
        CreateAudioSource();
        CreateCrownFrames();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isCelebrating || !collision.CompareTag("Player"))
        {
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            collision.transform.position);
        if (distance <= 0.5f)
        {
            BeginCelebration(collision.transform);
        }
    }

    private void BeginCelebration(Transform player)
    {
        if (isCelebrating || player == null || !CanLoadNextScene())
        {
            return;
        }

        isCelebrating = true;
        LockGameplay(player);
        CreateCelebrationVisuals(player);
        StartCoroutine(PlayCelebration());
    }

    private void LockGameplay(Transform player)
    {
        PlayerEmojiInteraction emojiInteraction =
            player.GetComponent<PlayerEmojiInteraction>();
        if (emojiInteraction != null)
        {
            emojiInteraction.enabled = false;
        }

        PlayerIdleBounce idleBounce = player.GetComponent<PlayerIdleBounce>();
        if (idleBounce != null)
        {
            idleBounce.SetSuspended(true);
        }

        PlayerDirectionalAnimator directionalAnimator =
            player.GetComponent<PlayerDirectionalAnimator>();
        if (directionalAnimator != null)
        {
            directionalAnimator.StopAndShowStanding();
            directionalAnimator.enabled = false;
        }

        PlayerFootstepVfx footstepVfx = player.GetComponent<PlayerFootstepVfx>();
        if (footstepVfx != null)
        {
            footstepVfx.enabled = false;
        }

        PlayerRewindVfx rewindVfx = player.GetComponent<PlayerRewindVfx>();
        if (rewindVfx != null)
        {
            rewindVfx.HidePreparation();
        }

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        PlayerInput playerInput = player.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.DeactivateInput();
        }

        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.simulated = false;
        }

        ReturnManager returnManager = FindAnyObjectByType<ReturnManager>();
        if (returnManager != null)
        {
            returnManager.enabled = false;
        }
    }

    private void CreateCelebrationVisuals(Transform player)
    {
        originalPlayerRenderer = player.GetComponent<SpriteRenderer>();
        if (originalPlayerRenderer == null)
        {
            celebrationMotionTransform = player;
            return;
        }

        GameObject celebrationVisualRoot =
            new GameObject("Stage Clear Celebration Visual");
        celebrationMotionTransform = celebrationVisualRoot.transform;
        celebrationMotionTransform.SetParent(player, false);

        GameObject playerVisualObject = new GameObject("Jumping Player Visual");
        playerVisualObject.transform.SetParent(celebrationMotionTransform, false);
        celebrationPlayerRenderer = playerVisualObject.AddComponent<SpriteRenderer>();
        CopyRendererAppearance(originalPlayerRenderer, celebrationPlayerRenderer);

        GameObject crownObject = new GameObject("Stage Clear Crown");
        crownObject.transform.SetParent(celebrationMotionTransform, false);
        crownObject.transform.localPosition = crownOffset;
        crownObject.transform.localScale = Vector3.one * crownScale;

        crownRenderer = crownObject.AddComponent<SpriteRenderer>();
        crownRenderer.sharedMaterial = originalPlayerRenderer.sharedMaterial;
        crownRenderer.sortingLayerID = originalPlayerRenderer.sortingLayerID;
        crownRenderer.sortingOrder = originalPlayerRenderer.sortingOrder +
            crownSortingOrderOffset;
        crownRenderer.maskInteraction = originalPlayerRenderer.maskInteraction;
        crownRenderer.enabled = crownFrames != null && crownFrames.Length > 0;
        if (crownRenderer.enabled)
        {
            crownRenderer.sprite = crownFrames[0];
        }

        originalPlayerRenderer.enabled = false;
    }

    private static void CopyRendererAppearance(
        SpriteRenderer source,
        SpriteRenderer destination)
    {
        destination.sprite = source.sprite;
        destination.sharedMaterial = source.sharedMaterial;
        destination.color = source.color;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
        destination.maskInteraction = source.maskInteraction;
        destination.flipX = source.flipX;
        destination.flipY = source.flipY;
    }

    private IEnumerator PlayCelebration()
    {
        float duration = successSound != null
            ? Mathf.Max(celebrationDuration, successSound.length)
            : celebrationDuration;
        duration = Mathf.Max(MinimumDuration, duration);
        float safeJumpDuration = Mathf.Min(
            Mathf.Max(MinimumDuration, jumpDuration),
            duration);

        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound, successSoundVolume);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            UpdateJump(elapsed, safeJumpDuration);
            UpdateCrown(elapsed);

            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        UpdateJump(safeJumpDuration, safeJumpDuration);
        if (crownRenderer != null)
        {
            crownRenderer.enabled = false;
        }

        ChangeScene();
    }

    private void UpdateJump(float elapsed, float safeJumpDuration)
    {
        if (celebrationMotionTransform == null)
        {
            return;
        }

        float jumpOffset = 0f;
        if (elapsed < safeJumpDuration)
        {
            float jumpProgress = Mathf.Clamp01(elapsed / safeJumpDuration);
            jumpOffset = 4f * jumpHeight * jumpProgress * (1f - jumpProgress);
        }

        celebrationMotionTransform.localPosition = Vector3.up * jumpOffset;

        if (celebrationPlayerRenderer != null)
        {
            float stretch = elapsed < safeJumpDuration
                ? Mathf.Sin(Mathf.Clamp01(elapsed / safeJumpDuration) * Mathf.PI)
                : 0f;
            celebrationPlayerRenderer.transform.localScale = new Vector3(
                1f - stretch * 0.025f,
                1f + stretch * 0.04f,
                1f);
        }
    }

    private void UpdateCrown(float elapsed)
    {
        if (crownRenderer == null || crownFrames == null ||
            crownFrames.Length == 0)
        {
            return;
        }

        int frameIndex = Mathf.FloorToInt(
            elapsed / Mathf.Max(MinimumDuration, crownFrameDuration));

        if (frameIndex >= crownFrames.Length)
        {
            crownRenderer.enabled = false;
            return;
        }

        crownRenderer.enabled = true;
        crownRenderer.sprite = crownFrames[frameIndex];
    }

    private void CreateAudioSource()
    {
        if (successSound == null)
        {
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        SfxMuteToggle.RegisterSoundEffect(audioSource);
    }

    private void CreateCrownFrames()
    {
        if (crownTexture == null || crownColumns < 1 || crownFrameCount < 1)
        {
            return;
        }

        int frameWidth = crownTexture.width / crownColumns;
        int frameHeight = frameWidth;
        int availableRows = crownTexture.height / frameHeight;
        int availableFrames = crownColumns * availableRows;
        int safeFrameCount = Mathf.Min(crownFrameCount, availableFrames);

        if (frameWidth < 1 || frameHeight < 1 || safeFrameCount < 1)
        {
            return;
        }

        crownFrames = new Sprite[safeFrameCount];
        for (int i = 0; i < safeFrameCount; i++)
        {
            int column = i % crownColumns;
            int rowFromTop = i / crownColumns;
            int y = crownTexture.height - (rowFromTop + 1) * frameHeight;

            crownFrames[i] = Sprite.Create(
                crownTexture,
                new Rect(
                    column * frameWidth,
                    y,
                    frameWidth,
                    frameHeight),
                new Vector2(0.5f, 0.5f),
                CrownPixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            crownFrames[i].name = $"Stage Clear Crown {i + 1}";
        }
    }

    private bool CanLoadNextScene()
    {
        if (!string.IsNullOrWhiteSpace(sceneName) &&
            Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return true;
        }

        Debug.LogError(
            $"Cannot load next scene '{sceneName}'. Add a valid scene name to the endpoint and include it in the active Build Profile.",
            this);
        return false;
    }

    public void ChangeScene()
    {
        if (CanLoadNextScene())
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void OnDestroy()
    {
        if (crownFrames == null)
        {
            return;
        }

        foreach (Sprite frame in crownFrames)
        {
            if (frame != null)
            {
                Destroy(frame);
            }
        }
    }

    private void OnValidate()
    {
        celebrationDuration = Mathf.Max(MinimumDuration, celebrationDuration);
        jumpDuration = Mathf.Max(MinimumDuration, jumpDuration);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        crownColumns = Mathf.Max(1, crownColumns);
        crownFrameCount = Mathf.Max(1, crownFrameCount);
        crownFrameDuration = Mathf.Max(MinimumDuration, crownFrameDuration);
        crownScale = Mathf.Max(0.01f, crownScale);
        successSoundVolume = Mathf.Clamp01(successSoundVolume);
    }
}
