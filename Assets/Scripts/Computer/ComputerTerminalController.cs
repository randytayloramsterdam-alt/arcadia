using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComputerTerminalController : MonoBehaviour
{
    [Header("Terminal View")]
    public ComputerTerminalView terminalView;

    [Header("Computer System")]
    public ComputerUIController computerUIController;
    public ComputerBootSequence bootSequence;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private enum TerminalLayer
    {
        Root,
        Mail,
        Diary
    }

    private TerminalLayer currentLayer = TerminalLayer.Root;
    private bool bootComplete;

    private void OnEnable()
    {
        if (bootSequence != null)
            bootSequence.OnBootComplete += OnBootComplete;
    }

    private void OnDisable()
    {
        if (bootSequence != null)
            bootSequence.OnBootComplete -= OnBootComplete;
    }

    private void Start()
    {
        if (terminalView != null && terminalView.inputField != null)
        {
            terminalView.inputField.onSubmit.RemoveListener(OnInputSubmitted);
            terminalView.inputField.onSubmit.AddListener(OnInputSubmitted);
        }

        if (terminalView != null && terminalView.sendButton != null)
        {
            terminalView.sendButton.onClick.RemoveListener(OnSendClicked);
            terminalView.sendButton.onClick.AddListener(OnSendClicked);
        }
    }

    private void OnBootComplete()
    {
        bootComplete = true;
        currentLayer = TerminalLayer.Root;
        terminalView.SetPrompt("ARCADIA:\\>");
        ShowRootMenu();
    }

    private void OnInputSubmitted(string _)
    {
        OnSendClicked();
    }

    private void OnSendClicked()
    {
        if (!bootComplete || terminalView == null)
            return;

        string rawInput = terminalView.GetInputText();
        terminalView.ClearInput();

        if (string.IsNullOrEmpty(rawInput))
        {
            terminalView.AppendPrompt();
            terminalView.FocusInput();
            return;
        }

        string displayLine = CurrentPrompt + " " + rawInput.Trim();
        terminalView.AppendLine(displayLine);

        string normalized = rawInput.Trim().ToUpperInvariant();
        string[] parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string verb = parts.Length > 0 ? parts[0] : "";
        string arg = parts.Length > 1 ? parts[1] : "";
        string normalizedCommand = string.Join(" ", parts);

        ProcessGlobalCommand(verb, arg, normalizedCommand);
    }

    private void ProcessGlobalCommand(string verb, string arg, string normalizedCommand)
    {
        switch (normalizedCommand)
        {
            case "MAIL":
            case "CD MAIL":
            case "OPEN MAIL":
                EnterMail();
                return;

            case "DIARY":
            case "CD DIARY":
            case "OPEN DIARY":
                EnterDiary();
                return;

            case "CLEAR":
            case "CLS":
                terminalView.Clear();
                terminalView.AppendPrompt();
                terminalView.FocusInput();
                return;

            case "EXIT":
            case "QUIT":
                ExitComputer();
                return;

            case "BACK":
            case "RETURN":
                HandleBack();
                return;
        }

        switch (currentLayer)
        {
            case TerminalLayer.Root:
                ProcessRootCommand(verb, arg, normalizedCommand);
                break;
            case TerminalLayer.Mail:
                ProcessMailCommand(verb, arg, normalizedCommand);
                break;
            case TerminalLayer.Diary:
                ProcessDiaryCommand(verb, arg, normalizedCommand);
                break;
        }
    }

    private void HandleBack()
    {
        switch (currentLayer)
        {
            case TerminalLayer.Root:
                terminalView.AppendPrompt();
                terminalView.FocusInput();
                break;
            case TerminalLayer.Mail:
            case TerminalLayer.Diary:
                currentLayer = TerminalLayer.Root;
                terminalView.SetPrompt("ARCADIA:\\>");
                terminalView.AppendPrompt();
                terminalView.FocusInput();
                break;
        }
    }

    private void ProcessRootCommand(string verb, string arg, string normalizedCommand)
    {
        switch (normalizedCommand)
        {
            case "DIR":
            case "LIST":
                AppendRootSystems();
                break;

            case "HELP":
                AppendRootHelp();
                break;

            default:
                BadCommand();
                break;
        }
    }

    private void ProcessMailCommand(string verb, string arg, string normalizedCommand)
    {
        switch (normalizedCommand)
        {
            case "DIR":
            case "LIST":
                AppendMailContacts();
                break;

            case "HELP":
                AppendMailHelp();
                break;

            default:
                BadCommand();
                break;
        }
    }

    private void ProcessDiaryCommand(string verb, string arg, string normalizedCommand)
    {
        switch (normalizedCommand)
        {
            case "DIR":
            case "LIST":
                AppendDiaryUnavailable();
                break;

            case "HELP":
                AppendDiaryHelp();
                break;

            default:
                BadCommand();
                break;
        }
    }

    private void AppendRootHelp()
    {
        terminalView.AppendLine("AVAILABLE COMMANDS:");
        terminalView.AppendLine("  HELP     - SHOW THIS LIST");
        terminalView.AppendLine("  DIR/LIST - SHOW AVAILABLE SYSTEMS");
        terminalView.AppendLine("  MAIL     - OPEN MAIL SYSTEM");
        terminalView.AppendLine("  DIARY    - OPEN DIARY SYSTEM");
        terminalView.AppendLine("  CLEAR    - CLEAR SCREEN");
        terminalView.AppendLine("  EXIT     - CLOSE TERMINAL");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void AppendMailHelp()
    {
        terminalView.AppendLine("MAIL COMMANDS:");
        terminalView.AppendLine("  DIR/LIST - SHOW CONTACTS");
        terminalView.AppendLine("  BACK     - RETURN TO ROOT");
        terminalView.AppendLine("  CLEAR    - CLEAR SCREEN");
        terminalView.AppendLine("  EXIT     - CLOSE TERMINAL");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void AppendDiaryHelp()
    {
        terminalView.AppendLine("DIARY COMMANDS:");
        terminalView.AppendLine("  BACK  - RETURN TO ROOT");
        terminalView.AppendLine("  CLEAR - CLEAR SCREEN");
        terminalView.AppendLine("  EXIT  - CLOSE TERMINAL");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void AppendRootSystems()
    {
        terminalView.AppendLine("");
        terminalView.AppendLine("AVAILABLE SYSTEMS:");
        terminalView.AppendLine("");
        terminalView.AppendLine("  MAIL      SYS");
        terminalView.AppendLine("  DIARY     SYS");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void AppendMailContacts()
    {
        terminalView.AppendLine("");
        terminalView.AppendLine("MAIL CONTACTS");
        terminalView.AppendLine("");
        terminalView.AppendLine("[001] A. MORRISON          READ        LAST: 1983-10-04");
        terminalView.AppendLine("[002] L. CARTER            UNREAD      LAST: 1983-10-05");
        terminalView.AppendLine("[003] E. BENSON            UNREAD      LAST: 1983-10-07");
        terminalView.AppendLine("[004] M. KELLER            READ        LAST: 1983-09-29");
        terminalView.AppendLine("[005] J. REED              READ        LAST: 1983-09-18");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void AppendDiaryUnavailable()
    {
        terminalView.AppendLine("");
        terminalView.AppendLine("DIARY SYSTEM NOT AVAILABLE.");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void ShowRootMenu()
    {
        terminalView.Clear();
        terminalView.AppendLine("ARCADIA TERMINAL READY.");
        terminalView.AppendLine("");
        terminalView.AppendLine("AVAILABLE SYSTEMS:");
        terminalView.AppendLine("");
        terminalView.AppendLine("  MAIL      SYS");
        terminalView.AppendLine("  DIARY     SYS");
        terminalView.AppendLine("");
        terminalView.AppendLine("TYPE HELP FOR COMMAND LIST.");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void EnterMail()
    {
        currentLayer = TerminalLayer.Mail;
        terminalView.SetPrompt("ARCADIA:\\MAIL>");
        terminalView.AppendLine("");
        terminalView.AppendLine("MAIL CONTACTS");
        terminalView.AppendLine("");
        terminalView.AppendLine("[001] A. MORRISON          READ        LAST: 1983-10-04");
        terminalView.AppendLine("[002] L. CARTER            UNREAD      LAST: 1983-10-05");
        terminalView.AppendLine("[003] E. BENSON            UNREAD      LAST: 1983-10-07");
        terminalView.AppendLine("[004] M. KELLER            READ        LAST: 1983-09-29");
        terminalView.AppendLine("[005] J. REED              READ        LAST: 1983-09-18");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void EnterDiary()
    {
        currentLayer = TerminalLayer.Diary;
        terminalView.SetPrompt("ARCADIA:\\DIARY>");
        terminalView.AppendLine("");
        terminalView.AppendLine("DIARY SYSTEM NOT AVAILABLE.");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void BadCommand()
    {
        terminalView.AppendLine("BAD COMMAND OR FILE NAME.");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void ExitComputer()
    {
        if (computerUIController != null)
            computerUIController.Close();
    }

    private string CurrentPrompt
    {
        get
        {
            return currentLayer switch
            {
                TerminalLayer.Root => "ARCADIA:\\>",
                TerminalLayer.Mail => "ARCADIA:\\MAIL>",
                TerminalLayer.Diary => "ARCADIA:\\DIARY>",
                _ => "ARCADIA:\\>"
            };
        }
    }
}