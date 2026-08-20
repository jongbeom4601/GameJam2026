using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Records keyboard changes and the player's real movement result for rewind playback.
/// Reverse playback must follow PoseSamples instead of reversing keyboard input.
/// </summary>
public sealed class PlayerTimeLoopRecorder : MonoBehaviour
{
    private const float MinimumDuration = 0.1f;
    private const float MinimumSampleInterval = 0.01f;

    [Header("Recording Target")]
    [Tooltip("Transform to record. Uses this GameObject when left empty.")]
    [SerializeField] private Transform recordedTransform;

    [Tooltip("Scene loop that saves this history immediately before reloading.")]
    [SerializeField] private SceneLoopResetter sceneLoopResetter;

    [Header("History")]
    [Tooltip("Use the SceneLoopResetter interval as the history duration.")]
    [SerializeField] private bool useSceneResetInterval = true;

    [Tooltip("Seconds of history to keep when not using the scene reset interval.")]
    [SerializeField, Min(MinimumDuration)]
    private float historyDurationSeconds = 20f;

    [Tooltip("Seconds between actual position samples. 0.02 records at up to 50 samples per second.")]
    [SerializeField, Min(MinimumSampleInterval)]
    private float positionSampleIntervalSeconds = 0.02f;

    private readonly Queue<KeyboardEventRecord> keyboardEvents = new();
    private readonly Queue<PlayerPoseRecord> poseSamples = new();

    private float recordingStartedAt;
    private float nextPositionSampleAt;
    private bool isSubscribedToSceneLoop;

    public float HistoryDurationSeconds =>
        useSceneResetInterval && sceneLoopResetter != null
            ? sceneLoopResetter.ResetIntervalSeconds
            : historyDurationSeconds;

    public int KeyboardEventCount => keyboardEvents.Count;
    public int PoseSampleCount => poseSamples.Count;

    private void Awake()
    {
        if (recordedTransform == null)
        {
            recordedTransform = transform;
        }
    }

    private void OnEnable()
    {
        BeginRecording();
        TrySubscribeToSceneLoop();
    }

    private void Start()
    {
        // Start is a second chance in case the scene loop object was initialized later.
        TrySubscribeToSceneLoop();
    }

    private void LateUpdate()
    {
        float currentTime = Time.time;

        CaptureKeyboardEvents(currentTime);

        if (currentTime >= nextPositionSampleAt)
        {
            RecordCurrentPose(currentTime);
            nextPositionSampleAt = currentTime + positionSampleIntervalSeconds;
        }

        TrimExpiredRecords(currentTime);
    }

    private void OnDisable()
    {
        UnsubscribeFromSceneLoop();
    }

    public PlayerTimeLoopHistory CreateSnapshot()
    {
        return new PlayerTimeLoopHistory(keyboardEvents.ToArray(), poseSamples.ToArray());
    }

    private void BeginRecording()
    {
        keyboardEvents.Clear();
        poseSamples.Clear();

        recordingStartedAt = Time.time;
        nextPositionSampleAt = recordingStartedAt + positionSampleIntervalSeconds;
        RecordCurrentPose(recordingStartedAt);
    }

    private void TrySubscribeToSceneLoop()
    {
        if (isSubscribedToSceneLoop)
        {
            return;
        }

        if (sceneLoopResetter == null)
        {
            sceneLoopResetter = FindAnyObjectByType<SceneLoopResetter>();
        }

        if (sceneLoopResetter == null)
        {
            return;
        }

        sceneLoopResetter.BeforeSceneReload += SaveHistoryForNextScene;
        isSubscribedToSceneLoop = true;
    }

    private void UnsubscribeFromSceneLoop()
    {
        if (!isSubscribedToSceneLoop || sceneLoopResetter == null)
        {
            return;
        }

        sceneLoopResetter.BeforeSceneReload -= SaveHistoryForNextScene;
        isSubscribedToSceneLoop = false;
    }

