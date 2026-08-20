using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reloads the active scene after a fixed amount of gameplay time.
/// </summary>
public sealed class SceneLoopResetter : MonoBehaviour
{
    private const float MinimumResetInterval = 0.1f;

    [Header("Time Loop")]
    [Tooltip("Seconds before the current scene is reloaded.")]
    [SerializeField, Min(MinimumResetInterval)]
    private float resetIntervalSeconds = 20f;

    private float elapsedSeconds;
    private bool isReloading;

    public float ResetIntervalSeconds => resetIntervalSeconds;

    public event Action BeforeSceneReload;

    private void Update()
    {
        if (isReloading)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;

        if (elapsedSeconds >= resetIntervalSeconds)
        {
            ReloadCurrentScene();
        }
    }

    private void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.buildIndex < 0)
        {
            Debug.LogError(
                $"Cannot reload scene '{currentScene.name}'. Add it to the active Build Profile first.",
                this);
            enabled = false;
            return;
        }

        isReloading = true;
        BeforeSceneReload?.Invoke();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void OnValidate()
    {
        resetIntervalSeconds = Mathf.Max(MinimumResetInterval, resetIntervalSeconds);
    }
}
