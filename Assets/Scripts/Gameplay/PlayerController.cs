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
    [SerializeField] private float recordDistance = 0.2f;
    [Header("되돌아가기 속도 설정")]
    [SerializeField] private float returnSpeed = 7.5f;
    [SerializeField] private float maximumReturnSpeed = 75f;
    [SerializeField] private float returnAcceleration = 5.5f;
    [Header("되돌아가는 시간 설정")]
    [SerializeField] private float returnTime = 20f;
    [SerializeField, Min(0f)] private float timerRestartDelay = 0.3f;
    private float returnningTime;
    private float timerRestartDelayRemaining;

    [SerializeField] private bool isInteract = false;
    public bool giveInteract = false;
    private bool isReturning = false;
    private bool isPreparingReturn = false;
    private float returnProgress;

    public bool returnEX = false;

    private Vector2 movementInput;
    private List<Vector3> path = new List<Vector3>();
    
    private Vector3 lastRecordedPosition;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private PlayerRewindVfx rewindVfx;

    public float ReturnDurationSeconds => returnTime;
    public float ElapsedReturnSeconds => Mathf.Clamp(returnningTime, 0f, returnTime);
    public bool IsReturning => isReturning;
    public bool IsPreparingReturn => isPreparingReturn;
    public float ReturnProgress => Mathf.Clamp01(returnProgress);


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        rewindVfx = GetComponent<PlayerRewindVfx>();
        playerInput.SwitchCurrentActionMap("Player");
    }

    private void Start()
    {
        path.Add(transform.position);
        lastRecordedPosition = transform.position;
    }

    private void Update()
    {
        if (isReturning)
        {
            return;
        }

        RecordPath();

        if (returnEX)
        {
            BeginReturn();
            return;
        }

        UpdatePreparationAnimation();

        if (returnningTime >= returnTime)
        {
            BeginReturn();
        }
    }

    private void FixedUpdate()
    {
        if (isReturning)
        {
            return;
        }

        Vector2 dir = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(dir);

        if (timerRestartDelayRemaining > 0f)
        {
            timerRestartDelayRemaining = Mathf.Max(
                0f,
                timerRestartDelayRemaining - Time.fixedDeltaTime);
            return;
        }

        returnningTime += Time.fixedDeltaTime;
    }

    public void OnMove(InputValue value)
    {
        movementInput = isReturning
            ? Vector2.zero
            : value.Get<Vector2>();
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

    private void UpdatePreparationAnimation()
    {
        if (rewindVfx == null)
        {
            isPreparingReturn = false;
            return;
        }

        float safeReturnTime = Mathf.Max(0.01f, returnTime);
        float preparationDuration = Mathf.Min(
            safeReturnTime,
            rewindVfx.PreparationDurationSeconds);
        float preparationStartTime = safeReturnTime - preparationDuration;

        if (returnningTime < preparationStartTime)
        {
            if (isPreparingReturn)
            {
                rewindVfx.HidePreparation();
            }

            isPreparingReturn = false;
            return;
        }

        isPreparingReturn = true;
        float preparationProgress = Mathf.InverseLerp(
            preparationStartTime,
            safeReturnTime,
            returnningTime);
        rewindVfx.SetPreparationProgress(preparationProgress);
    }

    private void BeginReturn()
    {
        returnEX = false;
        isPreparingReturn = false;
        movementInput = Vector2.zero;

        if (rewindVfx != null)
        {
            rewindVfx.HidePreparation();
        }

        StartCoroutine(ReturnToStart());
    }

    IEnumerator ReturnToStart()
    {
        if (path.Count <= 1)
        {
            returnningTime = 0f;
            timerRestartDelayRemaining = timerRestartDelay;
            returnProgress = 1f;
            yield break;
        }

        isReturning = true;
        returnProgress = 0f;

        float totalReturnDistance = Vector3.Distance(transform.position, path[path.Count - 1]);

        for (int i = path.Count - 1; i > 0; i--)
        {
            totalReturnDistance += Vector3.Distance(path[i], path[i - 1]);
        }

        totalReturnDistance = Mathf.Max(0.001f, totalReturnDistance);
        float completedDistance = 0f;
        float currentReturnSpeed = Mathf.Max(0.01f, returnSpeed);
        float targetReturnSpeed = Mathf.Max(currentReturnSpeed, maximumReturnSpeed);

        for (int i = path.Count - 1; i >= 0; i--)
        {
            Vector3 targetPosition = path[i];
            float segmentDistance = Vector3.Distance(transform.position, targetPosition);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                float accelerationStep = 1f - Mathf.Exp(
                    -Mathf.Max(0.01f, returnAcceleration) * Time.deltaTime);
                currentReturnSpeed = Mathf.Lerp(
                    currentReturnSpeed,
                    targetReturnSpeed,
                    accelerationStep);

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    currentReturnSpeed * Time.deltaTime
                );

                float segmentProgress = segmentDistance -
                    Vector3.Distance(transform.position, targetPosition);
                returnProgress = (completedDistance + segmentProgress) / totalReturnDistance;

                yield return null;
            }

            transform.position = targetPosition;
            completedDistance += segmentDistance;
            returnProgress = completedDistance / totalReturnDistance;
        }

        returnProgress = 1f;
        isReturning = false;

        path.Clear();
        path.Add(transform.position);
        lastRecordedPosition = transform.position;
        returnningTime = 0f;
        timerRestartDelayRemaining = timerRestartDelay;
    }

}
