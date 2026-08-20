using UnityEngine;

[RequireComponent(typeof(InteractObject))]
public class Laber : MonoBehaviour
{
    private bool isLaber;
    private InteractObject InterScript;
    [SerializeField] private GameObject TargetObject;
    [Header("Laber 이미지")]
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;
    private SpriteRenderer sprite;
    private SpriteRenderer[] targetSprites;
    private SpriteRenderer interactionOutline;
    private Collider2D TargetCollider;


    private void Awake()
    {
        InterScript = GetComponent<InteractObject>();
        sprite = GetComponent<SpriteRenderer>();
        CreateInteractionOutline();
        if (TargetObject != null)
        {
            // [수정] Laber Target 루트와 모든 하위 Square의 SpriteRenderer를 함께 제어
            targetSprites = TargetObject.GetComponentsInChildren<SpriteRenderer>(true);
            TargetCollider = TargetObject.GetComponent<Collider2D>();
            if (TargetCollider != null)
            {
                TargetCollider.enabled = true;
            }


        }
    }

    private void CreateInteractionOutline()
    {
        if (sprite == null)
            return;

        GameObject outlineObject = new GameObject("Interaction Outline");
        outlineObject.transform.SetParent(transform, false);
        // [수정] 레버 강조가 더 잘 보이도록 테두리를 조금 더 크게 표시
        outlineObject.transform.localScale = Vector3.one * 1.18f;

        interactionOutline = outlineObject.AddComponent<SpriteRenderer>();
        interactionOutline.sprite = sprite.sprite;
        // [수정] 은색 레버와 대비되는 선명한 주황색 테두리
        interactionOutline.color = new Color(1f, 0.38f, 0.05f, 1f);
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
        LaberSetting();
    }

    private void LaberSetting()
    {
        if (InterScript.exInteract && TargetObject != null)
        {
            // [수정] On 이미지가 지정되어 있으면 교체, 없으면 기존 색상 방식 사용
            if (onSprite != null)
            {
                sprite.sprite = onSprite;
                sprite.color = Color.white;
            }
            else
            {
                sprite.color = Color.red;
            }
            SyncInteractionOutline();
            SetTargetAlpha(1f);
            if (TargetCollider != null)
            {
                TargetCollider.enabled = false;
            }
        }
        else if (!InterScript.exInteract && TargetObject != null)
        {
            if (offSprite != null)
            {
                sprite.sprite = offSprite;
            }
            sprite.color = Color.white;
            SyncInteractionOutline();
            SetTargetAlpha(0.390625f);
            if (TargetCollider != null)
            {
                TargetCollider.enabled = true;
            }
        }
    }

    private void SyncInteractionOutline()
    {
        if (interactionOutline == null || sprite == null)
            return;

        interactionOutline.sprite = sprite.sprite;
        interactionOutline.sortingLayerID = sprite.sortingLayerID;
        interactionOutline.sortingOrder = sprite.sortingOrder - 1;
    }

    private void SetTargetAlpha(float alpha)
    {
        if (targetSprites == null)
            return;

        foreach (SpriteRenderer targetRenderer in targetSprites)
        {
            if (targetRenderer == null)
                continue;

            Color color = targetRenderer.color;
            color.a = alpha;
            targetRenderer.color = color;
        }
    }
}
