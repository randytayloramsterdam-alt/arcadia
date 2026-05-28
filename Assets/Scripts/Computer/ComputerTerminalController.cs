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

        string input = terminalView.GetInputText().Trim();
        terminalView.ClearInput();

        if (string.IsNullOrEmpty(input))
        {
            terminalView.AppendPrompt();
            terminalView.FocusInput();
            return;
        }

        terminalView.AppendLine(CurrentPrompt + " " + input);
        ProcessCommand(input);
    }

    private void ProcessCommand(string input)
    {
        string raw = input.Trim();
        string command = raw.ToUpperInvariant();
        string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string verb = parts.Length > 0 ? parts[0] : "";

        switch (currentLayer)
        {
            case TerminalLayer.Root:
                ProcessRootCommand(verb, parts, raw);
                break;
            case TerminalLayer.Mail:
                ProcessMailCommand(verb, parts, raw);
                break;
            case TerminalLayer.Diary:
                ProcessDiaryCommand(verb, parts, raw);
                break;
        }
    }

    private void ProcessRootCommand(string verb, string[] parts, string raw)
    {
        switch (verb)
        {
            case "HELP":
                terminalView.AppendLine("AVAILABLE COMMANDS:");
                terminalView.AppendLine("  HELP     - SHOW THIS LIST");
                terminalView.AppendLine("  DIR/LIST - SHOW AVAILABLE SYSTEMS");
                terminalView.AppendLine("  MAIL     - OPEN MAIL SYSTEM");
                terminalView.AppendLine("  DIARY    - OPEN DIARY SYSTEM");
                terminalView.AppendLine("  CLEAR    - CLEAR SCREEN");
                terminalView.AppendLine("  EXIT     - CLOSE TERMINAL");
                terminalView.AppendPrompt();
                break;

            case "DIR":
            case "LIST":
                ShowRootMenu();
                break;

            case "MAIL":
            case "CD MAIL":
            case "OPEN MAIL":
                EnterMail();
                break;

            case "DIARY":
            case "CD DIARY":
            case "OPEN DIARY":
                EnterDiary();
                break;

            case "CLEAR":
            case "CLS":
                terminalView.Clear();
                terminalView.AppendPrompt();
                terminalView.FocusInput();
                break;

            case "EXIT":
            case "QUIT":
                ExitComputer();
                break;

            case "BACK":
            case "RETURN":
                terminalView.AppendLine("ROOT DIRECTORY");
                terminalView.AppendPrompt();
                terminalView.FocusInput();
                break;

            default:
                BadCommand();
                break;
        }
    }

    private void ProcessMailCommand(string verb, string[] parts, string raw)
    {
        switch (verb)
        {
            case "HELP":
                terminalView.AppendLine("MAIL COMMANDS:");
                terminalView.AppendLine("  DIR/LIST - SHOW CONTACTS");
                terminalView.AppendLine("  BACK     - RETURN TO ROOT");
                terminalView.AppendLine("  CLEAR    - CLEAR SCREEN");
                terminalView.AppendLine("  EXIT     - CLOSE TERMINAL");
                terminalView.AppendPrompt();
                break;

            case "DIR":
            case "LIST":
                ShowMailContactList();
                break;

            case "BACK":
            case "RETURN":
                EnterRoot();
                break;

            case "CLEAR":
            case "CLS":
                terminalView.Clear();
                terminalView.AppendPrompt();
                terminalView.FocusInput();
                break;

            case "EXIT":
            case "QUIT":
                ExitComputer();
                break;

            default:
                BadCommand();
                break;
        }
    }

    private void ProcessDiaryCommand(string verb, string[] parts, string raw)
    {
        switch (verb)
        {
            case "HELP":
                terminalView.AppendLine("DIARY COMMANDS:");
                terminalView.AppendLine("  BACK  - RETURN TO ROOT");
                terminalView.AppendLine("  CLEAR - CLEAR SCREEN");
                terminalView.AppendLine("  EXIT  - CLOSE TERMINAL");
                terminalView.AppendPrompt();
                break;

            case "BACK":
            case "RETURN":
                EnterRoot();
                break;

            case "CLEAR":
            case "CLS":
                terminalView.Clear();
                terminalView.AppendPrompt();
                terminalView.FocusInput();
                break;

            case "EXIT":
            case "QUIT":
                ExitComputer();
                break;

            default:
                BadCommand();
                break;
        }
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

    private void ShowMailContactList()
    {
        terminalView.Clear();
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

    private void EnterMail()
    {
        currentLayer = TerminalLayer.Mail;
        terminalView.SetPrompt("ARCADIA:\\MAIL>");
        ShowMailContactList();
    }

    private void EnterDiary()
    {
        currentLayer = TerminalLayer.Diary;
        terminalView.SetPrompt("ARCADIA:\\DIARY>");
        terminalView.Clear();
        terminalView.AppendLine("DIARY SYSTEM NOT AVAILABLE.");
        terminalView.AppendPrompt();
        terminalView.FocusInput();
    }

    private void EnterRoot()
    {
        currentLayer = TerminalLayer.Root;
        terminalView.SetPrompt("ARCADIA:\\>");
        ShowRootMenu();
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