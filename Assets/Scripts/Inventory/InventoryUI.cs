using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("PS2 Style Colors")]
    public Color bgColor = new Color(0.04f, 0.04f, 0.04f, 0.95f);
    public Color slotColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    public Color slotBorderColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color hoverColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    public Color textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Vector2 slotSize = new Vector2(140, 90);

    [Header("Audio")]
    public AudioClip uiSound;

    private Canvas canvas;
    private GameObject rootPanel;
    private GameObject slotContainer;
    private List<GameObject> slotObjects = new List<GameObject>();

    private FirstPersonController fpsController;
    private InspectFromInventory inspectController;
    private WeaponHolder weaponHolder;
    private AudioSource uiAudioSource;
    private bool isOpen;
    private InventoryItem equippedWeapon;
    public bool IsOpen => isOpen;

    void Awake()
    {
        Debug.Log("[InventoryUI] Awake - initializing...");

        // Unity doesn't auto-create EventSystem for code-only Canvases.
        // Without it, no clicks / raycasts work on any UI.
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        inspectController = GetComponent<InspectFromInventory>();
        if (inspectController == null)
            inspectController = gameObject.AddComponent<InspectFromInventory>();

        fpsController = FindObjectOfType<FirstPersonController>();
        Debug.Log("[InventoryUI] fpsController found: " + (fpsController != null));

        weaponHolder = FindObjectOfType<WeaponHolder>();
        Debug.Log("[InventoryUI] weaponHolder found: " + (weaponHolder != null));

        // AudioSource for UI sounds
        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.spatialBlend = 0f; // 2D UI sound

        BuildUI();
        rootPanel.SetActive(false);

        Debug.Log("[InventoryUI] Awake complete, UI built, hidden by default.");
    }

    void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Canvas
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        // Root
        rootPanel = MakePanel("Root", transform, bgColor);
        FullStretch(rootPanel.GetComponent<RectTransform>());

        // Outer border
        var border = MakePanel("Border", rootPanel.transform, Color.clear);
        DestroyImmediate(border.GetComponent<Image>());
        var bImg = border.AddComponent<RawImage>();
        bImg.color = slotBorderColor;
        var br = border.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.04f, 0.04f);
        br.anchorMax = new Vector2(0.96f, 0.96f);
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;
        var outline = border.AddComponent<Outline>();
        outline.effectColor = slotBorderColor;
        outline.effectDistance = new Vector2(2, 2);

        // Inner fill
        var innerBg = MakePanel("InnerBg", border.transform, new Color(0.06f, 0.06f, 0.06f, 1f));
        var ibRect = innerBg.GetComponent<RectTransform>();
        ibRect.anchorMin = new Vector2(0.01f, 0.01f);
        ibRect.anchorMax = new Vector2(0.99f, 0.99f);
        ibRect.offsetMin = Vector2.zero;
        ibRect.offsetMax = Vector2.zero;

        // Title
        var title = MakeLabel("Title", innerBg.transform, "ITEMS", font, 28, textColor);
        title.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var tRect = title.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 0.93f);
        tRect.anchorMax = new Vector2(0.5f, 0.93f);
        tRect.sizeDelta = new Vector2(200, 36);
        tRect.anchoredPosition = Vector2.zero;

        // Slot grid
        slotContainer = MakePanel("Slots", innerBg.transform, Color.clear);
        var sc = slotContainer.GetComponent<RectTransform>();
        sc.anchorMin = new Vector2(0.06f, 0.08f);
        sc.anchorMax = new Vector2(0.94f, 0.90f);
        sc.offsetMin = Vector2.zero;
        sc.offsetMax = Vector2.zero;
        var grid = slotContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = slotSize;
        grid.spacing = new Vector2(12, 10);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.childAlignment = TextAnchor.UpperLeft;

        // Hint
        var hint = MakeLabel("Hint", innerBg.transform,
            "[ESC] Close   [B] Toggle   Click: Equip/Inspect", font, 13,
            new Color(0.35f, 0.35f, 0.35f, 1f));
        var hRect = hint.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.5f, 0.02f);
        hRect.anchorMax = new Vector2(0.5f, 0.02f);
        hRect.sizeDelta = new Vector2(500, 22);
        hRect.anchoredPosition = Vector2.zero;
    }

    void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshSlots;
            Debug.Log("[InventoryUI] Subscribed to InventoryManager.");
        }
        else
        {
            Debug.LogWarning("[InventoryUI] InventoryManager.Instance is null!");
        }

    }

    void Update()
    {
        // Don't handle backpack input during intro or 360 inspect
        if (IntroNarrative.IsPlaying) return;
        if (inspectController != null && inspectController.IsInspecting)
            return;

        // Don't handle B key while computer UI is open
        var computerUI = FindObjectOfType<ComputerUIController>();
        if (computerUI != null && computerUI.IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("[InventoryUI] B pressed, toggling backpack. isOpen=" + isOpen);
            Toggle();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[InventoryUI] ESC pressed, closing backpack.");
            Close();
        }
    }

    void LateUpdate()
    {
        // Re-show backpack when 360 inspect ends
        if (isOpen && inspectController != null &&
            !inspectController.IsInspecting && !rootPanel.activeSelf)
        {
            Debug.Log("[InventoryUI] Inspect ended, re-showing backpack.");
            rootPanel.SetActive(true);
            RefreshSlots();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        Debug.Log("[InventoryUI] Opening backpack.");
        isOpen = true;
        rootPanel.SetActive(true);
        RefreshSlots();
        if (fpsController) fpsController.SetControlEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayUISound();
    }

    public void Close()
    {
        Debug.Log("[InventoryUI] Closing backpack.");
        isOpen = false;
        rootPanel.SetActive(false);
        if (fpsController) fpsController.SetControlEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayUISound();
    }

    void PlayUISound()
    {
        if (uiSound != null && uiAudioSource != null)
            uiAudioSource.PlayOneShot(uiSound);
    }

    void RefreshSlots()
    {
        foreach (var s in slotObjects) Destroy(s);
        slotObjects.Clear();
        if (InventoryManager.Instance == null) return;

        foreach (var item in InventoryManager.Instance.items)
            slotObjects.Add(BuildSlot(item));
    }

    GameObject BuildSlot(InventoryItem item)
    {
        var slot = MakePanel("Slot_" + item.itemName, slotContainer.transform, slotColor);

        var outline = slot.AddComponent<Outline>();
        outline.effectColor = slotBorderColor;
        outline.effectDistance = new Vector2(2, 2);

        var label = MakeLabel("Label", slot.transform, item.itemName,
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), 16, textColor);
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0.38f);
        lr.anchorMax = new Vector2(1f, 0.85f);
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        var btn = slot.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = hoverColor * 1.3f;
        btn.colors = colors;
        btn.onClick.AddListener(() => OnSlotClick(item));

        if (item.isWeapon)
        {
            // Equip status label
            string eqLabel = (equippedWeapon == item) ? "[EQUIPPED]" : "[EQUIP]";
            var eqText = MakeLabel("EquipLabel", slot.transform, eqLabel,
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), 11,
                equippedWeapon == item ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.6f, 0.6f, 0.6f));
            var er = eqText.GetComponent<RectTransform>();
            er.anchorMin = new Vector2(0f, 0.08f);
            er.anchorMax = new Vector2(0.65f, 0.35f);
            er.offsetMin = Vector2.zero;
            er.offsetMax = Vector2.zero;

            // Info button for 360 inspect
            if (item.inspectPrefab != null)
            {
                var infoBtnGo = MakePanel("InfoBtn", slot.transform, new Color(0.25f, 0.25f, 0.25f, 1f));
                var ibr = infoBtnGo.GetComponent<RectTransform>();
                ibr.anchorMin = new Vector2(0.7f, 0.08f);
                ibr.anchorMax = new Vector2(0.95f, 0.35f);
                ibr.offsetMin = Vector2.zero;
                ibr.offsetMax = Vector2.zero;
                var iBtn = infoBtnGo.AddComponent<Button>();
                iBtn.onClick.AddListener(() => InspectItem(item));
                var iLabel = MakeLabel("IText", infoBtnGo.transform, "i", null, 12, Color.white);
                FullStretch(iLabel.GetComponent<RectTransform>(), 0, 0, 0, 0);
            }
        }

        return slot;
    }

    void OnSlotClick(InventoryItem item)
    {
        if (item.isWeapon)
        {
            EquipWeapon(item);
        }
        else if (item.inspectPrefab != null)
        {
            InspectItem(item);
        }
    }

    void EquipWeapon(InventoryItem item)
    {
        if (weaponHolder == null)
        {
            Debug.LogWarning("[InventoryUI] No WeaponHolder found. Add it to the Main Camera.");
            return;
        }

        if (equippedWeapon == item)
        {
            // Unequip
            weaponHolder.UnequipWeapon();
            equippedWeapon = null;
            Debug.Log("[InventoryUI] Unequipped " + item.itemName);
        }
        else
        {
            weaponHolder.EquipWeapon(item);
            equippedWeapon = item;
            Debug.Log("[InventoryUI] Equipped " + item.itemName);
        }

        Close();
    }

    void InspectItem(InventoryItem item)
    {
        if (item.inspectPrefab == null) return;
        rootPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        inspectController.StartInspect(item);
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

    static void FullStretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(r, t);
    }
}
