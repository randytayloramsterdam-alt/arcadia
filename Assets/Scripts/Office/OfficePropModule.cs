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
        Debug.Log($"[OfficePropModule] Initialized: moduleIndex={index}, position={transform.position}");
    }

    public void SetVisible(bool visible)
    {
        foreach (var obj in childObjects)
            obj.SetActive(visible);
    }

    public void SetLightsActive(bool active)
    {
        // 第一次调用时强制执行，不跳过
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
            Debug.LogWarning($"[OfficePropModule] moduleIndex={moduleIndex} has no lighting references configured (lightsRoot and moduleLights are null/empty).");
        }
        else
        {
            Debug.Log($"[OfficePropModule] moduleIndex={moduleIndex} lights {(active ? "ON" : "OFF")}");
        }
    }
}