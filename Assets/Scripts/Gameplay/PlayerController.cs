using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed =5f;
    [Header("경로 저장 거리 설정")]
    public float recordDistance = 0.2f;
    [Header("되돌아가기 속도 설정")]
    public float returnSpeed = 5f;
    [Header("되돌아가는 시간 설정")]
    public float returnTime = 20f;
    private float returnningTime;

   [SerializeField] private bool isInteract = false;
    public bool giveInteract = false;
    public bool isReturning = false;

    public bool returnEX = false;

    private Vector2 movementInput;
    private List<Vector3> path = new List<Vector3>();
    
    private Vector3 lastRecordedPosition;
    private Rigidbody2D rb;
    private PlayerInput playerInput;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        playerInput.SwitchCurrentActionMap("Player");
    }

    private void Start()
    {
        path.Add(transform.position);
        lastRecordedPosition = transform.position;
    }

    private void Update()
    {
        if (!isReturning)
        {
            RecordPath();


            if (returnEX || returnningTime >= returnTime)
            {
                StartCoroutine(ReturnToStart());
                returnEX = false;
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 dir = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(dir);
        returnningTime += Time.fixedDeltaTime;
    }

    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    public void OnInteract(InputValue value)
    {
        if (isInteract)
        {
            Debug.Log("상호작용");
            giveInteract = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Interactable")
        {
            isInteract = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Interactable")
        {
            isInteract = false;
            giveInteract = false;
        }

    }

    void RecordPath()
    {
        float distance = Vector3.Distance(
            transform.position,
            lastRecordedPosition
        );

        if (distance >= recordDistance)
        {
            path.Add(transform.position);
            lastRecordedPosition = transform.position;
        }
    }

    IEnumerator ReturnToStart()
    {
        if (path.Count <= 1)
            yield break;

        isReturning = true;

        for (int i = path.Count - 1; i >= 0; i--)
        {
            Vector3 targetPosition = path[i];

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    returnSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPosition;
        }

        isReturning = false;

        path.Clear();
        path.Add(transform.position);
        lastRecordedPosition = transform.position;
        returnningTime = 0f;
    }

}
