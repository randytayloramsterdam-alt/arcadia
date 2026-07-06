using System;
using System.Collections;
using UnityEngine;

public class ComputerUIFadeController : MonoBehaviour
{
    [Header("Target")]
    public CanvasGroup targetCanvasGroup;

    [Header("Fade Settings")]
    public float fadeInDuration = 0.25f;
    public float fadeOutDuration = 0.18f;
    public bool useUnscaledTime = true;

    [Header("Initial State")]
    public bool startHidden = true;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private Coroutine fadeRoutine;

    public bool IsFading { get; private set; }

    private void Awake()
    {
        if (targetCanvasGroup == null)
            targetCanvasGroup = GetComponent<CanvasGroup>();

        if (targetCanvasGroup == null && startHidden)
            targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (startHidden)
        {
            targetCanvasGroup.alpha = 0f;
            targetCanvasGroup.interactable = false;
            targetCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            targetCanvasGroup.alpha = 1f;
            targetCanvasGroup.interactable = true;
            targetCanvasGroup.blocksRaycasts = true;
        }
    }

    public void ShowInstant()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        IsFading = false;
        targetCanvasGroup.alpha = 1f;
        targetCanvasGroup.interactable = true;
        targetCanvasGroup.blocksRaycasts = true;
    }

    public void HideInstant()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        IsFading = false;
        targetCanvasGroup.alpha = 0f;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = false;
    }

    public void FadeIn(Action onComplete = null)
    {
        if (fadeInDuration <= 0f)
        {
            ShowInstant();
            onComplete?.Invoke();
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeInCoroutine(onComplete));
    }

    private IEnumerator FadeInCoroutine(Action onComplete)
    {
        IsFading = true;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = false;
        targetCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        float duration = fadeInDuration;
        if (duration <= 0f) duration = 0.01f;

        while (elapsed < duration)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += delta;
            targetCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        targetCanvasGroup.alpha = 1f;
        targetCanvasGroup.interactable = true;
        targetCanvasGroup.blocksRaycasts = true;
        IsFading = false;
        fadeRoutine = null;
        onComplete?.Invoke();
    }

    public void FadeOut(Action onComplete = null)
    {
        if (fadeOutDuration <= 0f)
        {
            HideInstant();
            onComplete?.Invoke();
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeOutCoroutine(onComplete));
    }

    private IEnumerator FadeOutCoroutine(Action onComplete)
    {
        IsFading = true;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float duration = fadeOutDuration;
        if (duration <= 0f) duration = 0.01f;

        while (elapsed < duration)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += delta;
            targetCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        targetCanvasGroup.alpha = 0f;
        targetCanvasGroup.interactable = false;
        targetCanvasGroup.blocksRaycasts = false;
        IsFading = false;
        fadeRoutine = null;
        onComplete?.Invoke();
    }
}