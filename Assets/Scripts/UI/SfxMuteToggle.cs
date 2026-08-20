using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles every registered sound-effect source and stamps a muted icon on the button.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class SfxMuteToggle : MonoBehaviour
{
    private const int IconCellSize = 32;

    [Header("Muted Icon")]
    [SerializeField] private Texture2D iconTexture;
    [SerializeField, Min(1)] private int rowFromTop = 8;
    [SerializeField, Min(1)] private int columnFromLeft = 2;
    [SerializeField] private Vector2 iconSize = new Vector2(116f, 116f);
    [SerializeField] private Vector2 iconOffset = Vector2.zero;
    [SerializeField] private RectTransform iconParent;

    private static readonly HashSet<AudioSource> SoundEffectSources =
        new HashSet<AudioSource>();
    private static bool isMuted;

    private Button button;
    private GameObject mutedIconObject;
    private Sprite mutedIconSprite;

    public static bool IsMuted => isMuted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        SoundEffectSources.Clear();
        isMuted = false;
    }

    public static void RegisterSoundEffect(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        RemoveDestroyedSources();
        SoundEffectSources.Add(source);
        source.mute = isMuted;
    }

    private static void SetMuted(bool muted)
    {
        isMuted = muted;
        RemoveDestroyedSources();

        foreach (AudioSource source in SoundEffectSources)
        {
            source.mute = isMuted;
        }
    }

    private static void RemoveDestroyedSources()
    {
        SoundEffectSources.RemoveWhere(source => source == null);
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        CreateMutedIcon();
        button.onClick.AddListener(ToggleMute);
        ApplyVisualState();
    }

    private void ToggleMute()
    {
        SetMuted(!isMuted);
        ApplyVisualState();
    }

    private void CreateMutedIcon()
    {
        if (iconTexture == null)
        {
            return;
        }

        int x = (columnFromLeft - 1) * IconCellSize;
        int y = iconTexture.height - rowFromTop * IconCellSize;
        if (x < 0 || y < 0 ||
            x + IconCellSize > iconTexture.width ||
            y + IconCellSize > iconTexture.height)
        {
            Debug.LogWarning("SFX muted icon cell is outside the texture.", this);
            return;
        }

        mutedIconSprite = Sprite.Create(
            iconTexture,
            new Rect(x, y, IconCellSize, IconCellSize),
            new Vector2(0.5f, 0.5f),
            IconCellSize,
            0,
            SpriteMeshType.FullRect);
        mutedIconSprite.name = "SFX Muted X";

        mutedIconObject = new GameObject(
            "SFX Muted Icon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        Transform parent = iconParent != null ? iconParent : transform;
        mutedIconObject.transform.SetParent(parent, false);
        mutedIconObject.transform.SetAsLastSibling();

        RectTransform iconRect = mutedIconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = iconOffset;
        iconRect.sizeDelta = iconSize;

        Image iconImage = mutedIconObject.GetComponent<Image>();
        iconImage.sprite = mutedIconSprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private void ApplyVisualState()
    {
        if (mutedIconObject != null)
        {
            mutedIconObject.SetActive(isMuted);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ToggleMute);
        }

        if (mutedIconSprite != null)
        {
            Destroy(mutedIconSprite);
        }
    }

    private void OnValidate()
    {
        rowFromTop = Mathf.Max(1, rowFromTop);
        columnFromLeft = Mathf.Max(1, columnFromLeft);
        iconSize.x = Mathf.Max(1f, iconSize.x);
        iconSize.y = Mathf.Max(1f, iconSize.y);
    }
}
