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

    // 되감기 이동 속도
    [SerializeField] private float rewindSpeed = 5f;

    // 이 값보다 작게 움직이면 기록하지 않음
    [SerializeField] private float recordThreshold = 0.001f;


    [Header("Timer")]

    // 되감기까지 남은 시간
    [SerializeField] private float remainingTime;

    public float RemainingTime => remainingTime;


    // =====================================================
    // 상태
    // =====================================================

    private float elapsedTime;

    private bool isRewinding = false;

    // ★ Stack에 기록 가능한 상태인지
    private bool canRecord = true;

    public bool IsRewinding => isRewinding;


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

        ResetTimer();

        canRecord = true;
    }


    private void Update()
    {
        // 되감기 중에는 타이머 정지
        if (isRewinding)
            return;


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


        // ---------------------------------------------
        // Stack을 최신 기록부터 하나씩 꺼낸다.
        // ---------------------------------------------

        while (movementHistory.Count > 0)
        {
            MovementRecord record =
                movementHistory.Pop();


            if (record.target == null)
                continue;


            yield return StartCoroutine(
                MoveBack(
                    record,
                    record.previousPosition
                )
            );
        }


        // ---------------------------------------------
        // Stack의 모든 정보가 소모됨
        // ---------------------------------------------


        // 현재 되돌아간 위치를
        // 다음 주기의 기준 위치로 설정
        ResetTrackedPositions();


        // Physics 원래대로
        EnablePhysics();


        // 되감기 종료
        isRewinding = false;


        // 다음 주기 타이머 초기화
        ResetTimer();


        // ---------------------------------------------
        // ★ 모든 되감기가 완전히 끝난 뒤에만
        // 다시 Stack 기록 허용
        // ---------------------------------------------

        canRecord = true;
    }


    // =====================================================
    // 하나의 이동 기록 되돌리기
    // =====================================================

    private IEnumerator MoveBack(
        MovementRecord record,
        Vector3 destination)
    {
        if (record.target == null)
            yield break;


        float speed = Mathf.Max(
            0.001f,
            rewindSpeed
        );


        while (record.target != null)
        {
            Vector3 currentPosition =
                GetCurrentPosition(record);


            // 목적지까지의 거리
            float distance =
                Vector3.Distance(
                    currentPosition,
                    destination
                );


            // 충분히 가까우면 종료
            if (distance <= 0.0001f)
                break;


            Vector3 nextPosition =
                Vector3.MoveTowards(
                    currentPosition,
                    destination,
                    speed * Time.deltaTime
                );


            SetExactPosition(
                record,
                nextPosition
            );


            yield return null;
        }


        // -------------------------------------------------
        // ★ 중요
        //
        // 마지막에는 MoveTowards 결과에 의존하지 않고
        // 저장되어 있던 좌표를 정확하게 대입한다.
        // -------------------------------------------------

        if (record.target != null)
        {
            SetExactPosition(
                record,
                destination
            );
        }
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
                    col.enabled
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


    // =====================================================
    // Timer
    // =====================================================

    private void ResetTimer()
    {
        elapsedTime = 0f;

        remainingTime =
            rewindStartDelay;
    }
}