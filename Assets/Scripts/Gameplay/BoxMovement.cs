
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
    private SpriteRenderer outline;
    private LineRenderer directionArrow;
    private bool isStopped;

    public bool CanInteract => !isStopped && col != null && col.enabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        boxSprite = GetComponent<SpriteRenderer>();
        CreateInteractionVisuals();
    }

    private void CreateInteractionVisuals()
    {
        if (boxSprite != null)
        {
            GameObject outlineObject = new GameObject("Interaction Outline");
            outlineObject.transform.SetParent(transform, false);
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

    public void SetInteractionPreview(bool visible, Vector2 playerPosition, Vector2 lastMoveDirection)
    {
        if (outline != null)
            outline.enabled = visible;

        if (directionArrow == null)
            return;

        directionArrow.enabled = visible;
        if (!visible)
            return;

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

    public void StopOnStopper()
    {
        isStopped = true;
        rb.linearVelocity = Vector2.zero;
        SetInteractionPreview(false, Vector2.zero, Vector2.down);

        // [수정] Stopper에 도착한 뒤 Box와의 충돌도 해제
        col.enabled = false;
    }

    public void ResetAfterRewind()
    {
        isStopped = false;
        col.enabled = true;
    }
}
