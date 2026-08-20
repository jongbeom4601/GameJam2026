using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Box : MonoBehaviour
{
    [SerializeField] private float positionTolerance = 0.1f;
    [Header("Box 도착 이미지")]
    [SerializeField] private Sprite stoppedBoxSprite;
    private Collider2D stopperCollider;

    private void Awake()
    {
        stopperCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Box") &&
            Vector2.Distance(transform.position, collision.transform.position) <= positionTolerance)
        {
            // [수정] 단순 접촉이 아니라 두 오브젝트가 같은 위치일 때만 도착 처리
            BoxMovement boxMovement = collision.gameObject.GetComponent<BoxMovement>();
            if (boxMovement != null)
            {
                boxMovement.StopOnStopper(stoppedBoxSprite, transform.position);
            }

            // Box가 도착하면 BoxStopper 자신의 충돌을 해제
            stopperCollider.enabled = false;
        }
    }

    public void ResetAfterRewind()
    {
        stopperCollider.enabled = true;
    }
}
