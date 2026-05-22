using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComputerChatUI : MonoBehaviour
{
    [Header("Existing Chat Wiring")]
    public TMP_InputField chatInputField;
    public TMP_Text chatOutputText;
    public Button sendButton;
    public BackendChatClient chatClient;

    [Header("Messages")]
    public string processingText = "QUERYING DEEP WELL MODEL...";
    public string emptyInputWarning = "NO INPUT BUFFERED.";

    [Header("Arcadia Boot")]
    public bool bootOnEnable = true;
    [Range(2f, 8f)] public float bootDuration = 5f;
    [Range(0.01f, 0.12f)] public float bootLineDelay = 0.045f;
    public Color bootTextColor = new Color(0.43f, 0.66f, 1f, 1f);
    public Color screenBackgroundColor = new Color(0.005f, 0.012f, 0.014f, 1f);
    public Color completeScreenTextColor = new Color(0.06f, 0.19f, 0.36f, 1f);
    public Color completeScreenBackgroundColor = new Color(0.70f, 0.78f, 0.80f, 1f);
    public string prompt = "ARC>";
    public string systemName = "ARCADIA LIFE SCIENCES TERMINAL";

    [Header("CRT Overlay")]
    public bool enableCrtOverlay = true;
    [Range(0f, 0.5f)] public float scanlineAlpha = 0.16f;
    [Range(0f, 0.8f)] public float vignetteAlpha = 0.35f;
    [Range(0f, 0.15f)] public float flickerAmount = 0.035f;
    [Range(0f, 3f)] public float crtJitter = 0.65f;
    [Range(20, 180)] public int maxTerminalLines = 80;

    private readonly List<string> terminalLines = new List<string>();
    private Coroutine bootRoutine;
    private Coroutine sendRoutine;
    private RawImage scanlineImage;
    private Image vignetteImage;
    private RectTransform overlayRect;
    private bool bootComplete;
    private bool isProcessing;

    private void Awake()
    {
        ConfigureTerminalStyle();
        if (enableCrtOverlay)
        {
            CreateCrtOverlay();
        }
    }

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SubmitCurrentInput);
            sendButton.onClick.AddListener(SubmitCurrentInput);
        }

        if (chatInputField != null)
        {
            chatInputField.onSubmit.RemoveListener(SubmitFromSubmitEvent);
            chatInputField.onSubmit.AddListener(SubmitFromSubmitEvent);
        }
    }

    private void OnEnable()
    {
        StopActiveRoutines();
        isProcessing = false;

        if (bootOnEnable)
        {
            bootRoutine = StartCoroutine(BootSequence());
        }
        else
        {
            bootComplete = true;
            EnableInput(true);
            ClearTerminal();
            AppendLine(systemName);
            AppendLine("SECURE TERMINAL READY.");
            AppendPrompt();
        }
    }

    private void OnDisable()
    {
        StopActiveRoutines();
        isProcessing = false;
    }

    private void Update()
    {
        if (scanlineImage == null)
        {
            return;
        }

        float flicker = Mathf.Sin(Time.unscaledTime * 38.7f) * flickerAmount;
        scanlineImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(scanlineAlpha + flicker));

        if (overlayRect != null && crtJitter > 0f)
        {
            float x = (Mathf.PerlinNoise(Time.unscaledTime * 18.3f, 0.13f) - 0.5f) * crtJitter;
            float y = (Mathf.PerlinNoise(0.71f, Time.unscaledTime * 24.1f) - 0.5f) * crtJitter;
            overlayRect.anchoredPosition = new Vector2(x, y);
        }
    }

    private void SubmitFromSubmitEvent(string _)
    {
        SubmitCurrentInput();
    }

    private void SubmitCurrentInput()
    {
        if (!bootComplete || isProcessing || chatInputField == null)
        {
            return;
        }

        string message = chatInputField.text.Trim();
        chatInputField.text = string.Empty;

        if (string.IsNullOrEmpty(message))
        {
            AppendLine(emptyInputWarning);
            AppendPrompt();
            FocusInput();
            return;
        }

        AppendLine(prompt + " " + message);

        if (TryHandleLocalCommand(message))
        {
            AppendPrompt();
            FocusInput();
            return;
        }

        sendRoutine = StartCoroutine(SendToBackend(message));
    }

    private IEnumerator SendToBackend(string message)
    {
        isProcessing = true;
        EnableInput(false);

        AppendLine("MODEM HANDSHAKE... OK");
        AppendLine("UPLINK: ARCADIA-BACKEND / DEEP WELL NODE");
        AppendLine(processingText);

        string reply = null;
        bool done = false;

        if (chatClient == null)
        {
            reply = "BACKEND CLIENT NOT INSTALLED.";
            done = true;
        }
        else
        {
            yield return StartCoroutine(chatClient.SendMessage(
                message,
                response =>
                {
                    reply = response;
                    done = true;
                },
                error =>
                {
                    reply = "ERROR: " + error;
                    done = true;
                }));
        }

        while (!done)
        {
            yield return null;
        }

        RemoveLastLineIf(processingText);

        if (string.IsNullOrWhiteSpace(reply))
        {
            reply = "NO RESPONSE. CARRIER SIGNAL LOST.";
        }

        yield return TypeMultiline("DEEPWELL> " + reply);
        AppendPrompt();

        isProcessing = false;
        EnableInput(true);
        FocusInput();
    }

    private bool TryHandleLocalCommand(string message)
    {
        string command = message.Trim().ToUpperInvariant();

        switch (command)
        {
            case "HELP":
                AppendLine("LOCAL COMMANDS: HELP, CLEAR, STATUS, LOGO, WHOAMI");
                AppendLine("REMOTE QUERY: TYPE ANY OTHER MESSAGE.");
                return true;
            case "CLEAR":
            case "CLS":
                ClearTerminal();
                return true;
            case "STATUS":
                AppendLine("SYSTEM: ONLINE");
                AppendLine("DEEP WELL LINK: STABLE");
                AppendLine("MEMETIC HYGIENE FILTER: DEGRADED");
                AppendLine("OFFICE LEVEL: SUB-BASEMENT / NON-EUCLIDEAN EXTENSION");
                AppendLine("EMPLOYEE COUNT: UNRESOLVED");
                return true;
            case "LOGO":
                AppendLogo();
                return true;
            case "WHOAMI":
                AppendLine("VISITOR / TEMPORARY CREDENTIAL");
                AppendLine("ACCESS CLASS: OBSERVATION ONLY");
                return true;
            default:
                return false;
        }
    }

    private IEnumerator BootSequence()
    {
        bootComplete = false;
        EnableInput(false);
        ClearTerminal();

        float stepDuration = Mathf.Max(0.35f, bootDuration / 5f);

        yield return BootStepPowerOn(stepDuration);
        yield return BootStepMemoryCheck(stepDuration);
        yield return BootStepFloppyRead(stepDuration);
        yield return BootStepDrawingLogo(stepDuration);
        yield return BootStepFinalizing(stepDuration);
        yield return BootStepComplete(stepDuration);

        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        ClearTerminal();
        AppendLogo();
        AppendLine("");
        AppendLine(systemName);
        AppendLine("SECURE TERMINAL READY.");
        AppendLine("TYPE HELP FOR LOCAL COMMANDS.");
        AppendPrompt();

        bootComplete = true;
        EnableInput(true);
        FocusInput();
    }

    private IEnumerator BootStepPowerOn(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        ClearTerminal();

        float endTime = Time.unscaledTime + duration;
        bool visible = true;
        while (Time.unscaledTime < endTime)
        {
            terminalLines.Clear();
            terminalLines.Add("");
            terminalLines.Add("");
            terminalLines.Add("    " + (visible ? "\u2588" : ""));
            RefreshTerminalText();
            visible = !visible;
            yield return WaitBoot(0.16f);
        }
    }

    private IEnumerator BootStepMemoryCheck(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        string[] lines =
        {
            "*** ARCADIA BIOS v1.0 ***",
            "64K RAM SYSTEM",
            "38911 BASIC BYTES FREE",
            "",
            "MEMORY CHECK...",
            "RAM TEST: $0000 - $FFFF  OK",
            "BASIC ROM: OK",
            "KERNAL ROM: OK",
            "",
            "READY.",
            "\u2588"
        };

        yield return ShowTimedLines(lines, duration, 0.035f);
    }

    private IEnumerator BootStepFloppyRead(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        ClearTerminal();

        string[] lines =
        {
            "LOAD \"ARCADIA.SYS\",8,1",
            "SEARCHING FOR DEVICE 8",
            "DRIVE 8: FOUND",
            "READING TRACK 18, SECTOR 01",
            "LOADING..."
        };

        foreach (string line in lines)
        {
            AppendLine(line);
            yield return WaitBoot(0.055f);
        }

        AppendLine("[--------------------------]");
        int progressLineIndex = terminalLines.Count - 1;
        float endTime = Time.unscaledTime + Mathf.Max(0.1f, duration - 0.28f);
        int width = 26;
        int frame = 0;
        while (Time.unscaledTime < endTime)
        {
            float t = Mathf.InverseLerp(endTime - duration + 0.28f, endTime, Time.unscaledTime);
            int filledCount = Mathf.Clamp(Mathf.RoundToInt(t * width), 0, width);
            string filled = new string('#', filledCount);
            string empty = new string('-', width - filledCount);
            ReplaceLine(progressLineIndex, "[" + filled + empty + "]");

            if (frame % 3 == 0)
            {
                AppendLine(RandomGlitchBand(frame));
                TrimTerminalLines();
            }

            frame++;
            yield return WaitBoot(0.055f);
        }
    }

    private IEnumerator BootStepDrawingLogo(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        ClearTerminal();

        string[] lines =
        {
            "        /\\",
            "       /  \\",
            "      / /\\ \\        ARCA",
            "     /_/  \\_\\       ___ ___",
            "        ||",
            "        ||"
        };

        yield return ShowTimedLines(lines, duration, 0.018f);
    }

    private IEnumerator BootStepFinalizing(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        ClearTerminal();

        string[] lines =
        {
            "        /\\",
            "       /  \\        ARCADIA",
            "      / /\\ \\       LIFE SCIENCES",
            "     /_/__\\_\\",
            "        ||",
            "        ||        \"VITAM EX PROFUNDIS\"",
            "                 (LIFE FROM THE DEPTHS)",
            "",
            "====--__--====__---___--====",
            "____----____--__----_____---"
        };

        foreach (string line in lines)
        {
            AppendLine(line);
            yield return WaitBoot(0.035f);
        }

        float endTime = Time.unscaledTime + Mathf.Max(0.05f, duration - lines.Length * 0.035f);
        int frame = 0;
        while (Time.unscaledTime < endTime)
        {
            ReplaceOrAppendLastLine(RandomGlitchBand(frame));
            frame++;
            yield return WaitBoot(0.08f);
        }
    }

    private IEnumerator BootStepComplete(float duration)
    {
        ApplyScreenPalette(completeScreenTextColor, completeScreenBackgroundColor);
        ClearTerminal();

        string[] lines =
        {
            "",
            "        /\\",
            "       /  \\        ARCADIA",
            "      / /\\ \\       LIFE SCIENCES",
            "     /_/__\\_\\",
            "        ||",
            "        ||",
            "",
            "\"VITAM EX PROFUNDIS\"",
            "(LIFE FROM THE DEPTHS)",
            "",
            "SYSTEM READY."
        };

        foreach (string line in lines)
        {
            AppendLine(line);
        }

        yield return WaitBoot(duration);
    }

    private IEnumerator ShowTimedLines(string[] lines, float duration, float charDelay)
    {
        ClearTerminal();
        float startTime = Time.unscaledTime;

        foreach (string line in lines)
        {
            yield return TypeLine(line, charDelay);
        }

        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < duration)
        {
            yield return WaitBoot(duration - elapsed);
        }
    }

    private IEnumerator DrawProgressBar(int width, float duration)
    {
        int total = Mathf.Max(4, width);
        for (int i = 0; i <= total; i++)
        {
            string filled = new string('#', i);
            string empty = new string('-', total - i);
            ReplaceOrAppendLastLine("[" + filled + empty + "]");
            yield return new WaitForSecondsRealtime(duration / total);
        }
    }

    private IEnumerator TypeLine(string line)
    {
        yield return TypeLine(line, bootLineDelay);
    }

    private IEnumerator TypeLine(string line, float charDelay)
    {
        if (string.IsNullOrEmpty(line))
        {
            AppendLine("");
            yield return WaitBoot(charDelay);
            yield break;
        }

        terminalLines.Add("");
        TrimTerminalLines();

        for (int i = 0; i < line.Length; i++)
        {
            terminalLines[terminalLines.Count - 1] += line[i];
            RefreshTerminalText();
            yield return WaitBoot(charDelay);
        }
    }

    private IEnumerator TypeMultiline(string text)
    {
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n');

        foreach (string line in lines)
        {
            yield return TypeLine(line, 0.012f);
        }
    }

    private WaitForSecondsRealtime WaitBoot(float seconds)
    {
        return new WaitForSecondsRealtime(Mathf.Max(0.001f, seconds));
    }

    private void AppendLogo()
    {
        AppendLine("       /\\");
        AppendLine("      /  \\        ARCADIA");
        AppendLine("     / /\\ \\       LIFE SCIENCES");
        AppendLine("    /_/__\\_\\");
        AppendLine("      ||||       VITAM EX PROFUNDIS");
    }

    private void AppendPrompt()
    {
        AppendLine(prompt);
    }

    private void AppendLine(string line)
    {
        terminalLines.Add(line ?? string.Empty);
        TrimTerminalLines();
        RefreshTerminalText();
    }

    private void ReplaceOrAppendLastLine(string line)
    {
        if (terminalLines.Count == 0)
        {
            terminalLines.Add(line);
        }
        else
        {
            terminalLines[terminalLines.Count - 1] = line;
        }

        RefreshTerminalText();
    }

    private void ReplaceLine(int index, string line)
    {
        if (index < 0 || index >= terminalLines.Count)
        {
            ReplaceOrAppendLastLine(line);
            return;
        }

        terminalLines[index] = line;
        RefreshTerminalText();
    }

    private void RemoveLastLineIf(string line)
    {
        if (terminalLines.Count == 0)
        {
            return;
        }

        if (terminalLines[terminalLines.Count - 1] == line)
        {
            terminalLines.RemoveAt(terminalLines.Count - 1);
            RefreshTerminalText();
        }
    }

    private void ClearTerminal()
    {
        terminalLines.Clear();
        RefreshTerminalText();
    }

    private void TrimTerminalLines()
    {
        int overflow = terminalLines.Count - maxTerminalLines;
        if (overflow > 0)
        {
            terminalLines.RemoveRange(0, overflow);
        }
    }

    private void RefreshTerminalText()
    {
        if (chatOutputText == null)
        {
            return;
        }

        chatOutputText.text = string.Join(Environment.NewLine, terminalLines);
    }

    private string RandomGlitchBand(int frame)
    {
        string[] bands =
        {
            "~~~~~___~~~~________~~~~~~___",
            "____----____--__----_____---",
            "====--__--====__---___--====",
            "----_____----~~~~~_____-----"
        };

        return bands[Mathf.Abs(frame) % bands.Length];
    }

    private void ApplyScreenPalette(Color textColor, Color backgroundColor)
    {
        if (chatOutputText != null)
        {
            chatOutputText.color = textColor;
        }

        if (chatInputField != null)
        {
            chatInputField.caretColor = textColor;
            chatInputField.selectionColor = new Color(textColor.r, textColor.g, textColor.b, 0.28f);

            if (chatInputField.textComponent != null)
            {
                chatInputField.textComponent.color = textColor;
            }

            TMP_Text placeholder = chatInputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.color = new Color(textColor.r, textColor.g, textColor.b, 0.42f);
            }
        }

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == chatOutputText || graphic == scanlineImage || graphic == vignetteImage)
            {
                continue;
            }

            Image image = graphic as Image;
            if (image == null)
            {
                continue;
            }

            bool isInput = chatInputField != null && image.gameObject == chatInputField.gameObject;
            bool isButton = sendButton != null && image.gameObject == sendButton.gameObject;
            if (!isInput && !isButton)
            {
                image.color = backgroundColor;
            }
        }
    }

    private void EnableInput(bool enabled)
    {
        if (chatInputField != null)
        {
            chatInputField.interactable = enabled;
        }

        if (sendButton != null)
        {
            sendButton.interactable = enabled;
        }
    }

    private void FocusInput()
    {
        if (chatInputField == null || !chatInputField.interactable)
        {
            return;
        }

        chatInputField.ActivateInputField();
        chatInputField.Select();
    }

    private void StopActiveRoutines()
    {
        if (bootRoutine != null)
        {
            StopCoroutine(bootRoutine);
            bootRoutine = null;
        }

        if (sendRoutine != null)
        {
            StopCoroutine(sendRoutine);
            sendRoutine = null;
        }
    }

    private void ConfigureTerminalStyle()
    {
        if (chatOutputText != null)
        {
            chatOutputText.color = bootTextColor;
            chatOutputText.fontSize = Mathf.Max(chatOutputText.fontSize, 18f);
            chatOutputText.alignment = TextAlignmentOptions.TopLeft;
            chatOutputText.richText = false;
            chatOutputText.text = string.Empty;
        }

        if (chatInputField != null)
        {
            chatInputField.caretColor = bootTextColor;
            chatInputField.selectionColor = new Color(bootTextColor.r, bootTextColor.g, bootTextColor.b, 0.28f);

            if (chatInputField.textComponent != null)
            {
                chatInputField.textComponent.color = bootTextColor;
                chatInputField.textComponent.fontSize = Mathf.Max(chatInputField.textComponent.fontSize, 18f);
            }

            TMP_Text placeholder = chatInputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = "TYPE COMMAND...";
                placeholder.color = new Color(bootTextColor.r, bootTextColor.g, bootTextColor.b, 0.42f);
            }

            Image inputImage = chatInputField.GetComponent<Image>();
            if (inputImage != null)
            {
                inputImage.color = new Color(0.01f, 0.025f, 0.032f, 0.92f);
            }
        }

        ConfigureButtonStyle();
        ConfigurePanelBackground();
    }

    private void ConfigureButtonStyle()
    {
        if (sendButton == null)
        {
            return;
        }

        ColorBlock colors = sendButton.colors;
        colors.normalColor = new Color(0.035f, 0.09f, 0.14f, 1f);
        colors.highlightedColor = new Color(0.08f, 0.18f, 0.28f, 1f);
        colors.pressedColor = new Color(0.12f, 0.26f, 0.38f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.02f, 0.03f, 0.04f, 0.7f);
        sendButton.colors = colors;

        TMP_Text buttonText = sendButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = "RUN";
            buttonText.color = bootTextColor;
            buttonText.richText = false;
        }
    }

    private void ConfigurePanelBackground()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == chatOutputText || graphic == scanlineImage || graphic == vignetteImage)
            {
                continue;
            }

            Image image = graphic as Image;
            if (image == null)
            {
                continue;
            }

            bool isInput = chatInputField != null && image.gameObject == chatInputField.gameObject;
            bool isButton = sendButton != null && image.gameObject == sendButton.gameObject;
            if (!isInput && !isButton)
            {
                image.color = screenBackgroundColor;
            }
        }
    }

    private void CreateCrtOverlay()
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Transform existing = transform.Find("Generated CRT Overlay");
        if (existing != null)
        {
            overlayRect = existing as RectTransform;
            scanlineImage = existing.GetComponent<RawImage>();
            return;
        }

        GameObject overlay = new GameObject("Generated CRT Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(RawImage));
        overlay.transform.SetParent(transform, false);
        overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.SetAsLastSibling();

        CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        scanlineImage = overlay.GetComponent<RawImage>();
        scanlineImage.texture = CreateScanlineTexture();
        scanlineImage.uvRect = new Rect(0f, 0f, 1f, 80f);
        scanlineImage.color = new Color(1f, 1f, 1f, scanlineAlpha);
        scanlineImage.raycastTarget = false;

        GameObject vignette = new GameObject("Generated CRT Vignette", typeof(RectTransform), typeof(Image));
        vignette.transform.SetParent(overlay.transform, false);
        RectTransform vignetteRect = vignette.GetComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;

        vignetteImage = vignette.GetComponent<Image>();
        vignetteImage.sprite = CreateVignetteSprite();
        vignetteImage.color = new Color(1f, 1f, 1f, vignetteAlpha);
        vignetteImage.raycastTarget = false;
    }

    private Texture2D CreateScanlineTexture()
    {
        Texture2D texture = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
        texture.SetPixel(0, 1, new Color(0f, 0f, 0f, 0.12f));
        texture.SetPixel(0, 2, new Color(0.1f, 0.24f, 0.34f, 0.08f));
        texture.SetPixel(0, 3, new Color(0f, 0f, 0f, 0.2f));
        texture.Apply();
        return texture;
    }

    private Sprite CreateVignetteSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDistance = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.36f, 1f, distance));
                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
