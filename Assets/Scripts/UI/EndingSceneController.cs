using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class EndingSceneController : MonoBehaviour
{
    private static readonly string[] EndingLines =
    {
        "끔찍한 저주에도 불구하고 당신은 계속해서 길을 나아갔습니다.",
        "모두 당신의 뛰어난 재치와 명석한 두뇌 덕분이죠!",
        "하지만 흑마법사는 당신을 놓아줄 생각이 없어보입니다.",
        "적어도 당분간은요..."
    };

    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Font dialogueFont;
    [SerializeField] private float inputDelay = 0.25f;
    [SerializeField] private float subtitleStartDelay = 0.75f;
    [SerializeField] private float characterInterval = 0.07f;
    [SerializeField] private float backgroundFadeDuration = 1.5f;

    private float acceptInputTime;
    private float subtitleStartTime;
    private float characterTimer;
    private float backgroundFadeTime;
    private int currentLineIndex;
    private int visibleCharacterCount;
    private bool hasSubtitleStarted;
    private bool isTyping;
    private bool isBackgroundFading;
    private Image backgroundImage;
    private Text dialogueText;

    private void Awake()
    {
        BuildEndingUI();
        acceptInputTime = Time.unscaledTime + inputDelay;
        subtitleStartTime = Time.unscaledTime + subtitleStartDelay;
    }

    private void Update()
    {
        if (!hasSubtitleStarted && Time.unscaledTime >= subtitleStartTime)
        {
            hasSubtitleStarted = true;
            StartLine(0);
            BeginBackgroundFadeIn();
        }

        UpdateTypewriter();
        UpdateBackgroundFade();

        if (!hasSubtitleStarted || Time.unscaledTime < acceptInputTime)
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

    private void BuildEndingUI()
    {
        GameObject canvasObject = new GameObject("Ending Story Canvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image blackBackground = CreateImage("Black Background", canvasObject.transform, Color.black);
        Stretch(blackBackground.rectTransform, Vector2.zero, Vector2.one);

        backgroundImage = CreateImage("Background", canvasObject.transform, new Color(1f, 1f, 1f, 0f));
        backgroundImage.sprite = backgroundSprite;
        Stretch(backgroundImage.rectTransform, Vector2.zero, Vector2.one);

        dialogueText = CreateText("Ending Text", canvasObject.transform, string.Empty, 42);
        dialogueText.color = new Color(1f, 0.95f, 0.82f, 1f);
        dialogueText.lineSpacing = 2f;
        dialogueText.resizeTextForBestFit = true;
        dialogueText.resizeTextMinSize = 26;
        dialogueText.resizeTextMaxSize = 42;
        Stretch(dialogueText.rectTransform, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.70f));

        Outline outline = dialogueText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);
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
            string currentLine = EndingLines[currentLineIndex];
            if (visibleCharacterCount >= currentLine.Length)
            {
                visibleCharacterCount = currentLine.Length;
                isTyping = false;
            }

            dialogueText.text = BuildDisplayedText(visibleCharacterCount);
        }
    }

    private void BeginBackgroundFadeIn()
    {
        backgroundFadeTime = 0f;
        isBackgroundFading = true;
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
        if (nextLineIndex < EndingLines.Length)
            StartLine(nextLineIndex);
    }

    private void StartLine(int lineIndex)
    {
        currentLineIndex = lineIndex;
        visibleCharacterCount = 0;
        characterTimer = 0f;
        dialogueText.text = BuildDisplayedText(0);
        isTyping = true;
    }

    private void CompleteCurrentLine()
    {
        visibleCharacterCount = EndingLines[currentLineIndex].Length;
        dialogueText.text = BuildDisplayedText(visibleCharacterCount);
        isTyping = false;
    }

    private string BuildDisplayedText(int currentLineCharacterCount)
    {
        string displayedText = string.Empty;
        for (int lineIndex = 0; lineIndex < currentLineIndex; lineIndex++)
            displayedText += EndingLines[lineIndex] + "\n";

        return displayedText + EndingLines[currentLineIndex].Substring(0, currentLineCharacterCount);
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(string objectName, Transform parent, string content, int fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = dialogueFont;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
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
}
