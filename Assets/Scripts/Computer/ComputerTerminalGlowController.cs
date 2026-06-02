using UnityEngine;
using TMPro;

public class ComputerTerminalGlowController : MonoBehaviour
{
    [Header("Target Texts")]
    public TMP_Text[] targetTexts;

    [Header("Glow Control")]
    public bool enableDynamicGlow = true;
    public Color glowColor = new Color32(0xB8, 0xFF, 0xFF, 0xFF);

    [Range(0f, 1f)] public float baseGlowPower = 0.18f;
    [Range(0f, 1f)] public float glowPowerAmplitude = 0.04f;
    public float glowPulseSpeed = 0.7f;

    [Range(0f, 1f)] public float baseGlowOuter = 0.12f;
    [Range(0f, 1f)] public float glowOuterAmplitude = 0.02f;
    public float glowOuterPulseSpeed = 0.45f;

    [Range(0f, 1f)] public float glowInner = 0.03f;
    [Range(0f, 1f)] public float glowOffset = 0f;

    [Header("Random Flicker")]
    public bool enableRandomFlicker = true;
    [Range(0f, 1f)] public float randomFlickerAmount = 0.015f;
    public float randomFlickerSpeed = 4f;

    [Header("Time")]
    public bool useUnscaledTime = true;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private Material[] runtimeMaterials;

    private void Start()
    {
        int count = targetTexts != null ? targetTexts.Length : 0;
        runtimeMaterials = new Material[count];

        if (targetTexts != null)
        {
            for (int i = 0; i < targetTexts.Length; i++)
            {
                TMP_Text text = targetTexts[i];
                if (text == null)
                    continue;

                Material mat = new Material(text.fontSharedMaterial);
                mat.name = text.name + "_GlowRuntimeMaterial";
                text.fontSharedMaterial = mat;
                runtimeMaterials[i] = mat;
            }
        }

        ApplyGlowSettings();
    }

    private void Update()
    {
        if (!enableDynamicGlow || runtimeMaterials == null)
            return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        float glowPower = baseGlowPower
            + Mathf.Sin(t * glowPulseSpeed) * glowPowerAmplitude;

        float glowOuter = baseGlowOuter
            + Mathf.Sin(t * glowOuterPulseSpeed + 1.37f) * glowOuterAmplitude;

        if (enableRandomFlicker)
        {
            float noise = Mathf.PerlinNoise(t * randomFlickerSpeed, 0.37f) - 0.5f;
            glowPower += noise * randomFlickerAmount;
        }

        glowPower = Mathf.Clamp01(glowPower);
        glowOuter = Mathf.Clamp01(glowOuter);

        ApplyGlowParameters(glowPower, glowOuter);
    }

    private void ApplyGlowParameters(float glowPower, float glowOuter)
    {
        foreach (var mat in runtimeMaterials)
        {
            if (mat == null)
                continue;

            if (mat.HasProperty("_GlowColor"))
                mat.SetColor("_GlowColor", glowColor);
            if (mat.HasProperty("_GlowPower"))
                mat.SetFloat("_GlowPower", glowPower);
            if (mat.HasProperty("_GlowOuter"))
                mat.SetFloat("_GlowOuter", glowOuter);
            if (mat.HasProperty("_GlowInner"))
                mat.SetFloat("_GlowInner", glowInner);
            if (mat.HasProperty("_GlowOffset"))
                mat.SetFloat("_GlowOffset", glowOffset);
        }
    }

    public void ApplyGlowSettings()
    {
        if (runtimeMaterials == null)
            return;

        ApplyGlowParameters(baseGlowPower, baseGlowOuter);
    }
}