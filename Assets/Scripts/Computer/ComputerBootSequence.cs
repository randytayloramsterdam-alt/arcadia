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
    public bool clearAfterBoot = true;
    public bool replayBootEveryOpen = false;

    [Header("Speed Control")]
    [Range(0.1f, 5f)] public float bootSpeedMultiplier = 1f;
    public float bootCharDelay = 0.035f;

    [Header("Visual")]
    public Color bootTextColor = new Color(0.43f, 0.66f, 1f, 1f);
    public Color screenBackgroundColor = new Color(0.005f, 0.012f, 0.014f, 1f);
    public Color completeScreenTextColor = new Color(0.06f, 0.19f, 0.36f, 1f);
    public Color completeScreenBackgroundColor = new Color(0.70f, 0.78f, 0.80f, 1f);
    public string systemName = "ARCADIA LIFE SCIENCES TERMINAL";

    [Header("Boot Text Lines")]
    [Tooltip("BIOS memory check screen lines")]
    public string[] memoryCheckLines = new string[]
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

    [Tooltip("Floppy read lines before progress bar")]
    public string[] floppyReadLines = new string[]
    {
        "LOAD \"ARCADIA.SYS\",8,1",
        "SEARCHING FOR DEVICE 8",
        "DRIVE 8: FOUND",
        "READING TRACK 18, SECTOR 01",
        "LOADING..."
    };

    [Tooltip("Logo drawing lines")]
    public string[] logoLines = new string[]
    {
        "        /\\",
        "       /  \\",
        "      / /\\ \\        ARCA",
        "     /_/  \\_\\       ___ ___",
        "        ||",
        "        ||"
    };

    [Tooltip("Finalizing / splash lines before SYSTEM READY")]
    public string[] finalizingLines = new string[]
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

    [Tooltip("Final SYSTEM READY screen lines")]
    public string[] completeLines = new string[]
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

    [Header("Debug")]
    public bool enableDebugLogs = false;

    [Header("Audio")]
    public ComputerTerminalAudio terminalAudio;

    public event Action OnBootComplete;

    private Coroutine bootRoutine;
    private bool bootComplete;
    private bool hasPlayedBootOnce = false;

    public bool IsBootComplete => bootComplete;

    void OnEnable()
    {
        StopActiveRoutine();

        if (skipBoot || !playBootOnEnable)
        {
            StartCoroutine(CompleteBootNextFrame());
            return;
        }

        if (!replayBootEveryOpen && hasPlayedBootOnce)
        {
            StartCoroutine(CompleteBootNextFrame());
            return;
        }

        bootComplete = false;
        terminalView.EnableInput(false);
        if (terminalAudio != null)
            terminalAudio.PlayBootPowerOn();
        bootRoutine = StartCoroutine(BootSequence());
    }

    void OnDisable()
    {
        StopActiveRoutine();
    }

    public void SkipBoot()
    {
        StopActiveRoutine();
        StartCoroutine(CompleteBootNextFrame());
    }

    private IEnumerator CompleteBootNextFrame()
    {
        yield return null;
        CompleteBoot();
    }

    private void CompleteBoot()
    {
        bootComplete = true;
        hasPlayedBootOnce = true;
        terminalView.ApplyVisualStyle();
        terminalView.EnableInput(true);
        terminalView.FocusInput();
        OnBootComplete?.Invoke();
    }

    private float ScaleBootTime(float seconds)
    {
        return seconds * Mathf.Max(0.01f, bootSpeedMultiplier);
    }

    private WaitForSecondsRealtime WaitBoot(float seconds)
    {
        return new WaitForSecondsRealtime(Mathf.Max(0.001f, ScaleBootTime(seconds)));
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

        if (clearAfterBoot)
        {
            terminalView.Clear();
        }
        else
        {
            ApplyScreenPalette(bootTextColor, screenBackgroundColor);
            AppendLogo();
            terminalView.AppendLine("");
            terminalView.AppendLine(systemName);
            terminalView.AppendLine("SECURE TERMINAL READY.");
            terminalView.AppendLine("TYPE HELP FOR LOCAL COMMANDS.");
            terminalView.AppendPrompt();
            terminalView.ApplyVisualStyle();
        }

        CompleteBoot();
    }

    private IEnumerator BootStepPowerOn(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        terminalView.Clear();

        float scaledDuration = ScaleBootTime(duration);
        float endTime = Time.unscaledTime + scaledDuration;
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
        if (memoryCheckLines != null && memoryCheckLines.Length > 0)
            yield return ShowTimedLines(memoryCheckLines, duration, bootCharDelay);
        else
            yield return WaitBoot(duration);
    }

    private IEnumerator BootStepFloppyRead(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        terminalView.Clear();

        if (floppyReadLines != null)
        {
            foreach (string line in floppyReadLines)
            {
                terminalView.AppendLine(line);
                yield return WaitBoot(0.055f);
            }
        }

        terminalView.AppendLine("[--------------------------]");
        int progressLineIndex = terminalView.TerminalLines.Count - 1;

        float scaledDuration = ScaleBootTime(duration);
        float fixedOffset = 0.28f;
        float loopDuration = scaledDuration - fixedOffset;
        float endTime = Time.unscaledTime + Mathf.Max(0.1f, loopDuration);

        int width = 26;
        int frame = 0;
        while (Time.unscaledTime < endTime)
        {
            float t = Mathf.InverseLerp(Time.unscaledTime + loopDuration - scaledDuration + fixedOffset, endTime, Time.unscaledTime);
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

        if (logoLines != null && logoLines.Length > 0)
            yield return ShowTimedLines(logoLines, duration, 0.018f);
        else
            yield return WaitBoot(duration);
    }

    private IEnumerator BootStepFinalizing(float duration)
    {
        ApplyScreenPalette(bootTextColor, screenBackgroundColor);
        terminalView.Clear();

        if (finalizingLines != null)
        {
            float scaledDelay = ScaleBootTime(0.035f);
            float totalLineDelay = finalizingLines.Length * scaledDelay;
            float scaledDuration = ScaleBootTime(duration);
            float loopDuration = scaledDuration - totalLineDelay;

            foreach (string line in finalizingLines)
            {
                terminalView.AppendLine(line);
                yield return new WaitForSecondsRealtime(scaledDelay);
            }

            float endTime = Time.unscaledTime + Mathf.Max(0.05f, loopDuration);
            int frame = 0;
            while (Time.unscaledTime < endTime)
            {
                terminalView.ReplaceLastLine(RandomGlitchBand(frame));
                frame++;
                yield return WaitBoot(0.08f);
            }
        }
        else
        {
            yield return WaitBoot(duration);
        }
    }

    private IEnumerator BootStepComplete(float duration)
    {
        ApplyScreenPalette(completeScreenTextColor, completeScreenBackgroundColor);
        terminalView.Clear();

        if (completeLines != null)
        {
            foreach (string line in completeLines)
            {
                terminalView.AppendLine(line);
            }
        }

        yield return WaitBoot(duration);
    }

    private IEnumerator ShowTimedLines(string[] lines, float duration, float charDelay)
    {
        terminalView.Clear();
        float startTime = Time.unscaledTime;

        float scaledCharDelay = ScaleBootTime(charDelay);
        float scaledDuration = ScaleBootTime(duration);

        terminalView.SetSuppressInputTick(true);
        foreach (string line in lines)
        {
            yield return terminalView.TypeLine(line, scaledCharDelay);
        }
        terminalView.SetSuppressInputTick(false);

        float elapsed = Time.unscaledTime - startTime;
        float remaining = scaledDuration - elapsed;
        if (remaining > 0)
        {
            yield return new WaitForSecondsRealtime(remaining);
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
        }

        if (terminalView != null && terminalView.backgroundImages != null)
        {
            foreach (var img in terminalView.backgroundImages)
            {
                if (img != null)
                    img.color = backgroundColor;
            }
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