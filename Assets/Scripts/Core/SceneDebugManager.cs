using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Build Settings 순서를 기준으로 1/2 키로 씬을 강제 이동하는 공통 매니저.
/// 어떤 씬에서 시작해도 자동 생성되며 씬 전환 후에도 유지된다.
/// </summary>
public sealed class SceneDebugManager : MonoBehaviour
{
    private static readonly string[] SceneOrder =
    {
        "StartScene",
        "prolog",
        "stage1",
        "stage2",
        "stage3",
        "ending"
    };

    private static SceneDebugManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateManager()
    {
        if (instance != null)
            return;

        GameObject managerObject = new GameObject("Scene Debug Manager");
        instance = managerObject.AddComponent<SceneDebugManager>();
        DontDestroyOnLoad(managerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            LoadRelativeScene(-1);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            LoadRelativeScene(1);
        }
    }

    private void LoadRelativeScene(int direction)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        int currentIndex = System.Array.IndexOf(SceneOrder, currentSceneName);
        if (currentIndex < 0)
        {
            Debug.LogWarning("현재 씬은 강제 이동 목록에 없습니다.", this);
            return;
        }

        int targetIndex = currentIndex + direction;
        if (targetIndex < 0 || targetIndex >= SceneOrder.Length)
            return;

        SceneManager.LoadScene(SceneOrder[targetIndex]);
    }
}