    private void SaveHistoryForNextScene()
    {
        float currentTime = Time.time;

        // Scene reload happens during Update, before this component's LateUpdate.
        // Capture the final input changes and resolved position before the objects are destroyed.
        CaptureKeyboardEvents(currentTime);
        RecordCurrentPose(currentTime);
        TrimExpiredRecords(currentTime);

        TimeLoopHistoryStore.Save(CreateSnapshot());
    }

    private void CaptureKeyboardEvents(float currentTime)
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        foreach (KeyControl keyControl in keyboard.allKeys)
        {
            if (keyControl.wasPressedThisFrame)
            {
                RecordKeyboardEvent(keyControl.keyCode, true, currentTime);
            }

            if (keyControl.wasReleasedThisFrame)
            {
                RecordKeyboardEvent(keyControl.keyCode, false, currentTime);
            }
        }
    }

    private void RecordKeyboardEvent(Key key, bool wasPressed, float currentTime)
    {
        keyboardEvents.Enqueue(new KeyboardEventRecord(
            currentTime - recordingStartedAt,
            key,
            wasPressed,
            recordedTransform.position,
            recordedTransform.rotation));
    }

    private void RecordCurrentPose(float currentTime)
    {
        poseSamples.Enqueue(new PlayerPoseRecord(
            currentTime - recordingStartedAt,
            recordedTransform.position,
            recordedTransform.rotation));
    }

    private void TrimExpiredRecords(float currentTime)
    {
        float cutoffTime = currentTime - recordingStartedAt - HistoryDurationSeconds;

        while (keyboardEvents.Count > 0 && keyboardEvents.Peek().TimeSeconds < cutoffTime)
        {
            keyboardEvents.Dequeue();
        }

        while (poseSamples.Count > 1 && poseSamples.Peek().TimeSeconds < cutoffTime)
        {
            poseSamples.Dequeue();
        }
    }

    private void OnValidate()
    {
        historyDurationSeconds = Mathf.Max(MinimumDuration, historyDurationSeconds);
        positionSampleIntervalSeconds = Mathf.Max(MinimumSampleInterval, positionSampleIntervalSeconds);
    }
}

public readonly struct KeyboardEventRecord
{
    public KeyboardEventRecord(
        float timeSeconds,
        Key key,
        bool wasPressed,
        Vector3 actualPosition,
        Quaternion actualRotation)
    {
        TimeSeconds = timeSeconds;
        Key = key;
        WasPressed = wasPressed;
        ActualPosition = actualPosition;
        ActualRotation = actualRotation;
    }

    public float TimeSeconds { get; }
    public Key Key { get; }
    public bool WasPressed { get; }
    public Vector3 ActualPosition { get; }
    public Quaternion ActualRotation { get; }
}

public readonly struct PlayerPoseRecord
{
    public PlayerPoseRecord(float timeSeconds, Vector3 position, Quaternion rotation)
    {
        TimeSeconds = timeSeconds;
        Position = position;
        Rotation = rotation;
    }

    public float TimeSeconds { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
}

public sealed class PlayerTimeLoopHistory
{
    public PlayerTimeLoopHistory(
        KeyboardEventRecord[] keyboardEvents,
        PlayerPoseRecord[] poseSamples)
    {
        KeyboardEvents = keyboardEvents;
        PoseSamples = poseSamples;
    }

    public IReadOnlyList<KeyboardEventRecord> KeyboardEvents { get; }
    public IReadOnlyList<PlayerPoseRecord> PoseSamples { get; }
}

/// <summary>
/// Keeps the latest recording alive while the current scene is reloaded.
/// </summary>
public static class TimeLoopHistoryStore
{
    public static PlayerTimeLoopHistory Latest { get; private set; }

    public static bool TryGetLatest(out PlayerTimeLoopHistory history)
    {
        history = Latest;
        return history != null;
    }

    public static void Clear()
    {
        Latest = null;
    }

    internal static void Save(PlayerTimeLoopHistory history)
    {
        Latest = history;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearOnPlayStart()
    {
        Clear();
    }
}
