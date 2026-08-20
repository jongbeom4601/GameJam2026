using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Provides button actions for the scene-authored start menu.
/// </summary>
public sealed class StartMenuActions : MonoBehaviour
{
    [Tooltip("Scene loaded when the Start button is pressed.")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private bool isLoading;

    public void StartGame()
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(gameSceneName) ||
            !Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError(
                $"Cannot load game scene '{gameSceneName}'. Add it to the active Build Profile.",
                this);
            return;
        }

        isLoading = true;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
