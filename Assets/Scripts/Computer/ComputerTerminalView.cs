using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComputerTerminalView : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text outputText;
    public TMP_InputField inputField;
    public Button sendButton;
    public RectTransform contentRect;
    public RectTransform outputTextRect;
    public TMP_Text liveInputLineText;

    [Header("Scroll Settings")]
    public ScrollRect terminalScrollRect;
    public bool autoScrollToBottom = true;
    public bool smoothAutoScroll = true;
    public bool forceInstantScrollSameFrame = true;
    [Range(1f, 60f)] public float autoScrollSpeed = 12f;
    [Range(1f, 120f)] public float mouseWheelScrollSensitivity = 40f;

    [Header("Inline Input Settings")]
    public bool useInlineInputLine = true;
    public string cursorSymbol = "█";
    public bool showBlinkingCursor = true;
    [Range(0.1f, 2f)] public float cursorBlinkInterval = 0.5f;

    [Header("Input Field Visuals")]
    public bool hideInputFieldVisuals = true;

    [Header("Terminal Settings")]
    public string currentPrompt = "ARCADIA:\\>";
    [Range(10, 200)] public int maxTerminalLines = 80;
    [Range(0.001f, 0.1f)] public float typeCharDelay = 0.012f;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private readonly List<string> terminalLines = new List<string>();
    private Coroutine scrollRoutine;
    private bool cursorVisible = true;
    private float cursorBlinkTimer;
    private string cachedPrompt = "";
    private string cachedInput = "";

    public IReadOnlyList<string> TerminalLines => terminalLines;

    private void Awake()
    {
        ApplyScrollSettings();
        if (hideInputFieldVisuals)
            ApplyHiddenInputFieldVisuals();
    }

    private void OnEnable()
    {
        if (hideInputFieldVisuals)
            ApplyHiddenInputFieldVisuals();
    }

    private void Update()
    {
        if (showBlinkingCursor && useInlineInputLine && liveInputLineText != null)
        {
            cursorBlinkTimer += Time.unscaledDeltaTime;
            if (cursorBlinkTimer >= cursorBlinkInterval)
            {
                cursorBlinkTimer -= cursorBlinkInterval;
                cursorVisible = !cursorVisible;
                RefreshLiveInputLine();
            }
        }
    }

    private void OnValidate()
    {
        ApplyScrollSettings();
    }

    private void ApplyScrollSettings()
    {
        if (terminalScrollRect != null)
            terminalScrollRect.scrollSensitivity = mouseWheelScrollSensitivity;
    }

    public void ApplyHiddenInputFieldVisuals()
    {
        if (inputField == null)
            return;

        Image backgroundImage = inputField.GetComponent<Image>();
        if (backgroundImage != null)
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);

        if (inputField.textComponent != null)
        {
            Color tc = inputField.textComponent.color;
            inputField.textComponent.color = new Color(tc.r, tc.g, tc.b, 0f);
        }

        TMP_Text placeholder = inputField.placeholder as TMP_Text;
        if (placeholder != null)
        {
            Color ph = placeholder.color;
            placeholder.color = new Color(ph.r, ph.g, ph.b, 0f);
        }
    }

    public void SetLiveInputVisible(bool visible)
    {
        if (liveInputLineText != null)
            liveInputLineText.gameObject.SetActive(visible);
    }

    public void UpdateLiveInputLine(string prompt, string input)
    {
        if (!useInlineInputLine || liveInputLineText == null)
            return;

        cachedPrompt = prompt;
        cachedInput = input;

        RefreshLiveInputLine();
    }

    private void RefreshLiveInputLine()
    {
        if (liveInputLineText == null || !useInlineInputLine)
            return;

        string display = cachedPrompt + " " + cachedInput;
        bool showCursor = showBlinkingCursor ? cursorVisible : true;
        if (showCursor)
            display += cursorSymbol;

        liveInputLineText.text = display;
    }

    public void ClearLiveInputLine()
    {
        if (liveInputLineText != null)
            liveInputLineText.text = "";
    }

    public void Clear()
    {
        terminalLines.Clear();
        Refresh();
    }

    public void AppendLine(string line)
    {
        terminalLines.Add(line ?? string.Empty);
        TrimLines();
        Refresh();
    }

    public void AppendLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
            terminalLines.Add(line ?? string.Empty);
        TrimLines();
        Refresh();
    }

    public void SetPrompt(string prompt)
    {
        currentPrompt = prompt;
    }

    public void AppendPrompt()
    {
        AppendLine(currentPrompt);
    }

    public void Refresh()
    {
        if (outputText == null)
            return;
        outputText.text = string.Join(Environment.NewLine, terminalLines);

        if (!autoScrollToBottom || terminalScrollRect == null)
            return;

        if (smoothAutoScroll)
        {
            ScrollToBottomNextFrame();
        }
        else if (forceInstantScrollSameFrame)
        {
            ForceScrollToBottomImmediate();
        }
        else
        {
            ScrollToBottomNextFrame();
        }
    }

    private void ScrollToBottomNextFrame()
    {
        if (terminalScrollRect == null)
            return;

        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }

        scrollRoutine = StartCoroutine(SmoothScrollRoutine());
    }

    private void ForceScrollToBottomImmediate()
    {
        if (terminalScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (outputTextRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(outputTextRect);
        if (contentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Canvas.ForceUpdateCanvases();

        terminalScrollRect.verticalNormalizedPosition = 0f;
        terminalScrollRect.velocity = Vector2.zero;
    }

    private IEnumerator SmoothScrollRoutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (!smoothAutoScroll)
        {
            terminalScrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            float threshold = 0.001f;
            while (terminalScrollRect.verticalNormalizedPosition > threshold)
            {
                float current = terminalScrollRect.verticalNormalizedPosition;
                terminalScrollRect.verticalNormalizedPosition = Mathf.Lerp(current, 0f, Time.unscaledDeltaTime * autoScrollSpeed);
                yield return null;
            }
            terminalScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ReplaceLastLine(string line)
    {
        if (terminalLines.Count == 0)
        {
            terminalLines.Add(line);
        }
        else
        {
            terminalLines[terminalLines.Count - 1] = line;
        }
        Refresh();
    }

    public void ReplaceLine(int index, string line)
    {
        if (index < 0 || index >= terminalLines.Count)
        {
            ReplaceLastLine(line);
            return;
        }
        terminalLines[index] = line;
        Refresh();
    }

    public void RemoveLastLineIf(string line)
    {
        if (terminalLines.Count == 0)
            return;
        if (terminalLines[terminalLines.Count - 1] == line)
        {
            terminalLines.RemoveAt(terminalLines.Count - 1);
            Refresh();
        }
    }

    public void EnableInput(bool enabled)
    {
        if (inputField != null)
            inputField.interactable = enabled;
        if (sendButton != null)
            sendButton.interactable = enabled;

        if (useInlineInputLine && liveInputLineText != null)
            liveInputLineText.gameObject.SetActive(enabled);
    }

    public void FocusInput()
    {
        if (inputField == null || !inputField.interactable)
            return;
        inputField.ActivateInputField();
        int len = inputField.text.Length;
        inputField.caretPosition = len;
        inputField.selectionAnchorPosition = len;
        inputField.selectionFocusPosition = len;
    }

    public void ClearInput()
    {
        if (inputField != null)
            inputField.text = string.Empty;
    }

    public string GetInputText()
    {
        return inputField != null ? inputField.text : string.Empty;
    }

    public IEnumerator TypeLine(string line)
    {
        yield return TypeLine(line, typeCharDelay);
    }

    public IEnumerator TypeLine(string line, float charDelay)
    {
        if (string.IsNullOrEmpty(line))
        {
            AppendLine("");
            yield return new WaitForSecondsRealtime(charDelay);
            yield break;
        }

        terminalLines.Add("");
        TrimLines();

        for (int i = 0; i < line.Length; i++)
        {
            terminalLines[terminalLines.Count - 1] += line[i];
            Refresh();
            yield return new WaitForSecondsRealtime(charDelay);
        }
    }

    public IEnumerator TypeLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            yield return TypeLine(line);
        }
    }

    public IEnumerator TypeMultiline(string text)
    {
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n');
        foreach (var line in lines)
            yield return TypeLine(line);
    }

    private void TrimLines()
    {
        int overflow = terminalLines.Count - maxTerminalLines;
        if (overflow > 0)
            terminalLines.RemoveRange(0, overflow);
    }
}