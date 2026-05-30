using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComputerBootSequence : MonoBehaviour
{
    [Header("Terminal View")]
    public ComputerTerminalView terminalView;

    [Header("Boot Settings")]
    public bool playBootOnEnable = true;
    public bool skipBoot = false;
    public float bootDuration = 5f;
    [Range(0.01f, 0.12f)] public float bootLineDelay = 0.045f;

    [Header("Visual")]
    public Color bootTextColor = new Color(0.43f, 0.66f, 1f, 1f);
    public Color screenBackgroundColor = new Color(0.005f, 0.012f, 0.014f, 1f);
    public Color completeScreenTextColor = new Color(0.06f, 0.19f, 0.36f, 1f);
    public Color completeScreenBackgroundColor = new Color(0.70f, 0.78f, 0.80f, 1f);
    public string systemName = "ARCADIA LIFE SCIENCES TERMINAL";

    [Header("Debug")]
    public bool enableDebugLogs = false;

    public event Action OnBootComplete;

    private Coroutine bootRoutine;
    private bool bootComplete;

    public bool IsBootComplete => bootComplete;

    void OnEnable()
    {
        StopActiveRoutine();

        if (skipBoot || !playBootOnEnable)
        {
            bootComplete = true;
            terminalView.EnableInput(true);
            terminalView.FocusInput();
            OnBootComplete?.Invoke();
            return;
        }

        bootComplete = false;
        terminalView.EnableInput(false);
        bootRoutine = StartCoroutine(BootSequence());
    }

    void OnDisable()
    {
        StopActiveRoutine();
    }

    public void SkipBoot()
    {
        StopActiveRoutine();
        bootComplete = true;
        terminalView.EnableInput(true);
        terminalView.FocusInput();
        OnBootComplete?.Invoke();
    }

    private IEnumerator BootSequence()
    {
        float stepDuration = Mathf.Max(0.35f, bootDuration / 5f);

        yield return BootStepPowerOn(stepDuration);
        yield return BootStepMemoryCheck(stepDuration);
        yield return BootStepFloppyRead(stepDuration);
        yield return BootStepDrawingLogo(stepDuration);
        yield return BootStepFinalizing(stepDuration);
        yield return BootStepComplete(stepDuration);

        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        terminalView.Clear();
        AppendLogo();
        terminalView.AppendLine("");
        terminalView.AppendLine(systemName);
        terminalView.AppendLine("SECURE TERMINAL READY.");
        terminalView.AppendLine("TYPE HELP FOR LOCAL COMMANDS.");
        terminalView.AppendPrompt();

        bootComplete = true;
        terminalView.EnableInput(true);
        terminalView.FocusInput();
        OnBootComplete?.Invoke();
    }

    private IEnumerator BootStepPowerOn(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        terminalView.Clear();

        float endTime = Time.unscaledTime + duration;
        bool visible = true;
        while (Time.unscaledTime < endTime)
        {
            terminalView.Clear();
            terminalView.AppendLine("");
            terminalView.AppendLine("");
            terminalView.AppendLine("    " + (visible ? "█" : ""));
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
            "█"
        };

        yield return ShowTimedLines(lines, duration, 0.035f);
    }

    private IEnumerator BootStepFloppyRead(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        terminalView.Clear();

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
            terminalView.AppendLine(line);
            yield return WaitBoot(0.055f);
        }

        terminalView.AppendLine("[--------------------------]");
        int progressLineIndex = terminalView.TerminalLines.Count - 1;
        float endTime = Time.unscaledTime + Mathf.Max(0.1f, duration - 0.28f);
        int width = 26;
        int frame = 0;
        while (Time.unscaledTime < endTime)
        {
            float t = Mathf.InverseLerp(endTime - duration + 0.28f, endTime, Time.unscaledTime);
            int filledCount = Mathf.Clamp(Mathf.RoundToInt(t * width), 0, width);
            string filled = new string('#', filledCount);
            string empty = new string('-', width - filledCount);
            terminalView.ReplaceLine(progressLineIndex, "[" + filled + empty + "]");

            if (frame % 3 == 0)
            {
                terminalView.AppendLine(RandomGlitchBand(frame));
                terminalView.Refresh();
            }

            frame++;
            yield return WaitBoot(0.055f);
        }
    }

    private IEnumerator BootStepDrawingLogo(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        terminalView.Clear();

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
        terminalView.Clear();

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
            terminalView.AppendLine(line);
            yield return WaitBoot(0.035f);
        }

        float endTime = Time.unscaledTime + Mathf.Max(0.05f, duration - lines.Length * 0.035f);
        int frame = 0;
        while (Time.unscaledTime < endTime)
        {
            terminalView.ReplaceLastLine(RandomGlitchBand(frame));
            frame++;
            yield return WaitBoot(0.08f);
        }
    }

    private IEnumerator BootStepComplete(float duration)
    {
        ApplyScreenPalette(completeScreenTextColor, completeScreenBackgroundColor);
        terminalView.Clear();

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
            terminalView.AppendLine(line);
        }

        yield return WaitBoot(duration);
    }

    private IEnumerator ShowTimedLines(string[] lines, float duration, float charDelay)
    {
        terminalView.Clear();
        float startTime = Time.unscaledTime;

        foreach (string line in lines)
        {
            yield return terminalView.TypeLine(line, charDelay);
        }

        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < duration)
        {
            yield return WaitBoot(duration - elapsed);
        }
    }

    private WaitForSecondsRealtime WaitBoot(float seconds)
    {
        return new WaitForSecondsRealtime(Mathf.Max(0.001f, seconds));
    }

    private void AppendLogo()
    {
        terminalView.AppendLine("       /\\");
        terminalView.AppendLine("      /  \\        ARCADIA");
        terminalView.AppendLine("     / /\\ \\       LIFE SCIENCES");
        terminalView.AppendLine("    /_/__\\_\\");
        terminalView.AppendLine("      ||||       VITAM EX PROFUNDIS");
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
        if (terminalView != null)
        {
            if (terminalView.outputText != null)
                terminalView.outputText.color = textColor;

            if (terminalView.inputField != null)
            {
                terminalView.inputField.caretColor = textColor;
                terminalView.inputField.selectionColor = new Color(textColor.r, textColor.g, textColor.b, 0.28f);

                if (terminalView.inputField.textComponent != null)
                    terminalView.inputField.textComponent.color = textColor;

                TMP_Text placeholder = terminalView.inputField.placeholder as TMP_Text;
                if (placeholder != null)
                    placeholder.color = new Color(textColor.r, textColor.g, textColor.b, 0.42f);
            }
        }

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (terminalView != null && (graphic == terminalView.outputText || graphic.gameObject == terminalView.inputField?.gameObject || graphic.gameObject == terminalView.sendButton?.gameObject))
                continue;

            Image image = graphic as Image;
            if (image != null)
                image.color = backgroundColor;
        }
    }

    private void StopActiveRoutine()
    {
        if (bootRoutine != null)
        {
            StopCoroutine(bootRoutine);
            bootRoutine = null;
        }
    }
}