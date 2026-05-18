using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroNarrative : MonoBehaviour
{
    [Header("Sentences")]
    [TextArea(2, 5)]
    public string[] sentences = new string[]
    {
        "我的头剧烈的疼痛",
        "眼球后面像是有一道血淋淋的创口",
        "也许喝杯咖啡就好了。",
        "......"
    };

    [TextArea(2, 5)]
    public string revealSubtitle = "眼前是旋转的风扇，似曾相识的天花板，以及电灯的滋滋声。";

    [Header("Style")]
    public Color textColor = new Color(0.9f, 0.9f, 0.9f);
    public int fontSize = 26;
    public float fadeInDuration = 2.5f;

    [Header("Subtitle")]
    public Color subtitleBgColor = new Color(0, 0, 0, 0.8f);
    public Color subtitleTextColor = new Color(0.85f, 0.85f, 0.85f);
    public int subtitleFontSize = 16;

    private Canvas canvas;
    private Image blackOverlay;
    private Text sentenceText;
    private CanvasGroup sentenceGroup;
    private RectTransform subtitlePanel;
    private Text subtitleText;
    private CanvasGroup subtitleGroup;

    public static bool IsPlaying { get; private set; }

    private int currentIndex = -1; // -1 = initial black, before any sentence
    private bool waitingForClick;
    private bool sequenceComplete;
    private FirstPersonController fpsController;

    void Awake()
    {
        fpsController = GetComponent<FirstPersonController>();
        BuildUI();
    }

    void Start()
    {
        IsPlaying = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ensure camera renders behind the overlay
        var cam = Camera.main;
        if (cam != null) cam.enabled = true;

        // First click shows the first sentence
        waitingForClick = true;
    }

    void BuildUI()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvas = new GameObject("IntroCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        // ── Full-screen black overlay ──
        var overlayGo = new GameObject("BlackOverlay", typeof(Image));
        overlayGo.transform.SetParent(canvas.transform, false);
        blackOverlay = overlayGo.GetComponent<Image>();
        blackOverlay.color = Color.black;
        blackOverlay.raycastTarget = true; // blocks clicks from going through
        var or = blackOverlay.rectTransform;
        or.anchorMin = Vector2.zero;
        or.anchorMax = Vector2.one;
        or.offsetMin = Vector2.zero;
        or.offsetMax = Vector2.zero;

        // ── Sentence text (centered) ──
        var sentGo = new GameObject("SentenceText", typeof(Text));
        sentGo.transform.SetParent(blackOverlay.transform, false);
        sentenceText = sentGo.GetComponent<Text>();
        sentenceText.font = font;
        sentenceText.fontSize = fontSize;
        sentenceText.color = textColor;
        sentenceText.alignment = TextAnchor.MiddleCenter;
        sentenceText.horizontalOverflow = HorizontalWrapMode.Wrap;
        sentenceText.text = "";
        sentenceText.raycastTarget = false;
        var sr = sentenceText.rectTransform;
        sr.anchorMin = new Vector2(0.1f, 0.3f);
        sr.anchorMax = new Vector2(0.9f, 0.7f);
        sr.offsetMin = Vector2.zero;
        sr.offsetMax = Vector2.zero;

        sentenceGroup = sentGo.AddComponent<CanvasGroup>();
        sentenceGroup.alpha = 0f;

        // ── Subtitle bar (bottom) ──
        subtitlePanel = NewRect("Subtitle", canvas.transform);
        var bg = subtitlePanel.gameObject.AddComponent<Image>();
        bg.color = subtitleBgColor;
        bg.raycastTarget = false;
        subtitlePanel.anchorMin = new Vector2(0.08f, 0.04f);
        subtitlePanel.anchorMax = new Vector2(0.92f, 0.18f);
        subtitlePanel.offsetMin = Vector2.zero;
        subtitlePanel.offsetMax = Vector2.zero;

        subtitleText = NewText("SubText", subtitlePanel, "", font, subtitleFontSize, subtitleTextColor);
        subtitleText.alignment = TextAnchor.MiddleCenter;
        var stRect = subtitleText.rectTransform;
        stRect.anchorMin = Vector2.zero;
        stRect.anchorMax = Vector2.one;
        stRect.offsetMin = new Vector2(20, 8);
        stRect.offsetMax = new Vector2(-20, -8);

        subtitleGroup = subtitlePanel.gameObject.AddComponent<CanvasGroup>();
        subtitleGroup.alpha = 0f;
        subtitlePanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (sequenceComplete) return;

        if (waitingForClick && Input.GetMouseButtonDown(0))
        {
            waitingForClick = false;
            Advance();
        }
    }

    void Advance()
    {
        currentIndex++;

        if (currentIndex < sentences.Length)
        {
            // Show next sentence
            StopAllCoroutines();
            StartCoroutine(ShowSentence(sentences[currentIndex]));
        }
        else if (currentIndex == sentences.Length)
        {
            // All sentences shown, now fade in the camera
            StopAllCoroutines();
            StartCoroutine(FadeInAndReveal());
        }
    }

    IEnumerator ShowSentence(string text)
    {
        sentenceText.text = text;

        // Fade text in
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            sentenceGroup.alpha = Mathf.Clamp01(elapsed / 0.4f);
            yield return null;
        }
        sentenceGroup.alpha = 1f;

        waitingForClick = true;
    }

    IEnumerator FadeInAndReveal()
    {
        // Fade out black overlay to reveal the room
        float elapsed = 0f;
        Color startColor = blackOverlay.color;
        Color endColor = new Color(0, 0, 0, 0);

        // Fade sentence text out
        float sentStartAlpha = sentenceGroup.alpha;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            blackOverlay.color = Color.Lerp(startColor, endColor, t);
            sentenceGroup.alpha = Mathf.Lerp(sentStartAlpha, 0f, t);
            yield return null;
        }

        blackOverlay.color = endColor;
        blackOverlay.raycastTarget = false; // clicks pass through now
        sentenceGroup.alpha = 0f;

        // Show subtitle
        subtitleText.text = revealSubtitle;
        subtitlePanel.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            subtitleGroup.alpha = Mathf.Clamp01(elapsed / 0.6f);
            yield return null;
        }
        subtitleGroup.alpha = 1f;

        waitingForClick = true;

        // Wait for final click
        while (!Input.GetMouseButtonDown(0))
            yield return null;

        // Dismiss subtitle
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            subtitleGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            yield return null;
        }
        subtitleGroup.alpha = 0f;
        subtitlePanel.gameObject.SetActive(false);

        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        sequenceComplete = true;
        IsPlaying = false;

        // Tell FPC to begin standing up
        if (fpsController != null)
            fpsController.BeginIntroSequence();
    }

    // ── helpers ──

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Text NewText(string name, Transform parent, string content, Font font, int size, Color color)
    {
        var t = new GameObject(name, typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.text = content;
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }
}
