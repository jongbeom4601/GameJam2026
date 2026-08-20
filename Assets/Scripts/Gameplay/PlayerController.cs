using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed;

    private Vector2 movementInput;

    private Rigidbody2D rb;
    private PlayerInput playerInput;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        playerInput.SwitchCurrentActionMap("Player");
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        Vector2 dir = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(dir);
    }

    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }
}
