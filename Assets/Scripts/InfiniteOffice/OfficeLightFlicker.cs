using UnityEngine;

[RequireComponent(typeof(Light))]
public class OfficeLightFlicker : MonoBehaviour
{
    public float baseIntensity = 0.55f;
    public float minIntensity = 0.18f;
    public float noiseSpeed = 7f;
    public float pulseSpeed = 19f;
    public float seedOffset;

    Light targetLight;

    void Awake()
    {
        targetLight = GetComponent<Light>();
    }

    void Update()
    {
        float time = Time.time + seedOffset;
        float noise = Mathf.PerlinNoise(seedOffset, time * noiseSpeed);
        float pulse = Mathf.Sin(time * pulseSpeed) * 0.5f + 0.5f;
        float occasionalDrop = noise > 0.86f ? Mathf.Lerp(1f, 0.18f, pulse) : 1f;
        targetLight.intensity = Mathf.Lerp(minIntensity, baseIntensity, noise) * occasionalDrop;
    }
}
