using UnityEngine;
using UnityEngine.UI;

public class CollectibleInspectUI : MonoBehaviour
{
    public static CollectibleInspectUI Instance { get; private set; }

    [Header("Overlay")]
    public Color overlayColor = new Color(0.08f, 0.08f, 0.08f, 0.88f);

    [Header("Layout")]
    [Range(0.3f, 0.95f)] public float contentWidth = 0.90f;
    [Range(0.3f, 0.95f)] public float contentHeight = 0.82f;
    [Range(0.25f, 0.7f)] public float modelRatio = 0.58f;

    [Header("Text Styles")]
    public Font font;  // null = uses built-in LegacyRuntime.ttf
    public Color titleColor = new Color(0.92f, 0.88f, 0.72f);
    public int titleFontSize = 36;
    public Color descColor = new Color(0.68f, 0.68f, 0.68f);
    public int descFontSize = 22;
    public Color dividerColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public float dividerHeight = 2f;
    public Color exitColor = new Color(0.55f, 0.55f, 0.55f);
    public int exitFontSize = 24;
    [Tooltip("Reference screen height for font scaling. At this resolution, fonts use their exact base sizes.")]
    public float referenceHeight = 1080f;

    [Header("3D Preview")]
    [Tooltip("Mouse drag sensitivity (degrees per unit of mouse delta).")]
    public float rotationSpeed = 0.2f;
    public float zoomSpeed = 2.5f;
    public float minZoom = 0.3f;
    public float maxZoom = 4f;
    public float defaultZoom = 1f;
    [Tooltip("Global baseline scale for ALL previewed models. Multiply with each item's Inspect Model Scale for final result.")]
    [Range(0.2f, 3f)] public float modelScale = 1f;
    [Tooltip("If true, the camera is automatically positioned to frame the model's bounding box.")]
    public bool autoFrame = true;
    public Color previewBgColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    public int previewLayer = 25;                  // reuse existing "Inspect" layer
    [Tooltip("Initial rotation (Euler angles) for the preview model. Per-item inspectModelRotation is added to this.")]
    public Vector3 initialRotation = Vector3.zero;
    [Tooltip("Max distance for G-key inspect raycast.")]
    public float inspectReach = 5f;

    private Canvas canvas;
    private GameObject panel;
    private RawImage rawImage;
    private Text titleText;
    private Text descText;
    private Text exitText;

    private Camera previewCam;
    private RenderTexture renderTex;
    private GameObject rig;
    private GameObject previewModel;
    private float currentZoom;
    private CollectibleItem currentItem;
    private bool isShowing;

    private Vector2 lastScreenSize;

    private FirstPersonController fpsController;
    private InteractionSystem interactionSystem;
    private Camera mainCam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        mainCam = Camera.main;
        fpsController = FindObjectOfType<FirstPersonController>();
        interactionSystem = GetComponent<InteractionSystem>();

