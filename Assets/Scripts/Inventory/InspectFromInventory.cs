using UnityEngine;
using UnityEngine.UI;

public class InspectFromInventory : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Mouse drag sensitivity for 360 spin.")]
    public float rotationSpeed = 200f;
    public float zoomSpeed = 2f;
    public float minZoom = 0.3f;
    public float maxZoom = 3f;
    public float defaultZoom = 1f;

    [Header("Description Panel")]
    public Sprite descPanelBgSprite;           // null = solid color
    public Color descPanelBgColor = new Color(0.02f, 0.02f, 0.02f, 0.9f);
    public Color descTitleColor = new Color(0.9f, 0.85f, 0.7f);
    public Color descBodyColor = new Color(0.65f, 0.65f, 0.65f);
    public Color descDividerColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private bool isInspecting;
    private GameObject rig;
    private GameObject itemInstance;
    private float currentZoom;
    private Camera mainCam;
    private InventoryItem currentItem;

    // Saved camera state
    private CameraClearFlags savedClearFlags;
    private Color savedBgColor;
    private int savedCullingMask;

    // Description UI (right 1/3 panel)
    private Canvas descCanvas;
    private Text descTitle;
    private Text descBody;

    public bool IsInspecting => isInspecting;

    void Awake()
    {
        mainCam = Camera.main;
        BuildDescriptionUI();
    }

    void BuildDescriptionUI()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var go = new GameObject("InspectCanvas");
        descCanvas = go.AddComponent<Canvas>();
        descCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        descCanvas.sortingOrder = 50;

        // ── Right panel: 30% width, full height ──
        var panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(descCanvas.transform, false);
        var panelImg = panel.GetComponent<Image>();
        if (descPanelBgSprite != null) panelImg.sprite = descPanelBgSprite;
        panelImg.color = descPanelBgColor;
        panelImg.raycastTarget = false;
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.7f, 0f);
        pr.anchorMax = new Vector2(1f, 1f);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;

        // Divider line
        var div = new GameObject("Divider", typeof(Image));
        div.transform.SetParent(panel.transform, false);
        div.GetComponent<Image>().color = descDividerColor;
        div.GetComponent<Image>().raycastTarget = false;
        var dr = div.GetComponent<RectTransform>();
        dr.anchorMin = new Vector2(0f, 0f);
        dr.anchorMax = new Vector2(0.004f, 1f);
        dr.offsetMin = Vector2.zero;
        dr.offsetMax = Vector2.zero;

        // Title
        var title = NewText("Title", panel.transform, "", font, 22, descTitleColor);
        title.alignment = TextAnchor.UpperLeft;
        title.fontStyle = FontStyle.Bold;
        var tr = title.rectTransform;
        tr.anchorMin = new Vector2(0.04f, 0.88f);
        tr.anchorMax = new Vector2(0.96f, 0.96f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        descTitle = title;

        // Description body
        var body = NewText("Body", panel.transform, "", font, 14, descBodyColor);
        body.alignment = TextAnchor.UpperLeft;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Truncate;
        var br = body.rectTransform;
        br.anchorMin = new Vector2(0.04f, 0.08f);
        br.anchorMax = new Vector2(0.96f, 0.87f);
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;
        descBody = body;

        descCanvas.gameObject.SetActive(false);
    }

    public void StartInspect(InventoryItem item)
    {
        if (item == null || item.inspectPrefab == null || isInspecting) return;

        currentItem = item;
        isInspecting = true;
        currentZoom = defaultZoom;

        // Save camera state
        savedClearFlags = mainCam.clearFlags;
        savedBgColor = mainCam.backgroundColor;
        savedCullingMask = mainCam.cullingMask;

        // Black background, only render Inspect layer
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = Color.black;
        int inspectLayer = LayerMask.NameToLayer("Inspect");
        if (inspectLayer < 0) inspectLayer = 25;
        int inspectMask = 1 << inspectLayer;
        mainCam.cullingMask = inspectMask;

        // Rig in front of camera
        rig = new GameObject("InspectRig");
        rig.transform.position = mainCam.transform.position + mainCam.transform.forward * currentZoom;

        // Instantiate item
        itemInstance = Instantiate(item.inspectPrefab, rig.transform);
        itemInstance.transform.localPosition = Vector3.zero;
        itemInstance.transform.localRotation = Quaternion.identity;
        SetLayerRecursive(itemInstance, inspectLayer);

        // Dedicated lights — only affect the Inspect layer
        CreateInspectLight(rig, "KeyLight",
            new Vector3(1.5f, 2f, -2f),
            new Color(1f, 0.95f, 0.88f), 3.5f, 10f, inspectMask);
        CreateInspectLight(rig, "FillLight",
            new Vector3(-1.5f, 0.5f, -2f),
            new Color(0.65f, 0.7f, 0.85f), 1.8f, 10f, inspectMask);
        CreateInspectLight(rig, "RimLight",
            new Vector3(0f, -0.5f, 2.5f),
            new Color(0.8f, 0.82f, 0.9f), 2f, 10f, inspectMask);

        // Description
        descTitle.text = item.itemName;
        descBody.text = item.description;
        descCanvas.gameObject.SetActive(true);
    }

    void CreateInspectLight(GameObject parent, string name, Vector3 localPos, Color color, float intensity, float range, int mask)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = localPos;
        var lt = obj.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = color;
        lt.intensity = intensity;
        lt.range = range;
        lt.cullingMask = mask;
        lt.shadows = LightShadows.None;
    }

    public void StopInspect()
    {
        if (!isInspecting) return;
        isInspecting = false;
        currentItem = null;

        // Restore camera
        mainCam.clearFlags = savedClearFlags;
        mainCam.backgroundColor = savedBgColor;
        mainCam.cullingMask = savedCullingMask;

        // Destroy rig and everything under it (item + lights)
        if (rig != null) Destroy(rig);
        rig = null;
        itemInstance = null;

        // Hide description UI
        descCanvas.gameObject.SetActive(false);

        // Back to backpack cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!isInspecting) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopInspect();
            return;
        }

        // Keep rig in front of camera
        rig.transform.position = mainCam.transform.position + mainCam.transform.forward * currentZoom;

        // Mouse drag rotates the rig — use GetAxis for frame-rate-independent delta
        if (Input.GetMouseButton(0))
        {
            float mx = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float my = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            rig.transform.Rotate(mainCam.transform.up, -mx, Space.World);
            rig.transform.Rotate(mainCam.transform.right, my, Space.World);
        }

        // Scroll to zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            currentZoom = Mathf.Clamp(currentZoom - scroll * zoomSpeed, minZoom, maxZoom);
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    // ── helpers ──

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
