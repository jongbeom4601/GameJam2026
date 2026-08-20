using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnManager : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string boxTag = "Box";


    [Header("Return Setting")]

    // 되감기 시작까지 걸리는 시간
    [SerializeField] private float rewindStartDelay = 10f;

    // 되감기 시작 속도와 점근적으로 도달할 최고 속도
    [SerializeField] private float rewindSpeed = 5f;
    [SerializeField] private float maximumRewindSpeed = 300f;
    [SerializeField] private float rewindAcceleration = 5.5f;

    // 효과음이 연결되지 않은 경우에만 사용하는 기본 역행 시간
    [SerializeField, Min(0.01f)] private float fallbackRewindDuration = 1.2f;

    // 되감기가 끝난 뒤 다음 루프 타이머가 시작되기 전 대기 시간
    [SerializeField, Min(0f)] private float timerRestartDelay = 0.3f;

    // 이 값보다 작게 움직이면 기록하지 않음
    [SerializeField] private float recordThreshold = 0.001f;


    [Header("Timer")]

    // 되감기까지 남은 시간
    [SerializeField] private float remainingTime;

    public float RemainingTime => remainingTime;
    public float LoopDuration => rewindStartDelay;


    // =====================================================
    // 상태
    // =====================================================

    private float elapsedTime;
    private float timerRestartDelayRemaining;

    private bool isRewinding = false;
    private float rewindProgress;

    // ★ Stack에 기록 가능한 상태인지
    private bool canRecord = true;

    public bool IsRewinding => isRewinding;
    public float RewindProgress => rewindProgress;


    // =====================================================
    // 추적 오브젝트
    // =====================================================

    private class TrackedObject
    {
        public Transform target;
        public Rigidbody2D rb;

        // 마지막으로 기록된 정확한 위치
        public Vector3 lastPosition;


        public TrackedObject(Transform target)
        {
            this.target = target;

            rb = target.GetComponent<Rigidbody2D>();

            lastPosition = GetPosition();
        }


        // Rigidbody2D가 있다면 Rigidbody 좌표를 기준으로 사용
        public Vector3 GetPosition()
        {
            if (rb != null)
            {
                return new Vector3(
                    rb.position.x,
                    rb.position.y,
                    target.position.z
                );
            }

            return target.position;
        }
    }


    // =====================================================
    // Stack에 저장될 이동 정보
    // =====================================================

    private struct MovementRecord
    {
        public Transform target;
        public Rigidbody2D rb;

        // 해당 움직임이 일어나기 전 정확한 위치
        public Vector3 previousPosition;


        public MovementRecord(
            Transform target,
            Rigidbody2D rb,
            Vector3 previousPosition)
        {
            this.target = target;
            this.rb = rb;
            this.previousPosition = previousPosition;
        }
    }


    private struct RewindSegment
    {
        public MovementRecord record;
        public Vector3 startPosition;
        public float distance;


        public RewindSegment(
            MovementRecord record,
            Vector3 startPosition)
        {
            this.record = record;
            this.startPosition = startPosition;
            distance = Vector3.Distance(
                startPosition,
                record.previousPosition
            );
        }
    }


    // =====================================================
    // 하나의 Stack
    // =====================================================

    private readonly Stack<MovementRecord> movementHistory
        = new Stack<MovementRecord>();


    private readonly List<TrackedObject> trackedObjects
        = new List<TrackedObject>();


    // =====================================================
    // Physics 상태 저장
    // =====================================================

    private readonly Dictionary<Collider2D, bool> colliderStates
        = new Dictionary<Collider2D, bool>();

    private readonly Dictionary<Rigidbody2D, bool> rigidbodyStates
        = new Dictionary<Rigidbody2D, bool>();


    // =====================================================
    // Unity
    // =====================================================

    private void Start()
    {
        FindObjects();

        ResetTimer(false);

        canRecord = true;
    }


    private void Update()
    {
        // 되감기 중에는 타이머 정지
        if (isRewinding)
            return;

        if (timerRestartDelayRemaining > 0f)
        {
            timerRestartDelayRemaining = Mathf.Max(
                0f,
                timerRestartDelayRemaining - Time.deltaTime);
            remainingTime = rewindStartDelay;
            return;
        }


        elapsedTime += Time.deltaTime;


        remainingTime = Mathf.Max(
            0f,
            rewindStartDelay - elapsedTime
        );


        // 시간이 다 됨
        if (remainingTime <= 0f)
        {
            BeginRewind();
        }
    }


    private void FixedUpdate()
    {
        // ★ 되감기 중 Stack에 절대로 추가하지 않음
        if (!canRecord)
            return;

        if (isRewinding)
            return;


        RecordMovement();
    }


    // =====================================================
    // 되감기 시작
    // =====================================================

    private void BeginRewind()
    {
        // 중복 실행 방지
        if (isRewinding)
            return;


        // -------------------------------------------------
        // ★ 중요
        //
        // 되감기가 시작되기 직전의 최종 위치를
        // 마지막으로 한 번 확인한다.
        // -------------------------------------------------

        if (canRecord)
        {
            RecordMovement();
        }


        // -------------------------------------------------
        // ★ 여기서부터 Stack 잠금
        //
        // 이후 Rewind가 완전히 끝날 때까지
        // MovementRecord가 절대로 추가되지 않는다.
        // -------------------------------------------------

        canRecord = false;

        isRewinding = true;
        rewindProgress = 0f;

        remainingTime = 0f;


        StartCoroutine(Rewind());
    }


    // =====================================================
    // 움직임 기록
    // =====================================================

    private void RecordMovement()
    {
        // ★ 이중 안전장치
        if (!canRecord)
            return;


        float thresholdSqr =
            recordThreshold * recordThreshold;


        foreach (TrackedObject tracked in trackedObjects)
        {
            if (tracked.target == null)
                continue;


            Vector3 currentPosition =
                tracked.GetPosition();


            // 실제 위치가 변했는지 확인
            if (
                (currentPosition - tracked.lastPosition)
                .sqrMagnitude > thresholdSqr
            )
            {
                // -----------------------------------------
                // 움직이기 전 위치 저장
                // -----------------------------------------

                movementHistory.Push(
                    new MovementRecord(
                        tracked.target,
                        tracked.rb,
                        tracked.lastPosition
                    )
                );


                // 현재 위치를 다음 기준으로 저장
                tracked.lastPosition =
                    currentPosition;
            }
        }
    }


    // =====================================================
    // 되감기
    // =====================================================

    private IEnumerator Rewind()
    {
        // ---------------------------------------------
        // 모든 Player / Box 물리 끄기
        // ---------------------------------------------

        DisablePhysics();


        List<RewindSegment> rewindSegments = BuildRewindSegments();
        float totalDistance = 0f;
        foreach (RewindSegment segment in rewindSegments)
        {
            totalDistance += segment.distance;
        }

        float rewindDuration = ResolveRewindDuration();
        float elapsedRewindTime = 0f;
        float completedDistance = 0f;
        int segmentIndex = 0;

        // The player's rewind sound begins in LateUpdate on this frame.
        yield return null;

        while (elapsedRewindTime < rewindDuration)
        {
            elapsedRewindTime = Mathf.Min(
                rewindDuration,
                elapsedRewindTime + Time.unscaledDeltaTime);

            float distanceProgress = EvaluateRewindDistanceProgress(
                elapsedRewindTime,
                rewindDuration);
            float targetDistance = totalDistance * distanceProgress;

            ApplyRewindDistance(
                rewindSegments,
                targetDistance,
                ref segmentIndex,
                ref completedDistance);

            rewindProgress = distanceProgress;

            if (elapsedRewindTime < rewindDuration)
            {
                yield return null;
            }
        }

        while (segmentIndex < rewindSegments.Count)
        {
            RewindSegment segment = rewindSegments[segmentIndex];
            if (segment.record.target != null)
            {
                SetExactPosition(
                    segment.record,
                    segment.record.previousPosition
                );
            }
            segmentIndex++;
        }


        // ---------------------------------------------
        // Stack의 모든 정보가 소모됨
        // ---------------------------------------------


        // 현재 되돌아간 위치를
        // 다음 주기의 기준 위치로 설정
        ResetTrackedPositions();


        // Physics 원래대로
        EnablePhysics();

        // [수정] 되감기 후 Box 잠금과 BoxStopper 충돌 상태 초기화
        ResetBoxInteractionState();


        // 되감기 종료
        isRewinding = false;
        rewindProgress = 1f;


        // 다음 주기 타이머 초기화
        ResetTimer(true);


        // ---------------------------------------------
        // ★ 모든 되감기가 완전히 끝난 뒤에만
        // 다시 Stack 기록 허용
        // ---------------------------------------------

        canRecord = true;
    }


    private List<RewindSegment> BuildRewindSegments()
    {
        List<RewindSegment> segments =
            new List<RewindSegment>(movementHistory.Count);
        Dictionary<Transform, Vector3> simulatedPositions =
            new Dictionary<Transform, Vector3>();

        while (movementHistory.Count > 0)
        {
            MovementRecord record = movementHistory.Pop();
            if (record.target == null)
                continue;

            if (!simulatedPositions.TryGetValue(
                record.target,
                out Vector3 startPosition))
            {
                startPosition = GetCurrentPosition(record);
            }

            segments.Add(new RewindSegment(record, startPosition));
            simulatedPositions[record.target] = record.previousPosition;
        }

        return segments;
    }


    private void ApplyRewindDistance(
        List<RewindSegment> segments,
        float targetDistance,
        ref int segmentIndex,
        ref float completedDistance)
    {
        while (segmentIndex < segments.Count)
        {
            RewindSegment segment = segments[segmentIndex];

            if (segment.record.target == null)
            {
                completedDistance += segment.distance;
                segmentIndex++;
                continue;
            }

            if (segment.distance <= 0.0001f)
            {
                SetExactPosition(
                    segment.record,
                    segment.record.previousPosition
                );
                segmentIndex++;
                continue;
            }

            if (targetDistance < completedDistance + segment.distance)
            {
                float segmentProgress = Mathf.InverseLerp(
                    completedDistance,
                    completedDistance + segment.distance,
                    targetDistance);
                SetExactPosition(
                    segment.record,
                    Vector3.Lerp(
                        segment.startPosition,
                        segment.record.previousPosition,
                        segmentProgress
                    )
                );
                return;
            }

            SetExactPosition(
                segment.record,
                segment.record.previousPosition
            );
            completedDistance += segment.distance;
            segmentIndex++;
        }
    }


    private float ResolveRewindDuration()
    {
        PlayerRewindVfx rewindVfx =
            FindAnyObjectByType<PlayerRewindVfx>();

        return Mathf.Max(
            0.01f,
            rewindVfx != null
                ? rewindVfx.RewindSoundDurationSeconds
                : fallbackRewindDuration
        );
    }


    private float EvaluateRewindDistanceProgress(
        float elapsed,
        float duration)
    {
        float startSpeed = Mathf.Max(0.01f, rewindSpeed);
        float targetSpeed = Mathf.Max(startSpeed, maximumRewindSpeed);
        float acceleration = Mathf.Max(0.01f, rewindAcceleration);

        float elapsedDistance = IntegratedRewindSpeed(
            Mathf.Clamp(elapsed, 0f, duration),
            startSpeed,
            targetSpeed,
            acceleration);
        float totalDistance = IntegratedRewindSpeed(
            duration,
            startSpeed,
            targetSpeed,
            acceleration);

        return totalDistance > 0.0001f
            ? Mathf.Clamp01(elapsedDistance / totalDistance)
            : Mathf.Clamp01(elapsed / duration);
    }


    private static float IntegratedRewindSpeed(
        float time,
        float startSpeed,
        float targetSpeed,
        float acceleration)
    {
        return targetSpeed * time -
            (targetSpeed - startSpeed) *
            (1f - Mathf.Exp(-acceleration * time)) / acceleration;
    }


    // =====================================================
    // 현재 위치 가져오기
    // =====================================================

    private Vector3 GetCurrentPosition(
        MovementRecord record)
    {
        if (record.rb != null)
        {
            return new Vector3(
                record.rb.position.x,
                record.rb.position.y,
                record.target.position.z
            );
        }


        return record.target.position;
    }


    // =====================================================
    // 정확한 위치 설정
    // =====================================================

    private void SetExactPosition(
        MovementRecord record,
        Vector3 position)
    {
        if (record.target == null)
            return;


        // Rigidbody2D가 있는 경우
        if (record.rb != null)
        {
            record.rb.position =
                new Vector2(
                    position.x,
                    position.y
                );


            // Z값까지 정확히 유지
            Vector3 transformPosition =
                record.target.position;

            transformPosition.z =
                position.z;

            record.target.position =
                new Vector3(
                    position.x,
                    position.y,
                    position.z
                );

            return;
        }


        // Rigidbody가 없는 경우
        record.target.position =
            position;
    }


    // =====================================================
    // Player / Box 찾기
    // =====================================================

    public void FindObjects()
    {
        trackedObjects.Clear();


        HashSet<Transform> alreadyAdded =
            new HashSet<Transform>();


        // Player
        GameObject[] players =
            GameObject.FindGameObjectsWithTag(
                playerTag
            );


        foreach (GameObject player in players)
        {
            AddTrackedObject(
                player.transform,
                alreadyAdded
            );
        }


        // Box
        GameObject[] boxes =
            GameObject.FindGameObjectsWithTag(
                boxTag
            );


        foreach (GameObject box in boxes)
        {
            AddTrackedObject(
                box.transform,
                alreadyAdded
            );
        }
    }


    private void AddTrackedObject(
        Transform target,
        HashSet<Transform> alreadyAdded)
    {
        if (target == null)
            return;


        if (alreadyAdded.Contains(target))
            return;


        alreadyAdded.Add(target);


        trackedObjects.Add(
            new TrackedObject(target)
        );
    }


    // =====================================================
    // Physics OFF
    // =====================================================

    private void DisablePhysics()
    {
        colliderStates.Clear();
        rigidbodyStates.Clear();


        foreach (TrackedObject tracked in trackedObjects)
        {
            if (tracked.target == null)
                continue;


            // -----------------------------------------
            // Collider2D
            // -----------------------------------------

            Collider2D[] colliders =
                tracked.target.GetComponentsInChildren
                <Collider2D>(true);


            foreach (Collider2D col in colliders)
            {
                if (
                    col == null ||
                    colliderStates.ContainsKey(col)
                )
                    continue;


                // 원래 상태 기억
                colliderStates.Add(
                    col,
                    // [수정] BoxStop이 끈 Box Collider는 되감기 후 다시 상호작용할 수 있게 복원
                    tracked.target.CompareTag(boxTag) || col.enabled
                );


                // OFF
                col.enabled = false;
            }


            // -----------------------------------------
            // Rigidbody2D
            // -----------------------------------------

            Rigidbody2D rb =
                tracked.rb;


            if (
                rb != null &&
                !rigidbodyStates.ContainsKey(rb)
            )
            {
                // 원래 상태 기억
                rigidbodyStates.Add(
                    rb,
                    rb.simulated
                );


                // 되감기 중 물리 계산 정지
                rb.simulated = false;
            }
        }
    }


    // =====================================================
    // Physics ON
    // =====================================================

    private void EnablePhysics()
    {
        // Collider 원래 상태로
        foreach (
            KeyValuePair<Collider2D, bool> pair
            in colliderStates
        )
        {
            if (pair.Key != null)
            {
                pair.Key.enabled =
                    pair.Value;
            }
        }


        // Rigidbody 원래 상태로
        foreach (
            KeyValuePair<Rigidbody2D, bool> pair
            in rigidbodyStates
        )
        {
            if (pair.Key != null)
            {
                pair.Key.simulated =
                    pair.Value;
            }
        }


        colliderStates.Clear();
        rigidbodyStates.Clear();
    }


    // =====================================================
    // 다음 기록 기준 위치 설정
    // =====================================================

    private void ResetTrackedPositions()
    {
        foreach (TrackedObject tracked in trackedObjects)
        {
            if (tracked.target == null)
                continue;


            tracked.lastPosition =
                tracked.GetPosition();
        }
    }


    private void ResetBoxInteractionState()
    {
        foreach (TrackedObject tracked in trackedObjects)
        {
            if (tracked.target == null || !tracked.target.CompareTag(boxTag))
                continue;

            BoxMovement boxMovement = tracked.target.GetComponent<BoxMovement>();
            if (boxMovement != null)
            {
                boxMovement.ResetAfterRewind();
            }
        }

        Box[] stoppers = FindObjectsByType<Box>(FindObjectsSortMode.None);
        foreach (Box stopper in stoppers)
        {
            stopper.ResetAfterRewind();
        }
    }


    // =====================================================
    // Timer
    // =====================================================

    private void ResetTimer(bool applyRestartDelay)
    {
        elapsedTime = 0f;

        timerRestartDelayRemaining = applyRestartDelay
            ? Mathf.Max(0f, timerRestartDelay)
            : 0f;

        remainingTime =
            rewindStartDelay;
    }
}
