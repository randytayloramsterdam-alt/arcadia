using UnityEngine;

[DefaultExecutionOrder(-50)]
public class OfficeAudioAmbience : MonoBehaviour
{
    [Header("Targets")]
    public FirstPersonController controller;

    [Header("Ambience")]
    [Range(0f, 1f)] public float fluorescentHumVolume = 0.22f;
    [Range(0f, 1f)] public float buildingRumbleVolume = 0.13f;

    [Header("Carpet Footsteps")]
    [Range(0f, 1f)] public float carpetStepVolume = 0.42f;
    public float carpetStepDistance = 1.35f;

    AudioClip carpetStepClip;
    AudioClip fluorescentHumClip;
    AudioClip buildingRumbleClip;

    void Awake()
    {
        carpetStepClip = CreateCarpetStepClip();
        fluorescentHumClip = CreateFluorescentHumClip();
        buildingRumbleClip = CreateBuildingRumbleClip();
        AssignFootsteps();
    }

    void Start()
    {
        AssignFootsteps();
        CreateLoopSource("Arcadia fluorescent room tone", fluorescentHumClip, fluorescentHumVolume, 1.0f);
        CreateLoopSource("Arcadia distant HVAC rumble", buildingRumbleClip, buildingRumbleVolume, 0.92f);
    }

    void AssignFootsteps()
    {
        if (controller == null)
        {
            controller = GetComponent<FirstPersonController>();
        }

        if (controller == null)
        {
            return;
        }

        controller.walkClip = carpetStepClip;
        controller.stepDistance = carpetStepDistance;
        controller.stepVolume = carpetStepVolume;
    }

    void CreateLoopSource(string sourceName, AudioClip clip, float volume, float pitch)
    {
        if (clip == null || volume <= 0f)
        {
            return;
        }

        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();
    }

    static AudioClip CreateCarpetStepClip()
    {
        const int sampleRate = 22050;
        const float length = 0.72f;
        int sampleCount = Mathf.RoundToInt(sampleRate * length);
        float[] samples = new float[sampleCount];
        uint state = 0x1234567u;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 14f) * Mathf.Clamp01(t * 35f);
            float lowThump = Mathf.Sin(2f * Mathf.PI * 82f * t) * 0.22f * env;
            float cloth = (NextNoise(ref state) * 0.22f + NextNoise(ref state) * 0.08f) * env;
            float heel = Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t * 28f) * 0.08f;
            samples[i] = Mathf.Clamp((lowThump + cloth + heel) * 0.75f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("procedural soft carpet step", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip CreateFluorescentHumClip()
    {
        const int sampleRate = 22050;
        const float length = 8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * length);
        float[] samples = new float[sampleCount];
        uint state = 0x91ac410bu;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float mains = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.12f;
            float ballast = Mathf.Sin(2f * Mathf.PI * 118f * t + Mathf.Sin(t * 0.7f) * 0.4f) * 0.06f;
            float whine = Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.012f;
            float dust = NextNoise(ref state) * 0.015f;
            samples[i] = mains + ballast + whine + dust;
        }

        AudioClip clip = AudioClip.Create("procedural fluorescent hum", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip CreateBuildingRumbleClip()
    {
        const int sampleRate = 22050;
        const float length = 11f;
        int sampleCount = Mathf.RoundToInt(sampleRate * length);
        float[] samples = new float[sampleCount];
        uint state = 0x78231u;
        float smoothedNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            smoothedNoise = Mathf.Lerp(smoothedNoise, NextNoise(ref state), 0.0025f);
            float sub = Mathf.Sin(2f * Mathf.PI * 31f * t) * 0.08f;
            float vent = Mathf.Sin(2f * Mathf.PI * 47f * t + smoothedNoise * 0.8f) * 0.05f;
            samples[i] = sub + vent + smoothedNoise * 0.04f;
        }

        AudioClip clip = AudioClip.Create("procedural distant building rumble", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static float NextNoise(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return ((state & 0xffff) / 32767.5f) - 1f;
    }
}
