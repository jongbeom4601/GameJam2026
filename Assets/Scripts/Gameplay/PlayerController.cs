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
    [Header("박스 상호작용")]
    [SerializeField] private float boxInteractionRange = 1.25f;
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
    private BoxMovement selectedBox;
    private Vector2 lastMoveDirection = Vector2.down;


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
        UpdateBoxSelection();

        /* [수정] 각 씬의 ReturnManager만 사용하도록 기존 되감기 비활성화 시작
        if (!isReturning)
        {
            RecordPath();


            if (returnEX || returnningTime >= returnTime)
            {
                StartCoroutine(ReturnToStart());
                returnEX = false;
            }
        }
        [수정] 각 씬의 ReturnManager만 사용하도록 기존 되감기 비활성화 끝 */
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

        if (movementInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y)
                ? new Vector2(Mathf.Sign(movementInput.x), 0f)
                : new Vector2(0f, Mathf.Sign(movementInput.y));
        }
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed)
            return;

        // [수정] 선택된 박스 하나만 상호작용 키로 이동
        if (selectedBox != null)
        {
            selectedBox.TryPush(transform.position, lastMoveDirection);
            return;
        }

        if (isInteract)
        {
            Debug.Log("상호작용");
            giveInteract = true;
        }
    }

    private void UpdateBoxSelection()
    {
        BoxMovement nearest = null;
        float nearestDistance = boxInteractionRange * boxInteractionRange;

        BoxMovement[] boxes = FindObjectsByType<BoxMovement>(FindObjectsSortMode.None);
        foreach (BoxMovement box in boxes)
        {
            if (!box.CanInteract)
                continue;

            float distance = ((Vector2)box.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance <= nearestDistance)
            {
                nearest = box;
                nearestDistance = distance;
            }
        }

        if (selectedBox != nearest)
        {
            if (selectedBox != null)
                selectedBox.SetInteractionPreview(false, transform.position, lastMoveDirection);

            selectedBox = nearest;
        }

        if (selectedBox != null)
            selectedBox.SetInteractionPreview(true, transform.position, lastMoveDirection);
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
