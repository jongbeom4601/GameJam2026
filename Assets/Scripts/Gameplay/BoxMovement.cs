
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BoxMovement : MonoBehaviour
{
    [Header("이동할 타일의 사이즈")]
    [SerializeField] private float tileSize = 1f;

    private GameObject[] boxStopper;
    private Rigidbody2D rb;
    private Collider2D col;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Vector2 playerPos = collision.transform.position;
            Vector2 boxPos = transform.position;

            Vector2 dir = (boxPos - playerPos).normalized;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                dir = new Vector2(Mathf.Sign(dir.x), 0);
            }
            else
            {
                dir = new Vector2(0, Mathf.Sign(dir.y));
            }

            transform.position += (Vector3)(dir * tileSize);
            rb.linearVelocity = Vector2.zero;
        }
    }
}