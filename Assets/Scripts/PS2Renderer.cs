using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PS2Renderer : MonoBehaviour
{
    [Header("Resolution")]
    public int renderWidth = 640;
    public int renderHeight = 480;

    [Header("Post-Processing")]
    [Tooltip("Screen-space ambient occlusion on top of the low-res image.")]
    public bool enableSSAO = true;

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
    private RenderTexture rtCamera;
    private RenderTexture rtFinal;
    private Canvas canvas;
    private SSAOEffect ssao;

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

        // SSAO
        ssao = GetComponent<SSAOEffect>();
        if (ssao == null && enableSSAO)
            ssao = gameObject.AddComponent<SSAOEffect>();
        if (ssao != null)
            ssao.enabled = enableSSAO;

        StartCoroutine(PostProcessLoop());
    }

    void SetupRenderTexture()
    {
        int w = renderWidth;
        float screenAspect = (float)Screen.width / Screen.height;
        int h = Mathf.RoundToInt(w / screenAspect);

        // Intermediate RT: camera renders here at low resolution
        rtCamera = new RenderTexture(w, h, 16);
        rtCamera.filterMode = FilterMode.Point;
        rtCamera.antiAliasing = 1;
        cam.targetTexture = rtCamera;

        // Final RT: displayed on screen (after post-processing)
        rtFinal = new RenderTexture(w, h, 16);
        rtFinal.filterMode = FilterMode.Point;
        rtFinal.antiAliasing = 1;
    }

    IEnumerator PostProcessLoop()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            bool hasSSAO = ssao != null && ssao.enabled && ssao.Material != null;
            if (hasSSAO)
            {
                ssao.ApplyMaterialParameters();
                Graphics.Blit(rtCamera, rtFinal, ssao.Material, 0);
            }
            else
            {
                Graphics.Blit(rtCamera, rtFinal);
            }
        }
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
        img.texture = rtFinal;
        img.raycastTarget = false;

        var rt2 = rawObj.GetComponent<RectTransform>();
        rt2.anchorMin = Vector2.zero;
        rt2.anchorMax = Vector2.one;
        rt2.offsetMin = Vector2.zero;
        rt2.offsetMax = Vector2.zero;

        var fitter = rawObj.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)rtFinal.width / rtFinal.height;
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
        StopAllCoroutines();

        if (rtCamera != null)
        {
            rtCamera.Release();
            rtCamera = null;
        }
        if (rtFinal != null)
        {
            rtFinal.Release();
            rtFinal = null;
        }
        if (cam != null)
            cam.targetTexture = null;
    }
}
