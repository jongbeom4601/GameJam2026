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

    private static GameCursorController instance;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private EventSystem cachedEventSystem;
    private PointerEventData pointerEventData;
    private bool? isShowingInteractiveCursor;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyCursor(false, true);
    }

    private void OnEnable()
    {
        if (instance == this)
        {
            ApplyCursor(false, true);
        }
    }

    private void Update()
    {
        EventSystem currentEventSystem = EventSystem.current;
        Mouse mouse = Mouse.current;

        if (currentEventSystem == null || mouse == null)
        {
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

        bool isOverInteractableButton = false;
        if (raycastResults.Count > 0)
        {
            Button button = raycastResults[0].gameObject.GetComponentInParent<Button>();
            isOverInteractableButton = button != null &&
                                       button.isActiveAndEnabled &&
                                       button.IsInteractable();
        }

        ApplyCursor(isOverInteractableButton);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && instance == this)
        {
            ApplyCursor(isShowingInteractiveCursor ?? false, true);
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

    private void ApplyCursor(bool useInteractiveCursor, bool force = false)
    {
        if (!force && isShowingInteractiveCursor == useInteractiveCursor)
        {
            return;
        }

        bool canUseInteractiveCursor = useInteractiveCursor && interactiveCursor != null;
        Texture2D texture = canUseInteractiveCursor ? interactiveCursor : defaultCursor;
        Vector2 hotspot = canUseInteractiveCursor ? interactiveHotspot : defaultHotspot;

        Cursor.SetCursor(texture, hotspot, cursorMode);
        Cursor.visible = true;
        isShowingInteractiveCursor = useInteractiveCursor;
    }
}
