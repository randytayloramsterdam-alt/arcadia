using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast")]
    public float maxDistance = 3f;
    public LayerMask interactableLayer = ~0;

    [Header("Inspect Camera")]
    public float inspectFOV = 30f;
    public float inspectTransitionSpeed = 5f;

    [Header("UI — Prompt (dot + [E])")]
    public Sprite promptDotSprite;             // null = procedural circle
    public Color promptDotColor = Color.white;
    public Vector2 promptDotSize = new Vector2(6, 6);
    public Color promptTextColor = Color.white;
    public int promptFontSize = 12;

    [Header("UI — Subtitle (bottom bar)")]
    public Sprite subtitleBgSprite;            // null = solid color
    public Color subtitleBgColor = new Color(0, 0, 0, 0.75f);
    public Color subtitleTextColor = Color.white;
    public int subtitleFontSize = 16;

    private Camera cam;
    private FirstPersonController fpsController;
    private InteractableObject currentTarget;
    private InteractableObject inspecting;

    // Runtime-created UI
    private Canvas uiCanvas;
    private RectTransform promptGroup;
    private Image promptDotImage;
    private Text promptLabel;
    private RectTransform subtitlePanel;
    private Image subtitleBgImage;
    private Text subtitleText;

    private float defaultFOV;
    private Quaternion targetCamRot;

    void Start()
    {
        cam = Camera.main;
        fpsController = GetComponent<FirstPersonController>();
        defaultFOV = cam.fieldOfView;

        CreateUI();
    }

    void CreateUI()
    {
        // Canvas — sits between PS2Canvas (-1000) and InventoryUI canvas (100)
        var go = new GameObject("InteractionCanvas");
        uiCanvas = go.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 0;

        // ── Prompt: white dot + [E] label ──
        promptGroup = NewRect("Prompt", uiCanvas.transform);
        promptGroup.anchorMin = Vector2.zero;
        promptGroup.anchorMax = Vector2.zero;
        promptGroup.pivot = new Vector2(0.5f, 0.5f);
        promptGroup.sizeDelta = new Vector2(64, 48);

        Sprite dotSprite = promptDotSprite != null ? promptDotSprite : MakeCircleSprite(16);
        promptDotImage = NewImage("Dot", promptGroup, dotSprite);
        promptDotImage.color = promptDotColor;
        promptDotImage.raycastTarget = false;
        var dRect = promptDotImage.rectTransform;
        dRect.anchorMin = dRect.anchorMax = new Vector2(0.5f, 0.5f);
        dRect.sizeDelta = promptDotSize;
        dRect.anchoredPosition = Vector2.zero;

        promptLabel = NewText("Label", promptGroup, "[E]");
        promptLabel.fontSize = promptFontSize;
        promptLabel.color = promptTextColor;
        promptLabel.alignment = TextAnchor.LowerCenter;
        promptLabel.raycastTarget = false;
        var lRect = promptLabel.rectTransform;
        lRect.anchorMin = lRect.anchorMax = new Vector2(0.5f, 0.5f);
        lRect.sizeDelta = new Vector2(48, 20);
        lRect.anchoredPosition = new Vector2(0, 12);

        // Outline for readability against bright backgrounds
        var outline = promptLabel.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        promptGroup.gameObject.SetActive(false);

        // ── Subtitle panel at screen bottom ──
        subtitlePanel = NewRect("Subtitle", uiCanvas.transform);
        subtitleBgImage = subtitlePanel.gameObject.AddComponent<Image>();
        if (subtitleBgSprite != null) subtitleBgImage.sprite = subtitleBgSprite;
        subtitleBgImage.color = subtitleBgColor;
        subtitleBgImage.raycastTarget = false;
        subtitlePanel.anchorMin = new Vector2(0.12f, 0.04f);
        subtitlePanel.anchorMax = new Vector2(0.88f, 0.20f);
        subtitlePanel.offsetMin = Vector2.zero;
        subtitlePanel.offsetMax = Vector2.zero;

        subtitleText = NewText("Text", subtitlePanel, "");
        subtitleText.fontSize = subtitleFontSize;
        subtitleText.color = subtitleTextColor;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        var sRect = subtitleText.rectTransform;
        sRect.anchorMin = Vector2.zero;
        sRect.anchorMax = Vector2.one;
        sRect.offsetMin = new Vector2(20, 10);
        sRect.offsetMax = new Vector2(-20, -10);

        subtitlePanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (IntroNarrative.IsPlaying) return;
        if (inspecting != null)
        {
            HandleInspecting();
            return;
        }

        DoRaycast();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
            StartInspect(currentTarget);
    }

    void DoRaycast()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            var interactable = hit.collider.GetComponentInParent<InteractableObject>();
            if (interactable != null)
            {
                currentTarget = interactable;
                PositionPrompt(interactable.GetInteractionPoint());
                promptGroup.gameObject.SetActive(true);
                return;
            }
        }

        currentTarget = null;
        promptGroup.gameObject.SetActive(false);
    }

    void PositionPrompt(Vector3 worldPoint)
    {
        // Use viewport coords so the dot stays correct even when the camera
        // renders to a low-res RenderTexture (viewport is always 0–1).
        Vector3 vp = cam.WorldToViewportPoint(worldPoint);
        if (vp.z < 0f)
        {
            promptGroup.gameObject.SetActive(false);
            return;
        }

        promptGroup.anchoredPosition = new Vector2(
            vp.x * Screen.width,
            vp.y * Screen.height
        );
    }

    void StartInspect(InteractableObject target)
    {
        inspecting = target;
        promptGroup.gameObject.SetActive(false);
        fpsController.SetControlEnabled(false);

        // Show subtitle text at bottom
        bool shouldShow = target.showDescription;
        if (shouldShow && !string.IsNullOrEmpty(target.description))
        {
            subtitleText.text = target.description;
            subtitlePanel.gameObject.SetActive(true);
        }

        target.OnStartInteract();

        // 如果是 ComputerInteractable，它自己处理相机过渡，跳过 InteractionSystem 的过渡
        if (target is ComputerInteractable)
            return;

        Vector3 dir = (target.transform.position - cam.transform.position).normalized;
        targetCamRot = Quaternion.LookRotation(dir);

        StartCoroutine(TransitionFOV(inspectFOV));
        StartCoroutine(TransitionCamRot(targetCamRot));
    }

    void HandleInspecting()
    {
        if (Input.GetKeyDown(KeyCode.E))
            StopInspect();

        if (Input.GetKeyDown(KeyCode.Escape))
            CancelInspect();
    }

    void StopInspect()
    {
        if (inspecting != null)
            inspecting.OnStopInteract();

        CleanupInspect();
    }

    void CancelInspect()
    {
        // ESC: bail out without collecting / triggering OnStopInteract
        CleanupInspect();
    }

    void CleanupInspect()
    {
        inspecting = null;
        fpsController.SetControlEnabled(true);
        subtitlePanel.gameObject.SetActive(false);
        StartCoroutine(TransitionFOV(defaultFOV));
    }

    // ComputerInteractable 调用此方法告知电脑交互已完全结束
    // （包括 UI 退出和相机返回），无需再次按 E
    public void NotifyComputerInteractDone()
    {
        inspecting = null;
    }

    IEnumerator TransitionFOV(float targetFOV)
    {
        while (Mathf.Abs(cam.fieldOfView - targetFOV) > 0.1f)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV,
                Time.deltaTime * inspectTransitionSpeed);
            yield return null;
        }
        cam.fieldOfView = targetFOV;
    }

    IEnumerator TransitionCamRot(Quaternion target)
    {
        float elapsed = 0f;
        Quaternion start = cam.transform.rotation;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * inspectTransitionSpeed;
            cam.transform.rotation = Quaternion.Slerp(start, target, elapsed);
            yield return null;
        }
        cam.transform.rotation = target;
    }

    // ── helpers ──

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        var img = new GameObject(name, typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.sprite = sprite;
        return img;
    }

    static Text NewText(string name, Transform parent, string content)
    {
        var t = new GameObject(name, typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float c = (size - 1) * 0.5f;
        float r = c - 1;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) <= r
                    ? Color.white
                    : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
