using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComputerChatUI : MonoBehaviour
{
    [Header("Terminal View")]
    public ComputerTerminalView terminalView;

    [Header("Messages")]
    public string processingText = "QUERYING DEEP WELL MODEL...";
    public string emptyInputWarning = "NO INPUT BUFFERED.";

    [Header("Arcadia Boot")]
    public ComputerBootSequence bootSequence;
    public bool bootOnEnable = true;

    [Header("CRT Overlay")]
    public bool enableCrtOverlay = true;
    [Range(0f, 0.5f)] public float scanlineAlpha = 0.16f;
    [Range(0f, 0.8f)] public float vignetteAlpha = 0.35f;
    [Range(0f, 0.15f)] public float flickerAmount = 0.035f;
    [Range(0f, 3f)] public float crtJitter = 0.65f;

    [Header("Visual")]
    public Color bootTextColor = new Color(0.43f, 0.66f, 1f, 1f);
    public Color screenBackgroundColor = new Color(0.005f, 0.012f, 0.014f, 1f);
    public string prompt = "ARC>";

    [Header("Backend")]
    public BackendChatClient chatClient;

    [Header("Debug")]
    public bool enableDebugLogs = false;

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
            CreateCrtOverlay();
    }

    private void OnEnable()
    {
        StopActiveRoutines();
        isProcessing = false;

        if (bootSequence != null)
        {
            bootSequence.OnBootComplete += OnBootComplete;
        }
    }

    private void OnDisable()
    {
        if (bootSequence != null)
            bootSequence.OnBootComplete -= OnBootComplete;

        StopActiveRoutines();
        isProcessing = false;
    }

    private void OnBootComplete()
    {
        bootComplete = true;
    }

    private void Start()
    {
        if (terminalView != null && terminalView.sendButton != null)
        {
            terminalView.sendButton.onClick.RemoveListener(SubmitCurrentInput);
            terminalView.sendButton.onClick.AddListener(SubmitCurrentInput);
        }

        if (terminalView != null && terminalView.inputField != null)
        {
            terminalView.inputField.onSubmit.RemoveListener(SubmitFromSubmitEvent);
            terminalView.inputField.onSubmit.AddListener(SubmitFromSubmitEvent);
        }
    }

    private void Update()
    {
        if (scanlineImage == null)
            return;

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
        if (!bootComplete || isProcessing || terminalView == null)
            return;

        string message = terminalView.GetInputText().Trim();
        terminalView.ClearInput();

        if (string.IsNullOrEmpty(message))
        {
            terminalView.AppendLine(emptyInputWarning);
            terminalView.AppendPrompt();
            terminalView.FocusInput();
            return;
        }

        terminalView.AppendLine(prompt + " " + message);

        if (TryHandleLocalCommand(message))
        {
            terminalView.AppendPrompt();
            terminalView.FocusInput();
            return;
        }

        sendRoutine = StartCoroutine(SendToBackend(message));
    }

    private IEnumerator SendToBackend(string message)
    {
        isProcessing = true;
        terminalView.EnableInput(false);

        terminalView.AppendLine("MODEM HANDSHAKE... OK");
        terminalView.AppendLine("UPLINK: ARCADIA-BACKEND / DEEP WELL NODE");
        terminalView.AppendLine(processingText);

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
            yield return null;

        terminalView.RemoveLastLineIf(processingText);

        if (string.IsNullOrWhiteSpace(reply))
            reply = "NO RESPONSE. CARRIER SIGNAL LOST.";

        yield return terminalView.TypeMultiline("DEEPWELL> " + reply);
        terminalView.AppendPrompt();

        isProcessing = false;
        terminalView.EnableInput(true);
        terminalView.FocusInput();
    }

    private bool TryHandleLocalCommand(string message)
    {
        string command = message.Trim().ToUpperInvariant();

        switch (command)
        {
            case "HELP":
                terminalView.AppendLine("LOCAL COMMANDS: HELP, CLEAR, STATUS, LOGO, WHOAMI");
                terminalView.AppendLine("REMOTE QUERY: TYPE ANY OTHER MESSAGE.");
                return true;
            case "CLEAR":
            case "CLS":
                terminalView.Clear();
                return true;
            case "STATUS":
                terminalView.AppendLine("SYSTEM: ONLINE");
                terminalView.AppendLine("DEEP WELL LINK: STABLE");
                terminalView.AppendLine("MEMETIC HYGIENE FILTER: DEGRADED");
                terminalView.AppendLine("OFFICE LEVEL: SUB-BASEMENT / NON-EUCLIDEAN EXTENSION");
                terminalView.AppendLine("EMPLOYEE COUNT: UNRESOLVED");
                return true;
            case "LOGO":
                AppendLogo();
                return true;
            case "WHOAMI":
                terminalView.AppendLine("VISITOR / TEMPORARY CREDENTIAL");
                terminalView.AppendLine("ACCESS CLASS: OBSERVATION ONLY");
                return true;
            default:
                return false;
        }
    }

    private void AppendLogo()
    {
        terminalView.AppendLine("       /\\");
        terminalView.AppendLine("      /  \\        ARCADIA");
        terminalView.AppendLine("     / /\\ \\       LIFE SCIENCES");
        terminalView.AppendLine("    /_/__\\_\\");
        terminalView.AppendLine("      ||||       VITAM EX PROFUNDIS");
    }

    private void StopActiveRoutines()
    {
        if (sendRoutine != null)
        {
            StopCoroutine(sendRoutine);
            sendRoutine = null;
        }
    }

    public void ConfigureTerminalStyle()
    {
        if (terminalView == null)
            return;

        if (terminalView.outputText != null)
        {
            terminalView.outputText.color = bootTextColor;
            terminalView.outputText.fontSize = Mathf.Max(terminalView.outputText.fontSize, 18f);
            terminalView.outputText.alignment = TextAlignmentOptions.TopLeft;
            terminalView.outputText.richText = false;
            terminalView.outputText.text = string.Empty;
        }

        if (terminalView.inputField != null)
        {
            terminalView.inputField.caretColor = bootTextColor;
            terminalView.inputField.selectionColor = new Color(bootTextColor.r, bootTextColor.g, bootTextColor.b, 0.28f);

            if (terminalView.inputField.textComponent != null)
            {
                terminalView.inputField.textComponent.color = bootTextColor;
                terminalView.inputField.textComponent.fontSize = Mathf.Max(terminalView.inputField.textComponent.fontSize, 18f);
            }

            TMP_Text placeholder = terminalView.inputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = "TYPE COMMAND...";
                placeholder.color = new Color(bootTextColor.r, bootTextColor.g, bootTextColor.b, 0.42f);
            }

            Image inputImage = terminalView.inputField.GetComponent<Image>();
            if (inputImage != null)
                inputImage.color = new Color(0.01f, 0.025f, 0.032f, 0.92f);
        }

        ConfigureButtonStyle();
        ConfigurePanelBackground();
    }

    private void ConfigureButtonStyle()
    {
        if (terminalView == null || terminalView.sendButton == null)
            return;

        ColorBlock colors = terminalView.sendButton.colors;
        colors.normalColor = new Color(0.035f, 0.09f, 0.14f, 1f);
        colors.highlightedColor = new Color(0.08f, 0.18f, 0.28f, 1f);
        colors.pressedColor = new Color(0.12f, 0.26f, 0.38f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.02f, 0.03f, 0.04f, 0.7f);
        terminalView.sendButton.colors = colors;

        TMP_Text buttonText = terminalView.sendButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = "RUN";
            buttonText.color = bootTextColor;
            buttonText.richText = false;
        }
    }

    private void ConfigurePanelBackground()
    {
        if (terminalView == null)
            return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == terminalView.outputText || graphic == scanlineImage || graphic == vignetteImage)
                continue;

            Image image = graphic as Image;
            if (image == null)
                continue;

            bool isInput = terminalView.inputField != null && image.gameObject == terminalView.inputField.gameObject;
            bool isButton = terminalView.sendButton != null && image.gameObject == terminalView.sendButton.gameObject;
            if (!isInput && !isButton)
                image.color = screenBackgroundColor;
        }
    }

    private void CreateCrtOverlay()
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null)
            return;

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
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.36f, 1f, distance));
                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}