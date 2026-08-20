using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Swaps a UI button to its pressed sprite and optionally lowers its label.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ButtonPressVisual : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Sprite shown while the button is held down.")]
    [SerializeField] private Sprite pressedSprite;

    [Tooltip("Optional label that should move with the pressed button face.")]
    [SerializeField] private RectTransform contentToMove;

    [Tooltip("Distance the separate label moves downward while pressed.")]
    [SerializeField, Min(0f)] private float contentPressOffset = 12f;

    private Image targetImage;
    private Selectable selectable;
    private Vector2 contentRestPosition;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        ResetVisual();
    }

    private void OnDisable()
    {
        ResetVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Initialize();

        if (pressedSprite == null ||
            targetImage == null ||
            (selectable != null && !selectable.IsInteractable()))
        {
            return;
        }

        targetImage.overrideSprite = pressedSprite;

        if (contentToMove != null)
        {
            contentToMove.anchoredPosition =
                contentRestPosition + Vector2.down * contentPressOffset;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetVisual();
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        targetImage = GetComponent<Image>();
        selectable = GetComponent<Selectable>();

        if (contentToMove != null)
        {
            contentRestPosition = contentToMove.anchoredPosition;
        }

        isInitialized = true;
    }

    private void ResetVisual()
    {
        if (!isInitialized)
        {
            return;
        }

        if (targetImage != null)
        {
            targetImage.overrideSprite = null;
        }

        if (contentToMove != null)
        {
            contentToMove.anchoredPosition = contentRestPosition;
        }
    }
}
