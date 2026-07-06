using UnityEngine;
using UnityEngine.UI;

public class ComputerScanlineEffectController : MonoBehaviour
{
    [Header("Target")]
    public RawImage scanlineRawImage;

    [Header("Scroll")]
    public bool enableScroll = true;
    public Vector2 uvScrollSpeed = new Vector2(0f, 0.035f);

    [Header("Alpha Pulse")]
    public bool enableAlphaPulse = true;
    [Range(0f, 1f)] public float baseAlpha = 0.12f;
    [Range(0f, 1f)] public float alphaAmplitude = 0.025f;
    public float alphaPulseSpeed = 1.1f;

    [Header("Random Flicker")]
    public bool enableRandomFlicker = true;
    [Range(0f, 1f)] public float randomFlickerAmount = 0.012f;
    public float randomFlickerSpeed = 4f;

    [Header("Time")]
    public bool useUnscaledTime = true;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private Rect initialUvRect;
    private Color initialColor;
    private bool initialized;

    private void Awake()
    {
        if (scanlineRawImage == null)
            scanlineRawImage = GetComponent<RawImage>();

        if (scanlineRawImage == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ComputerScanlineEffectController] No RawImage assigned and none found on this GameObject.");
            return;
        }

        initialUvRect = scanlineRawImage.uvRect;
        initialColor = scanlineRawImage.color;
        scanlineRawImage.raycastTarget = false;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || scanlineRawImage == null)
            return;

        if (!scanlineRawImage.enabled)
            return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (enableScroll)
        {
            Rect rect = scanlineRawImage.uvRect;
            rect.x = Mathf.Repeat(rect.x + uvScrollSpeed.x * dt, 1f);
            rect.y = Mathf.Repeat(rect.y + uvScrollSpeed.y * dt, 1f);
            scanlineRawImage.uvRect = rect;
        }

        float alpha = baseAlpha;

        if (enableAlphaPulse)
        {
            alpha += Mathf.Sin(t * alphaPulseSpeed) * alphaAmplitude;
        }

        if (enableRandomFlicker)
        {
            float noise = Mathf.PerlinNoise(t * randomFlickerSpeed, 0.51f) - 0.5f;
            alpha += noise * randomFlickerAmount;
        }

        alpha = Mathf.Clamp01(alpha);

        Color c = scanlineRawImage.color;
        c.a = alpha;
        scanlineRawImage.color = c;
    }

    public void SetEffectEnabled(bool enabled)
    {
        if (scanlineRawImage == null)
            return;

        scanlineRawImage.enabled = enabled;
    }

    public void ResetEffect()
    {
        if (!initialized || scanlineRawImage == null)
            return;

        scanlineRawImage.uvRect = initialUvRect;
        Color c = scanlineRawImage.color;
        c.a = baseAlpha;
        scanlineRawImage.color = c;
    }
}