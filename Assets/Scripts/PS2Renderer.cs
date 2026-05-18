using UnityEngine;
using UnityEngine.UI;

public class PS2Renderer : MonoBehaviour
{
    [Header("Resolution")]
    public int renderWidth = 640;
    public int renderHeight = 480;

    [Header("Lighting")]
    [Tooltip("Disable the main directional light (sun) so the scene relies on placed indoor lights only.")]
    public bool disableDirectionalLight = true;

    [Tooltip("Ambient fill color — keep it very dark so unlit corners aren't pitch black.")]
    public Color ambientColor = new Color(0.04f, 0.04f, 0.05f);

    [Tooltip("Ambient intensity multiplier.")]
    [Range(0f, 1f)] public float ambientIntensity = 0.15f;

    [Tooltip("Reflection intensity for metallic surfaces. Low but non-zero keeps the PS2 vibe.")]
    [Range(0f, 1f)] public float reflectionIntensity = 0.3f;

    [Header("Fog")]
    public bool disableFog = true;

    private Camera cam;
    private RenderTexture rt;
    private Canvas canvas;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) { enabled = false; return; }

        SetupRenderTexture();
        SetupDisplayCanvas();
        SetupDisplayCamera();
        ApplyLightingSettings();
        ApplyQualitySettings();
    }

    void SetupRenderTexture()
    {
        int w = renderWidth;
        float screenAspect = (float)Screen.width / Screen.height;
        int h = Mathf.RoundToInt(w / screenAspect);

        rt = new RenderTexture(w, h, 16);
        rt.filterMode = FilterMode.Point;
        rt.antiAliasing = 1;
        cam.targetTexture = rt;
    }

    void SetupDisplayCanvas()
    {
        var canvasObj = new GameObject("PS2Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1000;

        var rawObj = new GameObject("Display");
        rawObj.transform.SetParent(canvasObj.transform, false);
        var img = rawObj.AddComponent<RawImage>();
        img.texture = rt;
        img.raycastTarget = false;

        var rt2 = rawObj.GetComponent<RectTransform>();
        rt2.anchorMin = Vector2.zero;
        rt2.anchorMax = Vector2.one;
        rt2.offsetMin = Vector2.zero;
        rt2.offsetMax = Vector2.zero;

        var fitter = rawObj.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)rt.width / rt.height;
    }

    void SetupDisplayCamera()
    {
        var go = new GameObject("DisplayCamera");
        var displayCam = go.AddComponent<Camera>();
        displayCam.cullingMask = 0;
        displayCam.depth = -2;
        displayCam.clearFlags = CameraClearFlags.Nothing;
        displayCam.targetTexture = null;
        DontDestroyOnLoad(go);
    }

    void ApplyLightingSettings()
    {
        // Kill world/environment light — only placed indoor lights illuminate
        if (disableDirectionalLight)
        {
            foreach (Light l in FindObjectsOfType<Light>())
            {
                if (l.type == LightType.Directional)
                    l.enabled = false;
            }
        }

        // Dark but not pitch-black ambient so corners are still faintly visible
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;

        // Keep reflections alive for metallic surfaces (fan etc.)
        RenderSettings.reflectionIntensity = reflectionIntensity;

        if (disableFog)
            RenderSettings.fog = false;
    }

    void ApplyQualitySettings()
    {
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.vSyncCount = 0;
    }

    void OnDestroy()
    {
        if (rt != null)
        {
            rt.Release();
            rt = null;
        }
        if (cam != null)
            cam.targetTexture = null;
    }
}
