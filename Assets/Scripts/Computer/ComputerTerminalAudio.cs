using System.Collections;
using UnityEngine;

public class ComputerTerminalAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource oneShotAudioSource;
    public AudioSource humAudioSource;

    [Header("Clips")]
    public AudioClip bootPowerOnClip;
    public AudioClip typeTickClip;
    public AudioClip humLoopClip;

    [Header("Volumes")]
    [Range(0f, 1f)] public float bootVolume = 0.7f;
    [Range(0f, 1f)] public float humVolume = 0.06f;

    [Header("System Type Tick Settings")]
    public bool enableSystemTypeTick = true;
    [Range(0f, 1f)] public float systemTypeVolume = 0.04f;
    public int systemTypeEveryNVisibleCharacters = 6;
    public float systemTypeMinInterval = 0.08f;
    public bool randomizeSystemTypePitch = true;
    public Vector2 systemTypePitchRange = new Vector2(0.96f, 1.04f);

    [Header("Player Input Tick Settings")]
    public bool enablePlayerInputTick = true;
    [Range(0f, 1f)] public float playerInputVolume = 0.06f;
    public float playerInputMinInterval = 0.04f;
    public bool randomizePlayerInputPitch = true;
    public Vector2 playerInputPitchRange = new Vector2(0.92f, 1.08f);

    [Header("Hum Settings")]
    public bool enableHum = true;
    public float humFadeDuration = 0.4f;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private float lastSystemTypeTickTime = -999f;
    private float lastPlayerInputTickTime = -999f;
    private int visibleCharCount = 0;
    private bool humIsPlaying = false;
    private Coroutine humFadeRoutine;

    public void PlayBootPowerOn()
    {
        if (bootPowerOnClip == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ComputerTerminalAudio] bootPowerOnClip is null");
            return;
        }
        if (oneShotAudioSource == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ComputerTerminalAudio] oneShotAudioSource is null");
            return;
        }

        oneShotAudioSource.PlayOneShot(bootPowerOnClip, bootVolume);
    }

    public void PlaySystemTypeTick()
    {
        if (!enableSystemTypeTick)
            return;
        if (typeTickClip == null)
            return;
        if (oneShotAudioSource == null)
            return;

        float now = Time.unscaledTime;
        if (now - lastSystemTypeTickTime < systemTypeMinInterval)
            return;

        lastSystemTypeTickTime = now;

        if (randomizeSystemTypePitch)
        {
            float savedPitch = oneShotAudioSource.pitch;
            oneShotAudioSource.pitch = Random.Range(systemTypePitchRange.x, systemTypePitchRange.y);
            oneShotAudioSource.PlayOneShot(typeTickClip, systemTypeVolume);
            oneShotAudioSource.pitch = savedPitch;
        }
        else
        {
            oneShotAudioSource.PlayOneShot(typeTickClip, systemTypeVolume);
        }
    }

    public void PlayInputKeyTick()
    {
        if (!enablePlayerInputTick)
            return;
        if (typeTickClip == null)
            return;
        if (oneShotAudioSource == null)
            return;

        float now = Time.unscaledTime;
        if (now - lastPlayerInputTickTime < playerInputMinInterval)
            return;

        lastPlayerInputTickTime = now;

        if (randomizePlayerInputPitch)
        {
            float savedPitch = oneShotAudioSource.pitch;
            oneShotAudioSource.pitch = Random.Range(playerInputPitchRange.x, playerInputPitchRange.y);
            oneShotAudioSource.PlayOneShot(typeTickClip, playerInputVolume);
            oneShotAudioSource.pitch = savedPitch;
        }
        else
        {
            oneShotAudioSource.PlayOneShot(typeTickClip, playerInputVolume);
        }
    }

    public void NotifyVisibleCharacter()
    {
        if (!enableSystemTypeTick)
            return;

        visibleCharCount++;
        if (visibleCharCount >= systemTypeEveryNVisibleCharacters)
        {
            visibleCharCount = 0;
            PlaySystemTypeTick();
        }
    }

    public void ResetTypeTickCounter()
    {
        visibleCharCount = 0;
    }

    public void StartHum()
    {
        if (!enableHum)
            return;
        if (humAudioSource == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ComputerTerminalAudio] humAudioSource is null");
            return;
        }
        if (humLoopClip == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ComputerTerminalAudio] humLoopClip is null");
            return;
        }
        if (humIsPlaying)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        humAudioSource.clip = humLoopClip;
        humAudioSource.loop = true;
        humAudioSource.volume = 0f;
        humAudioSource.Play();
        humIsPlaying = true;

        if (humFadeRoutine != null)
            StopCoroutine(humFadeRoutine);
        humFadeRoutine = StartCoroutine(HumFadeIn());
    }

    public void StopHum()
    {
        if (!humIsPlaying)
            return;

        if (humFadeRoutine != null)
        {
            StopCoroutine(humFadeRoutine);
            humFadeRoutine = null;
        }

        if (!gameObject.activeInHierarchy)
        {
            humIsPlaying = false;
            if (humAudioSource != null)
                humAudioSource.Stop();
            return;
        }

        humFadeRoutine = StartCoroutine(HumFadeOut());
    }

    private IEnumerator HumFadeIn()
    {
        float elapsed = 0f;
        while (elapsed < humFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / humFadeDuration);
            humAudioSource.volume = Mathf.Lerp(0f, humVolume, t);
            yield return null;
        }
        humAudioSource.volume = humVolume;
    }

    private IEnumerator HumFadeOut()
    {
        float startVol = humAudioSource.volume;
        float elapsed = 0f;
        while (elapsed < humFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / humFadeDuration);
            humAudioSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }
        humAudioSource.volume = 0f;
        humAudioSource.Stop();
        humIsPlaying = false;
    }
}