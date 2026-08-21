using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PrologueSceneController : MonoBehaviour
{
    private static readonly string[] PrologueLines =
    {
        "길을 걸어가던 당신, 불행히도 당신이 향한 곳은 흑마법사의 영역이었습니다.",
        "절망스럽게도 이곳의 주인, 흑마법사는 당신을 환영하지 않는 것 같군요...",
        "흑마법사는 당신에게 무시무시한 저주를 걸었습니다. 바로 무한반복의 저주죠!"
    };

    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Font dialogueFont;
    [SerializeField] private string nextSceneName = "stage1";
    [SerializeField] private float inputDelay = 0.25f;
    [SerializeField] private float subtitleStartDelay = 0.75f;
    [SerializeField] private float characterInterval = 0.07f;
    [SerializeField] private float backgroundFadeDuration = 1.5f;
    [SerializeField] private float sceneFadeOutDuration = 1f;

    private float acceptInputTime;
    private float subtitleStartTime;
    private float characterTimer;
    private float backgroundFadeTime;
    private float sceneFadeOutTime;
    private int currentLineIndex;
    private int visibleCharacterCount;
    private bool isTyping;
    private bool hasSubtitleStarted;
    private bool isBackgroundFading;
    private bool isSceneFadingOut;
    private Image backgroundImage;
    private Image fadeOutImage;
    private Text dialogueText;

    private void Awake()
    {
        BuildPrologueUI();
        acceptInputTime = Time.unscaledTime + inputDelay;
        subtitleStartTime = Time.unscaledTime + subtitleStartDelay;
    }

    private void Update()
    {
        if (!hasSubtitleStarted && Time.unscaledTime >= subtitleStartTime)
        {
            hasSubtitleStarted = true;
            StartLine(0);
        }

        UpdateTypewriter();
        UpdateBackgroundFade();
        UpdateSceneFadeOut();

        if (!hasSubtitleStarted || Time.unscaledTime < acceptInputTime || isSceneFadingOut)
            return;

        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool screenTouched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool keyboardPressed = Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame);

        if (mouseClicked || screenTouched || keyboardPressed)
            HandleAdvanceInput();
    }

    private void BuildPrologueUI()
    {
        GameObject canvasObject = new GameObject("Prologue Canvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image blackBackground = CreateImage("Black Background", canvasObject.transform, Color.black);
        Stretch(blackBackground.rectTransform, Vector2.zero, Vector2.one);

        backgroundImage = CreateImage("Background", canvasObject.transform, new Color(1f, 1f, 1f, 0f));
        backgroundImage.sprite = backgroundSprite;
        Stretch(backgroundImage.rectTransform, Vector2.zero, Vector2.one);

        dialogueText = CreateText("Prologue Text", canvasObject.transform, string.Empty, 34, TextAnchor.MiddleCenter);
        dialogueText.color = new Color(1f, 0.95f, 0.82f, 1f);
        dialogueText.lineSpacing = 2.2f;
        dialogueText.resizeTextForBestFit = true;
        dialogueText.resizeTextMinSize = 26;
        dialogueText.resizeTextMaxSize = 34;
        Stretch(dialogueText.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f));

        Outline outline = dialogueText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        fadeOutImage = CreateImage("Scene Fade Out", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(fadeOutImage.rectTransform, Vector2.zero, Vector2.one);
    }

    private void UpdateTypewriter()
    {
        if (!isTyping)
            return;

        float interval = Mathf.Max(0.005f, characterInterval);
        characterTimer += Time.unscaledDeltaTime;
        while (characterTimer >= interval && isTyping)
        {
            characterTimer -= interval;
            visibleCharacterCount++;

            string currentLine = PrologueLines[currentLineIndex];
            if (visibleCharacterCount >= currentLine.Length)
            {
                visibleCharacterCount = currentLine.Length;
                isTyping = false;
            }

            dialogueText.text = BuildDisplayedText(visibleCharacterCount);
        }
    }

    private void UpdateBackgroundFade()
    {
        if (!isBackgroundFading)
            return;

        backgroundFadeTime += Time.unscaledDeltaTime;
        float alpha = Mathf.Clamp01(backgroundFadeTime / Mathf.Max(0.01f, backgroundFadeDuration));
        backgroundImage.color = new Color(1f, 1f, 1f, alpha);
        isBackgroundFading = alpha < 1f;
    }

    private void HandleAdvanceInput()
    {
        if (isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        int nextLineIndex = currentLineIndex + 1;
        if (nextLineIndex < PrologueLines.Length)
        {
            StartLine(nextLineIndex);
            return;
        }

        BeginSceneFadeOut();
    }

    private void StartLine(int lineIndex)
    {
        currentLineIndex = lineIndex;
        visibleCharacterCount = 0;
        characterTimer = 0f;
        dialogueText.text = BuildDisplayedText(0);
        isTyping = true;

        if (lineIndex == 1)
        {
            backgroundFadeTime = 0f;
            isBackgroundFading = true;
        }
    }

    private void CompleteCurrentLine()
    {
        visibleCharacterCount = PrologueLines[currentLineIndex].Length;
        dialogueText.text = BuildDisplayedText(visibleCharacterCount);
        isTyping = false;
    }

    private string BuildDisplayedText(int currentLineCharacterCount)
    {
        string displayedText = string.Empty;
        for (int lineIndex = 0; lineIndex < currentLineIndex; lineIndex++)
            displayedText += PrologueLines[lineIndex] + "\n";

        return displayedText + PrologueLines[currentLineIndex].Substring(0, currentLineCharacterCount);
    }

    private void BeginSceneFadeOut()
    {
        isSceneFadingOut = true;
        sceneFadeOutTime = 0f;
    }

    private void UpdateSceneFadeOut()
    {
        if (!isSceneFadingOut)
            return;

        sceneFadeOutTime += Time.unscaledDeltaTime;
        float alpha = Mathf.Clamp01(sceneFadeOutTime / Mathf.Max(0.01f, sceneFadeOutDuration));
        fadeOutImage.color = new Color(0f, 0f, 0f, alpha);

        if (alpha >= 1f)
            LoadNextScene();
    }

    private Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(string objectName, Transform parent, string content, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = dialogueFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void LoadNextScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"Build Profile에 '{nextSceneName}' 씬이 없습니다.", this);
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
