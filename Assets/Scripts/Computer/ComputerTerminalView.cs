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

    [Header("Terminal Settings")]
    public string currentPrompt = "ARCADIA:\\>";
    [Range(10, 200)] public int maxTerminalLines = 80;
    [Range(0.001f, 0.1f)] public float typeCharDelay = 0.012f;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private readonly List<string> terminalLines = new List<string>();

    public IReadOnlyList<string> TerminalLines => terminalLines;

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
    }

    public void FocusInput()
    {
        if (inputField == null || !inputField.interactable)
            return;
        inputField.ActivateInputField();
        inputField.Select();
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
