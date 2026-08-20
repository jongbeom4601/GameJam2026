using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Plays a short sprite animation above an interactable UI button when it is pressed.
/// </summary>
[DisallowMultipleComponent]
public sealed class ButtonClickVfxController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Sprite[] effectFrames;
    [SerializeField, Min(0.01f)] private float frameDuration = 0.05f;
    [SerializeField] private Vector2 effectSize = new Vector2(160f, 160f);
    [SerializeField] private Vector2 pointerOffset = Vector2.zero;

    private static ButtonClickVfxController instance;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private EventSystem cachedEventSystem;
    private PointerEventData pointerEventData;
    private Image effectImage;
    private RectTransform effectRectTransform;
    private int currentFrameIndex;
    private float elapsedFrameTime;
    private bool isPlaying;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
        CreateEffectVisual();
    }

    private void Update()
    {
        TryPlayFromMousePress();
        UpdateAnimation(Time.unscaledDeltaTime);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void TryPlayFromMousePress()
    {
        Mouse mouse = Mouse.current;
        EventSystem currentEventSystem = EventSystem.current;

        if (mouse == null || currentEventSystem == null ||
            !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (pointerEventData == null || cachedEventSystem != currentEventSystem)
        {
            cachedEventSystem = currentEventSystem;
            pointerEventData = new PointerEventData(currentEventSystem);
        }

        Vector2 pointerPosition = mouse.position.ReadValue();
        pointerEventData.Reset();
        pointerEventData.position = pointerPosition;

        raycastResults.Clear();
        currentEventSystem.RaycastAll(pointerEventData, raycastResults);

        if (raycastResults.Count == 0)
        {
            return;
        }

        Button button = raycastResults[0].gameObject.GetComponentInParent<Button>();
        if (button == null || !button.isActiveAndEnabled || !button.IsInteractable())
        {
            return;
        }

        PlayEffect(button, pointerPosition);
    }

    private void CreateEffectVisual()
    {
        GameObject imageObject = new GameObject(
            "ButtonClickVfx",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        imageObject.transform.SetParent(transform, false);

        effectRectTransform = imageObject.GetComponent<RectTransform>();
        effectRectTransform.anchorMin = Vector2.zero;
        effectRectTransform.anchorMax = Vector2.zero;
        effectRectTransform.pivot = new Vector2(0.5f, 0.5f);
        effectRectTransform.sizeDelta = effectSize;

        effectImage = imageObject.GetComponent<Image>();
        effectImage.raycastTarget = false;
        effectImage.preserveAspect = true;
        imageObject.GetComponent<LayoutElement>().ignoreLayout = true;

        imageObject.SetActive(false);
    }

    private void PlayEffect(Button button, Vector2 pointerScreenPosition)
    {
        if (effectImage == null)
        {
            CreateEffectVisual();
        }

        RectTransform buttonRectTransform = button.transform as RectTransform;
        if (effectImage == null || buttonRectTransform == null ||
            effectFrames == null || effectFrames.Length == 0)
        {
            return;
        }

        RectTransform buttonParent = buttonRectTransform.parent as RectTransform;
        if (buttonParent == null)
        {
            return;
        }

        Canvas buttonCanvas = button.GetComponentInParent<Canvas>();
        Camera canvasCamera = null;
        if (buttonCanvas != null && buttonCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = buttonCanvas.worldCamera != null
                ? buttonCanvas.worldCamera
                : Camera.main;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                buttonParent,
                pointerScreenPosition,
                canvasCamera,
                out Vector2 localPointerPosition))
        {
            return;
        }

        // Place the effect immediately after the button so it renders above the button.
        // The hardware cursor remains above all Unity UI rendering.
        effectRectTransform.SetParent(transform, false);
        int buttonSiblingIndex = buttonRectTransform.GetSiblingIndex();
        effectRectTransform.SetParent(buttonParent, false);
        effectRectTransform.SetSiblingIndex(buttonSiblingIndex + 1);

        effectRectTransform.anchorMin = buttonParent.pivot;
        effectRectTransform.anchorMax = buttonParent.pivot;
        effectRectTransform.pivot = new Vector2(0.5f, 0.5f);
        effectRectTransform.localRotation = Quaternion.identity;
        effectRectTransform.localScale = Vector3.one;
        effectRectTransform.sizeDelta = effectSize;
        effectRectTransform.anchoredPosition = localPointerPosition + pointerOffset;

        currentFrameIndex = 0;
        elapsedFrameTime = 0f;
        isPlaying = true;
        effectImage.sprite = effectFrames[currentFrameIndex];
        effectImage.gameObject.SetActive(true);
    }

    private void UpdateAnimation(float unscaledDeltaTime)
    {
        if (!isPlaying)
        {
            return;
        }

        if (effectImage == null)
        {
            isPlaying = false;
            return;
        }

        elapsedFrameTime += unscaledDeltaTime;
        float safeFrameDuration = Mathf.Max(0.01f, frameDuration);

        while (elapsedFrameTime >= safeFrameDuration)
        {
            elapsedFrameTime -= safeFrameDuration;
            currentFrameIndex++;

            if (currentFrameIndex >= effectFrames.Length)
            {
                isPlaying = false;
                effectImage.gameObject.SetActive(false);
                effectRectTransform.SetParent(transform, false);
                return;
            }

            effectImage.sprite = effectFrames[currentFrameIndex];
        }
    }
}
