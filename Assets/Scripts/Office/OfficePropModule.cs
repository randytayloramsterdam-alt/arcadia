using UnityEngine;

public class OfficePropModule : MonoBehaviour
{
    public int moduleIndex { get; private set; }

    [Header("Lights")]
    [Tooltip("灯光根物体")]
    public GameObject lightsRoot;

    [Tooltip("模块内的灯光组件列表")]
    public Light[] moduleLights;

    private bool lightsActive;
    private bool hasInitializedLightState;

    [Header("Visibility")]
    [Tooltip("物品内容根物体（用于隐藏/显示整个模块内容）")]
    public GameObject contentRoot;

    private bool contentVisible;
    private bool hasInitializedVisibilityState;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private GameObject[] childObjects;

    void Awake()
    {
        childObjects = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            childObjects[i] = transform.GetChild(i).gameObject;
    }

    public void Initialize(int index)
    {
        moduleIndex = index;
        if (enableDebugLogs)
            Debug.Log($"[OfficePropModule] Initialized: moduleIndex={index}, position={transform.position}");
    }

    public void SetVisible(bool visible)
    {
        foreach (var obj in childObjects)
            obj.SetActive(visible);
    }

    public void SetLightsActive(bool active)
    {
        if (hasInitializedLightState && lightsActive == active)
            return;

        hasInitializedLightState = true;
        lightsActive = active;

        bool hasLights = false;

        if (lightsRoot != null)
        {
            lightsRoot.SetActive(active);
            hasLights = true;
        }

        if (moduleLights != null && moduleLights.Length > 0)
        {
            foreach (var light in moduleLights)
            {
                if (light != null)
                    light.enabled = active;
            }
            hasLights = true;
        }

        if (!hasLights)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[OfficePropModule] moduleIndex={moduleIndex} has no lighting references configured (lightsRoot and moduleLights are null/empty).");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[OfficePropModule] moduleIndex={moduleIndex} lights {(active ? "ON" : "OFF")}");
        }
    }

    public void SetContentVisible(bool visible)
    {
        if (hasInitializedVisibilityState && contentVisible == visible)
            return;

        hasInitializedVisibilityState = true;
        contentVisible = visible;

        if (contentRoot != null)
        {
            contentRoot.SetActive(visible);
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[OfficePropModule] moduleIndex={moduleIndex} has no contentRoot configured (contentRoot is null).");
        }

        if (enableDebugLogs)
            Debug.Log($"[OfficePropModule] moduleIndex={moduleIndex} content {(visible ? "VISIBLE" : "HIDDEN")}");
    }
}