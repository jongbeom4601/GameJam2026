using UnityEngine;

[RequireComponent(typeof(InteractObject))]
public class Laber : MonoBehaviour
{
    private bool isLaber;
    private InteractObject InterScript;
    [SerializeField] private GameObject TargetObject;
    [Header("Interaction Sound")]
    [SerializeField] private AudioClip interactionSound;
    [SerializeField, Range(0f, 1f)] private float interactionSoundVolume = 1f;
    private SpriteRenderer sprite;
    private SpriteRenderer targetSprite;
    private SpriteRenderer interactionOutline;
    private Collider2D TargetCollider;
    private AudioSource interactionAudioSource;
    private bool previousInteractionState;


    private void Awake()
    {
        InterScript = GetComponent<InteractObject>();
        sprite = GetComponent<SpriteRenderer>();
        previousInteractionState = InterScript.exInteract;
        CreateAudioSource();
        CreateInteractionOutline();
        if (TargetObject != null)
        {
            targetSprite = TargetObject.GetComponent<SpriteRenderer>();
            TargetCollider = TargetObject.GetComponent<Collider2D>();
            TargetCollider.enabled = true;


        }
    }

    private void CreateInteractionOutline()
    {
        if (sprite == null)
            return;

        GameObject outlineObject = new GameObject("Interaction Outline");
        outlineObject.transform.SetParent(transform, false);
        outlineObject.transform.localScale = Vector3.one * 1.1f;

        interactionOutline = outlineObject.AddComponent<SpriteRenderer>();
        interactionOutline.sprite = sprite.sprite;
        interactionOutline.color = Color.cyan;
        interactionOutline.sortingLayerID = sprite.sortingLayerID;
        interactionOutline.sortingOrder = sprite.sortingOrder;
        interactionOutline.enabled = false;

        // [수정] 원본보다 뒤에 테두리가 보이도록 런타임 정렬 순서 조정
        sprite.sortingOrder += 1;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && interactionOutline != null)
        {
            interactionOutline.enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && interactionOutline != null)
        {
            interactionOutline.enabled = false;
        }
    }

    private void Update()
    {
        PlaySoundWhenInteractionChanges();
        LaberSetting();
    }

    private void CreateAudioSource()
    {
        if (interactionSound == null)
        {
            return;
        }

        interactionAudioSource = gameObject.AddComponent<AudioSource>();
        interactionAudioSource.playOnAwake = false;
        interactionAudioSource.loop = false;
        interactionAudioSource.spatialBlend = 0f;
        SfxMuteToggle.RegisterSoundEffect(interactionAudioSource);
    }

    private void PlaySoundWhenInteractionChanges()
    {
        bool currentInteractionState = InterScript.exInteract;
        if (currentInteractionState == previousInteractionState)
        {
            return;
        }

        previousInteractionState = currentInteractionState;
        if (interactionAudioSource != null && interactionSound != null)
        {
            interactionAudioSource.PlayOneShot(
                interactionSound,
                interactionSoundVolume);
        }
    }

    private void LaberSetting()
    {
        if (InterScript.exInteract && TargetObject != null)
        {
            // 이미지 변경으로 대체
            sprite.color = Color.red;
            targetSprite.color = new Color(1f, 1f, 1f, 1f);
            TargetCollider.enabled = false;
        }
        else if (!InterScript.exInteract && TargetObject != null)
        {
            // 이미지 변경으로 대체
            sprite.color = Color.white;
            targetSprite.color = new Color(1f, 1f, 1f, 0.390625f);
            TargetCollider.enabled = true;
        }
    }

    private void OnValidate()
    {
        interactionSoundVolume = Mathf.Clamp01(interactionSoundVolume);
    }
}
