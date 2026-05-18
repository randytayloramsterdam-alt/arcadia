using UnityEngine;
using UnityEngine.UI;

public class ComputerUIController : MonoBehaviour
{
    [Header("UI Colors (PS2 style)")]
    public Color bgColor = new Color(0.05f, 0.05f, 0.08f, 0.97f);
    public Color borderColor = new Color(0.35f, 0.35f, 0.4f, 1f);
    public Color textColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color buttonColor = new Color(0.12f, 0.12f, 0.14f, 1f);
    public Color buttonHoverColor = new Color(0.22f, 0.22f, 0.25f, 1f);

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;

    private Canvas canvas;
    private GameObject rootPanel;
    private AudioSource uiAudioSource;
    private bool isOpen;

    public bool IsOpen => isOpen;

    public System.Action OnClose;

    void Awake()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;

        BuildUI();
        rootPanel.SetActive(false);
    }

    void BuildUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        rootPanel = MakePanel("Root", transform, bgColor);
        FullStretch(rootPanel.GetComponent<RectTransform>());

        var border = MakePanel("Border", rootPanel.transform, Color.clear);
        DestroyImmediate(border.GetComponent<Image>());
        var bImg = border.AddComponent<RawImage>();
        bImg.color = borderColor;
        var br = border.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.03f, 0.03f);
        br.anchorMax = new Vector2(0.97f, 0.97f);
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;
        var outline = border.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2, 2);

        var innerBg = MakePanel("InnerBg", border.transform, new Color(0.07f, 0.07f, 0.09f, 1f));
        var ibRect = innerBg.GetComponent<RectTransform>();
        ibRect.anchorMin = new Vector2(0.01f, 0.01f);
        ibRect.anchorMax = new Vector2(0.99f, 0.99f);
        ibRect.offsetMin = Vector2.zero;
        ibRect.offsetMax = Vector2.zero;

        var title = MakeLabel("Title", innerBg.transform, "COMPUTER",
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), 32, textColor);
        title.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var tRect = title.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 0.88f);
        tRect.anchorMax = new Vector2(0.5f, 0.88f);
        tRect.sizeDelta = new Vector2(300, 40);
        tRect.anchoredPosition = Vector2.zero;

        var hint = MakeLabel("Hint", innerBg.transform,
            "Press [ESC] or click below to exit.",
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), 14,
            new Color(0.4f, 0.4f, 0.4f, 1f));
        var hRect = hint.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.5f, 0.03f);
        hRect.anchorMax = new Vector2(0.5f, 0.03f);
        hRect.sizeDelta = new Vector2(400, 24);
        hRect.anchoredPosition = Vector2.zero;

        var exitBtn = MakeButton("ExitButton", innerBg.transform, "[ EXIT ]");
        var btnRect = exitBtn.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.08f);
        btnRect.anchorMax = new Vector2(0.5f, 0.08f);
        btnRect.sizeDelta = new Vector2(200, 50);
        btnRect.anchoredPosition = Vector2.zero;
        exitBtn.GetComponent<Button>().onClick.AddListener(() => Close());

        var content = MakeLabel("Content", innerBg.transform,
            "[ Computer UI - Phase 1 ]",
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), 20, textColor);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.sizeDelta = new Vector2(400, 30);
        cRect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        rootPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlaySound(openSound);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        rootPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlaySound(closeSound);
        OnClose?.Invoke();
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && uiAudioSource != null)
            uiAudioSource.PlayOneShot(clip);
    }

    GameObject MakePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    GameObject MakeLabel(string name, Transform parent, string text, Font font, int size, Color color)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        return go;
    }

    GameObject MakeButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = buttonColor;

        var txt = new GameObject("Label", typeof(Text));
        txt.transform.SetParent(go.transform, false);
        var t = txt.GetComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.color = textColor;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        var tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonHoverColor * 1.2f;
        btn.colors = colors;

        return go;
    }

    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}