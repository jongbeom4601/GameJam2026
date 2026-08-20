
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BoxMovement : MonoBehaviour
{
    [Header("이동할 타일의 사이즈")]
    [SerializeField] private float tileSize = 1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer boxSprite;
    private Sprite originalSprite;
    private int originalSortingOrder;
    private Vector3 originalSpriteLocalPosition;
    private SpriteRenderer outline;
    private LineRenderer directionArrow;
    private bool isStopped;
    private bool isUsingStoppedSprite;
    private Vector2 stoppedPosition;

    public bool CanInteract => !isStopped && col != null && col.enabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        // [수정] 이미지가 Box의 하위 오브젝트로 이동해도 찾을 수 있게 함
        boxSprite = GetComponentInChildren<SpriteRenderer>(true);
        if (boxSprite != null)
        {
            originalSprite = boxSprite.sprite;
            originalSpriteLocalPosition = boxSprite.transform.localPosition;
        }
        CreateInteractionVisuals();
        if (boxSprite != null)
        {
            // 테두리 생성 후 실제 플레이 중인 Box의 Order 저장
            originalSortingOrder = boxSprite.sortingOrder;
        }
    }

    private void CreateInteractionVisuals()
    {
        if (boxSprite != null)
        {
            GameObject outlineObject = new GameObject("Interaction Outline");
            // 실제 Box 이미지의 위치와 Sorting 설정을 그대로 따라감
            outlineObject.transform.SetParent(boxSprite.transform, false);
            outlineObject.transform.localScale = Vector3.one * 1.1f;
            outline = outlineObject.AddComponent<SpriteRenderer>();
            outline.sprite = boxSprite.sprite;
            outline.color = Color.cyan;
            outline.sortingLayerID = boxSprite.sortingLayerID;
            outline.sortingOrder = boxSprite.sortingOrder;
            boxSprite.sortingOrder += 1;
            outline.enabled = false;
        }

        GameObject arrowObject = new GameObject("Push Direction Arrow");
        arrowObject.transform.SetParent(transform, false);
        arrowObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        directionArrow = arrowObject.AddComponent<LineRenderer>();
        directionArrow.useWorldSpace = false;
        directionArrow.positionCount = 5;
        directionArrow.SetPositions(new[]
        {
            new Vector3(-0.25f, 0f),
            new Vector3(0.25f, 0f),
            new Vector3(0.05f, 0.15f),
            new Vector3(0.25f, 0f),
            new Vector3(0.05f, -0.15f)
        });
        directionArrow.startWidth = 0.07f;
        directionArrow.endWidth = 0.07f;
        directionArrow.material = new Material(Shader.Find("Sprites/Default"));
        directionArrow.sortingOrder = boxSprite != null ? boxSprite.sortingOrder + 1 : 2;
        directionArrow.enabled = false;
    }

    private void Update()
    {
        // [수정] 되감기로 Stopper 위치에서 벗어나는 즉시 원래 이미지 복원
        if (isStopped && isUsingStoppedSprite &&
            Vector2.Distance(transform.position, stoppedPosition) > 0.1f)
        {
            boxSprite.sprite = originalSprite;
            boxSprite.sortingOrder = originalSortingOrder;
            boxSprite.transform.localPosition = originalSpriteLocalPosition;
            isUsingStoppedSprite = false;
        }
    }

    public void SetInteractionPreview(bool visible, Vector2 playerPosition, Vector2 lastMoveDirection)
    {
        if (outline != null)
        {
            outline.sprite = boxSprite.sprite;
            outline.sortingLayerID = boxSprite.sortingLayerID;
            outline.sortingOrder = boxSprite.sortingOrder - 1;
            outline.enabled = visible;
        }

        if (directionArrow == null)
            return;

        directionArrow.enabled = visible;
        if (!visible)
            return;

        directionArrow.sortingLayerID = boxSprite != null ? boxSprite.sortingLayerID : 0;
        directionArrow.sortingOrder = boxSprite != null ? boxSprite.sortingOrder + 1 : 2;

        Vector2 direction = GetPushDirection(playerPosition, lastMoveDirection);
        directionArrow.transform.localRotation = Quaternion.Euler(0f, 0f, DirectionAngle(direction));
        Color color = CanMove(direction) ? Color.green : Color.red;
        directionArrow.startColor = color;
        directionArrow.endColor = color;
    }

    public bool TryPush(Vector2 playerPosition, Vector2 lastMoveDirection)
    {
        if (!CanInteract)
            return false;

        Vector2 direction = GetPushDirection(playerPosition, lastMoveDirection);
        if (!CanMove(direction))
            return false;

        rb.position += direction * tileSize;
        rb.linearVelocity = Vector2.zero;
        return true;
    }

    private Vector2 GetPushDirection(Vector2 playerPosition, Vector2 lastMoveDirection)
    {
        Vector2 difference = (Vector2)transform.position - playerPosition;

        if (Mathf.Abs(Mathf.Abs(difference.x) - Mathf.Abs(difference.y)) <= 0.1f &&
            Vector2.Dot(lastMoveDirection, difference) > 0f)
        {
            return lastMoveDirection;
        }

        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
        {
            return new Vector2(Mathf.Sign(difference.x), 0f);
        }

        return new Vector2(0f, Mathf.Sign(difference.y));
    }

    private float DirectionAngle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private bool CanMove(Vector2 direction)
    {
        Vector2 checkSize = col.bounds.size * 0.9f;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            rb.position,
            checkSize,
            0f,
            direction,
            tileSize);

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D overlap = hit.collider;

            if (overlap == col || overlap.isTrigger || overlap.CompareTag("Player"))
            {
                continue;
            }

            // BoxStopper는 Box가 닿을 때 자기 충돌을 해제하므로 이동을 허용
            if (overlap.GetComponent<Box>() != null)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public void StopOnStopper(Sprite stoppedSprite, Vector2 stopperPosition)
    {
        isStopped = true;
        stoppedPosition = stopperPosition;
        rb.linearVelocity = Vector2.zero;
        SetInteractionPreview(false, Vector2.zero, Vector2.down);

        // [수정] BoxStopper에 지정된 완료 이미지로 교체
        if (boxSprite != null && stoppedSprite != null)
        {
            boxSprite.sprite = stoppedSprite;
        }

        if (boxSprite != null)
        {
            // [수정] BoxStopper 위에서는 완료 이미지의 Order를 0으로 고정
            boxSprite.sortingOrder = 0;
            boxSprite.transform.localPosition =
                originalSpriteLocalPosition + new Vector3(0f, -0.2f, 0f);
            isUsingStoppedSprite = true;
        }

        // [수정] Stopper에 도착한 뒤 Box와의 충돌도 해제
        col.enabled = false;
    }

    public void ResetAfterRewind()
    {
        isStopped = false;
        col.enabled = true;

        if (boxSprite != null)
        {
            boxSprite.sprite = originalSprite;
            boxSprite.sortingOrder = originalSortingOrder;
            boxSprite.transform.localPosition = originalSpriteLocalPosition;
        }
        isUsingStoppedSprite = false;
    }
}
