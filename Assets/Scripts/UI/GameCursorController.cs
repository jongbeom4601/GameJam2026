using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shows an open hand normally and a pointing hand over the topmost interactable UI button.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameCursorController : MonoBehaviour
{
    [Header("Cursor Textures")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D interactiveCursor;

    [Header("Click Hotspots")]
    [Tooltip("Pixel used as the click point for the default cursor.")]
    [SerializeField] private Vector2 defaultHotspot = new Vector2(4f, 4f);

    [Tooltip("Pixel used as the click point for the interactive cursor.")]
    [SerializeField] private Vector2 interactiveHotspot = new Vector2(4f, 4f);

    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonPressedSound;
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField, Range(0f, 1f)] private float buttonPressedVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float buttonHoverVolume = 1f;

    private static GameCursorController instance;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private EventSystem cachedEventSystem;
    private PointerEventData pointerEventData;
    private bool? isShowingInteractiveCursor;
    private Object worldCursorOwner;
    private Texture2D worldCursorTexture;
    private Vector2 worldCursorHotspot;
    private bool isShowingWorldCursor;
    private AudioSource uiAudioSource;
    private Button hoveredButton;
    private bool hasHoveredButtonState;

    public static bool ShowWorldCursor(Object owner, Texture2D texture, Vector2 hotspot)
    {
        if (instance == null || owner == null || texture == null)
        {
            return false;
        }

        instance.worldCursorOwner = owner;
        instance.worldCursorTexture = texture;
        instance.worldCursorHotspot = hotspot;
        instance.ApplyWorldCursor(true);
        return true;
    }

    public static bool HideWorldCursor(Object owner)
    {
        if (instance == null || instance.worldCursorOwner != owner)
        {
            return false;
        }

        instance.worldCursorOwner = null;
        instance.worldCursorTexture = null;
        instance.ApplyCursor(false, true);
        return true;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        CreateAudioSource();
        DontDestroyOnLoad(gameObject);
        ApplyCursor(false, true);
    }

    private void OnEnable()
    {
        if (instance == this)
        {
            hoveredButton = null;
            hasHoveredButtonState = false;
            ApplyCursor(false, true);
        }
    }

    private void Update()
    {
        if (worldCursorOwner != null && worldCursorTexture != null)
        {
            SetHoveredButton(null, false);
            ApplyWorldCursor();
            return;
        }

        if (worldCursorTexture != null)
        {
            worldCursorTexture = null;
            ApplyCursor(false, true);
        }

        EventSystem currentEventSystem = EventSystem.current;
        Mouse mouse = Mouse.current;

        if (currentEventSystem == null || mouse == null)
        {
            SetHoveredButton(null, false);
            ApplyCursor(false);
            return;
        }

        if (pointerEventData == null || cachedEventSystem != currentEventSystem)
        {
            cachedEventSystem = currentEventSystem;
            pointerEventData = new PointerEventData(currentEventSystem);
        }

        pointerEventData.Reset();
        pointerEventData.position = mouse.position.ReadValue();

        raycastResults.Clear();
        currentEventSystem.RaycastAll(pointerEventData, raycastResults);

        Button currentButton = null;
        if (raycastResults.Count > 0)
        {
            Button button = raycastResults[0].gameObject.GetComponentInParent<Button>();
            if (button != null && button.isActiveAndEnabled && button.IsInteractable())
            {
                currentButton = button;
            }
        }

        SetHoveredButton(currentButton, true);

        if (currentButton != null && mouse.leftButton.wasPressedThisFrame)
        {
            PlayUiSound(buttonPressedSound, buttonPressedVolume);
        }

        ApplyCursor(currentButton != null);
    }

    private void CreateAudioSource()
    {
        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.spatialBlend = 0f;
        SfxMuteToggle.RegisterSoundEffect(uiAudioSource);
    }

    private void SetHoveredButton(Button currentButton, bool allowSound)
    {
        if (!hasHoveredButtonState)
        {
            hoveredButton = currentButton;
            hasHoveredButtonState = true;
            return;
        }

        bool enteredDifferentButton = currentButton != null &&
            currentButton != hoveredButton;
        hoveredButton = currentButton;

        if (allowSound && enteredDifferentButton)
        {
            PlayUiSound(buttonHoverSound, buttonHoverVolume);
        }
    }

    private void PlayUiSound(AudioClip clip, float volume)
    {
        if (uiAudioSource == null || clip == null)
        {
            return;
        }

        uiAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && instance == this)
        {
            if (worldCursorOwner != null && worldCursorTexture != null)
            {
                ApplyWorldCursor(true);
            }
            else
            {
                ApplyCursor(isShowingInteractiveCursor ?? false, true);
            }
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        instance = null;
    }

    private void OnValidate()
    {
        buttonPressedVolume = Mathf.Clamp01(buttonPressedVolume);
        buttonHoverVolume = Mathf.Clamp01(buttonHoverVolume);
    }

    private void ApplyCursor(bool useInteractiveCursor, bool force = false)
    {
        if (!force && !isShowingWorldCursor &&
            isShowingInteractiveCursor == useInteractiveCursor)
        {
            return;
        }

        bool canUseInteractiveCursor = useInteractiveCursor && interactiveCursor != null;
        Texture2D texture = canUseInteractiveCursor ? interactiveCursor : defaultCursor;
        Vector2 hotspot = canUseInteractiveCursor ? interactiveHotspot : defaultHotspot;

        Cursor.SetCursor(texture, hotspot, cursorMode);
        Cursor.visible = true;
        isShowingInteractiveCursor = useInteractiveCursor;
        isShowingWorldCursor = false;
    }

    private void ApplyWorldCursor(bool force = false)
    {
        if (worldCursorTexture == null || (!force && isShowingWorldCursor))
        {
            return;
        }

        Cursor.SetCursor(worldCursorTexture, worldCursorHotspot, cursorMode);
        Cursor.visible = true;
        isShowingInteractiveCursor = null;
        isShowingWorldCursor = true;
    }
}