        BuildUI();
    }

    void Update()
    {
        // G key toggle: open or close 3D inspect UI
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!isShowing)
            {
                // Open: raycast from camera to find a CollectibleItem
                if (mainCam != null)
                {
                    Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, inspectReach))
                    {
                        var item = hit.collider.GetComponentInParent<CollectibleItem>();
                        if (item != null)
                            Show(item);
                    }
                }
            }
            else
            {
                Hide();
                return;
            }
        }

        if (!isShowing) return;

        // ESC also closes the UI
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }

        // Refresh RenderTexture if screen resolution changed
        Vector2 curSize = new Vector2(Screen.width, Screen.height);
        if (lastScreenSize != curSize)
        {
            lastScreenSize = curSize;
            RefreshRenderTexture();
        }

        if (rig == null) return;

        // Mouse drag rotates the model
        if (Input.GetMouseButton(0))
        {
            float mx = Input.GetAxis("Mouse X") * rotationSpeed;
            float my = Input.GetAxis("Mouse Y") * rotationSpeed;
            rig.transform.Rotate(previewCam.transform.up, -mx, Space.World);
            rig.transform.Rotate(previewCam.transform.right, my, Space.World);
        }
    }

    void LateUpdate()
    {
        // Ensure cursor stays visible while inspecting
        if (isShowing)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ── Public API ──

    public void Show(CollectibleItem item)
    {
        if (isShowing || item == null) return;
        currentItem = item;
        isShowing = true;

        // Disable FPS controls and lock cursor visibility
        if (fpsController != null)
            fpsController.SetControlEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Populate UI text — itemNameOverride takes priority
        string itemName = "?";
        if (!string.IsNullOrEmpty(item.itemNameOverride))
            itemName = item.itemNameOverride;
        else if (item.itemData != null)
            itemName = item.itemData.itemName;

        string description = !string.IsNullOrEmpty(item.itemDescription)
            ? item.itemDescription
            : (item.itemData != null ? item.itemData.description : "");
        // Scale font sizes to current resolution
        float fontScale = Mathf.Clamp(Screen.height / referenceHeight, 0.5f, 3f);
        titleText.fontSize = Mathf.RoundToInt(titleFontSize * fontScale);
        descText.fontSize  = Mathf.RoundToInt(descFontSize * fontScale);
        exitText.fontSize  = Mathf.RoundToInt(exitFontSize * fontScale);

        titleText.text = itemName;
        descText.text = description;

        // Setup 3D preview
        SetupPreview(item);

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (!isShowing) return;
        isShowing = false;

        DestroyPreview();
        canvas.gameObject.SetActive(false);
        currentItem = null;

        // Restore FPS controls (we bypassed InteractionSystem, so handle it ourselves)
        if (fpsController != null)
            fpsController.SetControlEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ── UI Construction ──

    void BuildUI()
    {
        Font font = this.font != null ? this.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── Canvas ──
        var cgo = new GameObject("CollectibleInspectCanvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        // Full-screen overlay (semi-transparent, clickable blocker)
        var overlay = NewRect("Overlay", canvas.transform);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        var overlayImg = overlay.gameObject.AddComponent<Image>();
        overlayImg.color = overlayColor;
        overlayImg.raycastTarget = true;

        // ── Content panel (centered, anchor-based so it auto-adapts to resolution) ──
        panel = new GameObject("ContentPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay, false);
        var pImg = panel.GetComponent<Image>();
        pImg.color = Color.clear;
        pImg.raycastTarget = false;
        var pr = panel.GetComponent<RectTransform>();
        float mx = (1f - contentWidth) * 0.5f;
        float my = (1f - contentHeight) * 0.5f;
        pr.anchorMin = new Vector2(mx, my);
        pr.anchorMax = new Vector2(1f - mx, 1f - my);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;

        // ── Left: 3D model preview background ──
        var previewBg = NewRect("PreviewBg", panel.transform);
        previewBg.anchorMin = new Vector2(0f, 0f);
        previewBg.anchorMax = new Vector2(modelRatio, 1f);
        previewBg.offsetMin = Vector2.zero;
        previewBg.offsetMax = Vector2.zero;
        var bgImg = previewBg.gameObject.AddComponent<Image>();
        bgImg.color = previewBgColor;
        bgImg.raycastTarget = false;

        // RawImage for RenderTexture (inset slightly for padding)
        rawImage = NewRawImage("ModelPreview", previewBg, null);
        var rr = rawImage.rectTransform;
        rr.anchorMin = new Vector2(0.04f, 0.04f);
        rr.anchorMax = new Vector2(0.96f, 0.96f);
        rr.offsetMin = Vector2.zero;
        rr.offsetMax = Vector2.zero;

        // ── Right: text panel ──
        var rightPanel = NewRect("RightPanel", panel.transform);
        rightPanel.anchorMin = new Vector2(modelRatio, 0f);
        rightPanel.anchorMax = new Vector2(1f, 1f);
        rightPanel.offsetMin = new Vector2(24, 0);
        rightPanel.offsetMax = new Vector2(-12, 0);

        // Title (top of right panel)
        titleText = NewText("Title", rightPanel, "", font, titleFontSize, titleColor);
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.fontStyle = FontStyle.Bold;
        var trr = titleText.rectTransform;
        trr.anchorMin = new Vector2(0f, 0.78f);
        trr.anchorMax = new Vector2(1f, 0.98f);
        trr.offsetMin = Vector2.zero;
        trr.offsetMax = Vector2.zero;

        // Divider line
        var divider = NewRect("Divider", rightPanel);
        divider.anchorMin = new Vector2(0f, 0.76f);
        divider.anchorMax = new Vector2(1f, 0.77f);
        divider.offsetMin = Vector2.zero;
        divider.offsetMax = Vector2.zero;
        var dImg = divider.gameObject.AddComponent<Image>();
        dImg.color = dividerColor;
        dImg.raycastTarget = false;
        dImg.type = Image.Type.Sliced;

        // Description (middle of right panel)
        descText = NewText("Description", rightPanel, "", font, descFontSize, descColor);
        descText.alignment = TextAnchor.UpperLeft;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Truncate;
        var drr = descText.rectTransform;
        drr.anchorMin = new Vector2(0f, 0.16f);
        drr.anchorMax = new Vector2(1f, 0.75f);
        drr.offsetMin = Vector2.zero;
        drr.offsetMax = Vector2.zero;

        // Exit hint (bottom-right of right panel)
        exitText = NewText("ExitHint", rightPanel, "Press [G] to exit", font, exitFontSize, exitColor);
        exitText.alignment = TextAnchor.LowerRight;
        var er = exitText.rectTransform;
        er.anchorMin = new Vector2(0f, 0f);
        er.anchorMax = new Vector2(1f, 0.14f);
        er.offsetMin = Vector2.zero;
        er.offsetMax = Vector2.zero;

        canvas.gameObject.SetActive(false);
    }

    // ── 3D Preview Setup ──

    void SetupPreview(CollectibleItem item)
    {
        DestroyPreview();

        // Determine which model to show: inspectPrefab > item's own mesh
        GameObject sourceModel = null;
        if (item.itemData != null && item.itemData.inspectPrefab != null)
            sourceModel = item.itemData.inspectPrefab;
        else
            sourceModel = item.gameObject;  // fallback: use the world object itself

        if (sourceModel == null) return;

        int layer = LayerMask.NameToLayer("Inspect");
        if (layer < 0) layer = 25;

        // RenderTexture (match RawImage size on screen)
        int rtW = Mathf.RoundToInt(Screen.width * contentWidth * modelRatio * 0.92f);
        int rtH = Mathf.RoundToInt(Screen.height * contentHeight * 0.92f);
        rtW = Mathf.Max(rtW, 256);
        rtH = Mathf.Max(rtH, 256);
        renderTex = new RenderTexture(rtW, rtH, 24);
        renderTex.antiAliasing = 2;
        renderTex.Create();

        rawImage.texture = renderTex;

        // Preview camera (initial placement, will be adjusted by auto-framing)
        var camObj = new GameObject("PreviewCam");
        previewCam = camObj.AddComponent<Camera>();
        previewCam.clearFlags = CameraClearFlags.SolidColor;
        previewCam.backgroundColor = previewBgColor;
        previewCam.cullingMask = 1 << layer;
        previewCam.fieldOfView = 35f;
        previewCam.targetTexture = renderTex;
        previewCam.aspect = (float)rtW / rtH;   // match RawImage aspect exactly
        previewCam.transform.position = new Vector3(0f, 0f, -3f);
        previewCam.transform.LookAt(Vector3.zero);

        // Rig at origin (focal / rotation centre)
        rig = new GameObject("PreviewRig");
        rig.transform.position = Vector3.zero;

        // Instantiate the model as a child of the rig
        previewModel = Instantiate(sourceModel, rig.transform);
        previewModel.transform.localPosition = Vector3.zero;
        previewModel.transform.localRotation = Quaternion.identity;
        SetLayerRecursive(previewModel, layer);

        // Strip interactive components from the clone
        foreach (var col in previewModel.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (var mb in previewModel.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!(mb is Transform) && !(mb is MeshFilter) && !(mb is MeshRenderer) && !(mb is SkinnedMeshRenderer))
                Destroy(mb);
        }

        // Apply scale: preserve prefab's original proportions, multiply by our factors
        float finalScale = modelScale * item.inspectModelScale;
        previewModel.transform.localScale = Vector3.Scale(
            previewModel.transform.localScale, Vector3.one * finalScale);

        // ── Auto-frame the model ──
        if (autoFrame)
        {
            Bounds b = CalculateLocalBounds(previewModel);
            if (b.size.magnitude > 0.001f)
            {
                // Centre the model's bounding box on the rig origin
                previewModel.transform.localPosition = -b.center;

                // Recalculate bounds after centring (in world units, since the
                // camera will look from the world origin direction)
                b = CalculateLocalBounds(previewModel);

                float extent = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
                extent = Mathf.Max(extent, 0.1f);
                float fovRad = previewCam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float camDist = extent / Mathf.Tan(fovRad) * 1.5f;  // 1.5× padding
                camDist = Mathf.Max(camDist, 0.5f);

                previewCam.transform.position = new Vector3(0f, 0f, -camDist);
                previewCam.transform.LookAt(Vector3.zero);

                defaultZoom = camDist;
                currentZoom = camDist;
            }
        }
        else
        {
            currentZoom = defaultZoom;
        }

        // Apply initial viewing angle: global + per-item override
        rig.transform.rotation = Quaternion.Euler(initialRotation + item.inspectModelRotation);

        // Lights (children of rig so they rotate with it)
        CreateLight(rig, "PreviewKey",  new Vector3(1.2f, 1.8f, -2.5f),
            new Color(1f, 0.96f, 0.9f), 3.5f, 14f, layer);
        CreateLight(rig, "PreviewFill", new Vector3(-1.2f, 0.3f, -2.5f),
            new Color(0.6f, 0.68f, 0.82f), 1.8f, 14f, layer);
        CreateLight(rig, "PreviewRim",  new Vector3(0f, -0.3f, 2.5f),
            new Color(0.75f, 0.78f, 0.88f), 2f, 14f, layer);
    }

    /// <summary>Recreate the RenderTexture at the current screen resolution.</summary>
    void RefreshRenderTexture()
    {
        if (previewCam == null || rawImage == null) return;

        int rtW = Mathf.RoundToInt(Screen.width * contentWidth * modelRatio * 0.92f);
        int rtH = Mathf.RoundToInt(Screen.height * contentHeight * 0.92f);
        rtW = Mathf.Max(rtW, 256);
        rtH = Mathf.Max(rtH, 256);

        if (renderTex != null)
        {
            renderTex.Release();
            Destroy(renderTex);
        }

        renderTex = new RenderTexture(rtW, rtH, 24);
        renderTex.antiAliasing = 2;
        renderTex.Create();

        rawImage.texture = renderTex;
        previewCam.targetTexture = renderTex;
        previewCam.aspect = (float)rtW / rtH;
    }

    /// <summary>Compute axis-aligned bounds of all renderers, relative to the rig origin.</summary>
    Bounds CalculateLocalBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.one * 0.5f);

        // Use world-space bounds and make them relative to the rig.
        // Rig is at world origin so this is effectively world-space, and
        // correctly includes the modelScale factor in the extent.
        Vector3 rigPos = rig != null ? rig.transform.position : Vector3.zero;
        Bounds b = new Bounds(renderers[0].bounds.center - rigPos, Vector3.zero);
        foreach (var r in renderers)
        {
            b.Encapsulate(r.bounds.min - rigPos);
            b.Encapsulate(r.bounds.max - rigPos);
        }
        return b;
    }

    void DestroyPreview()
    {
        if (rig != null) { Destroy(rig); rig = null; }
        previewModel = null;

        if (previewCam != null) { Destroy(previewCam.gameObject); previewCam = null; }

        if (renderTex != null)
        {
            renderTex.Release();
            Destroy(renderTex);
            renderTex = null;
        }

        if (rawImage != null)
            rawImage.texture = null;
    }

    void OnDestroy()
    {
        DestroyPreview();
        if (Instance == this) Instance = null;
    }

    // ── Helpers ──

    void CreateLight(GameObject parent, string name, Vector3 localPos, Color color, float intensity, float range, int layer)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = localPos;
        var lt = obj.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = color;
        lt.intensity = intensity;
        lt.range = range;
        lt.cullingMask = 1 << layer;
        lt.shadows = LightShadows.None;
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

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

    static RawImage NewRawImage(string name, Transform parent, Texture tex)
    {
        var ri = new GameObject(name, typeof(RawImage)).GetComponent<RawImage>();
        ri.transform.SetParent(parent, false);
        ri.texture = tex;
        ri.raycastTarget = false;
        return ri;
    }
}
