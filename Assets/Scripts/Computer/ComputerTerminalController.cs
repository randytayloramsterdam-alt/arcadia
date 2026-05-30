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
    public ComputerMailSystem mailSystem;

    [Header("Focus Settings")]
    public bool keepInputFocused = true;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private enum TerminalLayer
    {
        Root,
        Mail,
        Diary,
        MailContact,
        MailMessage
    }

    private TerminalLayer currentLayer = TerminalLayer.Root;
    private bool bootComplete;
    private string currentContactId = "";
    private string currentMessageId = "";

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

    private void Update()
    {
        if (!keepInputFocused || computerUIController == null || !computerUIController.IsOpen)
            return;
        if (terminalView == null || terminalView.inputField == null)
            return;
        if (!terminalView.inputField.interactable)
            return;

        GameObject selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        if (selected != terminalView.inputField.gameObject)
            terminalView.FocusInput();
    }

    private void Start()
    {
        if (mailSystem != null)
            mailSystem.Initialize();

        if (terminalView != null && terminalView.inputField != null)
        {
            terminalView.inputField.onSubmit.RemoveListener(OnInputSubmitted);
            terminalView.inputField.onSubmit.AddListener(OnInputSubmitted);

            terminalView.inputField.onValueChanged.RemoveListener(OnInputValueChanged);
            terminalView.inputField.onValueChanged.AddListener(OnInputValueChanged);
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
        currentContactId = "";
        currentMessageId = "";
        terminalView.SetPrompt("ARCADIA:\\>");
        ShowRootMenu();
    }

    private void OnInputValueChanged(string value)
    {
        if (!bootComplete || terminalView == null)
            return;
        terminalView.UpdateLiveInputLine(CurrentPrompt, value);
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
        terminalView.ClearLiveInputLine();

        if (string.IsNullOrEmpty(rawInput))
        {
            terminalView.UpdateLiveInputLine(CurrentPrompt, "");
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

        string sendBody = "";
        if (normalizedCommand.StartsWith("SEND "))
            sendBody = rawInput.Substring(5).Trim();

        ProcessGlobalCommand(verb, arg, normalizedCommand, sendBody);
    }

    private void ProcessGlobalCommand(string verb, string arg, string normalizedCommand, string sendBody = "")
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
                terminalView.UpdateLiveInputLine(CurrentPrompt, "");
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
            case TerminalLayer.MailContact:
                ProcessMailContactCommand(verb, arg, normalizedCommand, sendBody);
                break;
            case TerminalLayer.MailMessage:
                ProcessMailMessageCommand(verb, arg, normalizedCommand);
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
                terminalView.UpdateLiveInputLine(terminalView.currentPrompt, "");
                terminalView.FocusInput();
                break;

            case TerminalLayer.Mail:
            case TerminalLayer.Diary:
                currentLayer = TerminalLayer.Root;
                currentContactId = "";
                currentMessageId = "";
                terminalView.SetPrompt("ARCADIA:\\>");
                terminalView.UpdateLiveInputLine("ARCADIA:\\>", "");
                terminalView.FocusInput();
                break;

            case TerminalLayer.MailContact:
                EnterMail();
                break;

            case TerminalLayer.MailMessage:
                EnterMailContact(currentContactId);
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
                AppendMailContactList();
                break;

            case "HELP":
                AppendMailHelp();
                break;

            default:
                if (mailSystem != null && mailSystem.GetContact(normalizedCommand) != null)
                {
                    EnterMailContact(normalizedCommand);
                }
                else if (mailSystem != null && mailSystem.GetContact(arg) != null)
                {
                    EnterMailContact(arg);
                }
                else
                {
                    BadCommand();
                }
                break;
        }
    }

    private void ProcessMailContactCommand(string verb, string arg, string normalizedCommand, string sendBody = "")
    {
        if (verb == "SEND")
        {
            if (string.IsNullOrWhiteSpace(sendBody))
            {
                terminalView.AppendLine("EMPTY MESSAGE BUFFER.");
                terminalView.UpdateLiveInputLine(CurrentPrompt, "");
                terminalView.FocusInput();
                return;
            }
            HandleSend(sendBody);
            return;
        }

        if (normalizedCommand.StartsWith("SEND "))
        {
            HandleSend(sendBody);
            return;
        }

        switch (normalizedCommand)
        {
            case "DIR":
            case "LIST":
                AppendMessageList();
                break;

            case "HELP":
                AppendMailContactHelp();
                break;

            default:
                if (IsMessageId(verb))
                {
                    EnterMailMessage(currentContactId, verb);
                }
                else if (IsMessageId(arg))
                {
                    EnterMailMessage(currentContactId, arg);
                }
                else
                {
                    BadCommand();
                }
                break;
        }
    }

    private void ProcessMailMessageCommand(string verb, string arg, string normalizedCommand)
    {
        switch (normalizedCommand)
        {
            case "DIR":
            case "LIST":
                AppendMessageBody();
                break;

            case "HELP":
                AppendMailMessageHelp();
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

    private bool IsMessageId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        var contact = mailSystem != null ? mailSystem.GetContact(currentContactId) : null;
        if (contact == null)
            return false;
        foreach (var msg in contact.messages)
        {
            if (msg.id == id)
                return true;
        }
        return false;
    }

    private void HandleSend(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            terminalView.AppendLine("EMPTY MESSAGE BUFFER.");
            terminalView.UpdateLiveInputLine(CurrentPrompt, "");
            terminalView.FocusInput();
            return;
        }

        if (mailSystem == null || string.IsNullOrEmpty(currentContactId))
        {
            BadCommand();
            return;
        }

        var msg = mailSystem.AddSentMessage(currentContactId, body);
        string contactName = mailSystem.GetContactName(currentContactId);

        terminalView.AppendLine("OUTGOING MESSAGE CREATED.");
        terminalView.AppendLine("");
        terminalView.AppendLine($"TO      : {contactName}");
        terminalView.AppendLine($"FROM    : LOCAL USER");
        terminalView.AppendLine($"DATE    : {mailSystem.currentMailDate}");
        terminalView.AppendLine($"SUBJECT : {msg.subject}");
        terminalView.AppendLine("");
        terminalView.AppendLine("TRANSMISSION STATUS: SENT");
        terminalView.AppendLine("");

        AppendMessageList();
    }

    private void EnterMail()
    {
        currentLayer = TerminalLayer.Mail;
        currentContactId = "";
        currentMessageId = "";
        terminalView.SetPrompt("ARCADIA:\\MAIL>");
        AppendMailContactList();
    }

    private void EnterDiary()
    {
        currentLayer = TerminalLayer.Diary;
        currentContactId = "";
        currentMessageId = "";
        terminalView.SetPrompt("ARCADIA:\\DIARY>");
        AppendDiaryUnavailable();
    }

    private void EnterMailContact(string contactId)
    {
        if (mailSystem == null || mailSystem.GetContact(contactId) == null)
        {
            BadCommand();
            return;
        }

        currentLayer = TerminalLayer.MailContact;
        currentContactId = contactId;
        currentMessageId = "";
        string contactName = mailSystem.GetContactName(contactId);
        terminalView.SetPrompt($"ARCADIA:\\MAIL\\{contactName}>");
        AppendMessageList();
    }

    private void EnterMailMessage(string contactId, string messageId)
    {
        if (mailSystem == null || mailSystem.GetMessage(contactId, messageId) == null)
        {
            BadCommand();
            return;
        }

        currentLayer = TerminalLayer.MailMessage;
        currentMessageId = messageId;
        string contactName = mailSystem.GetContactName(contactId);
        terminalView.SetPrompt($"ARCADIA:\\MAIL\\{contactName}\\{messageId}>");
        AppendMessageBody();

        mailSystem.MarkMessageRead(contactId, messageId);
    }

    private void AppendMailContactList()
    {
        if (mailSystem == null)
        {
            terminalView.AppendLine("MAIL SYSTEM NOT AVAILABLE.");
            terminalView.UpdateLiveInputLine(CurrentPrompt, "");
            terminalView.FocusInput();
            return;
        }

        string output = mailSystem.RenderContactList();
        string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
            terminalView.AppendLine(line);

        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendMessageList()
    {
        if (mailSystem == null || string.IsNullOrEmpty(currentContactId))
        {
            BadCommand();
            return;
        }

        string output = mailSystem.RenderMessageList(currentContactId);
        string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
            terminalView.AppendLine(line);

        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendMessageBody()
    {
        if (mailSystem == null || string.IsNullOrEmpty(currentContactId) || string.IsNullOrEmpty(currentMessageId))
        {
            BadCommand();
            return;
        }

        string output = mailSystem.RenderMessageBody(currentContactId, currentMessageId);
        string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
            terminalView.AppendLine(line);

        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
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
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendMailHelp()
    {
        terminalView.AppendLine("MAIL COMMANDS:");
        terminalView.AppendLine("  DIR/LIST   - SHOW CONTACTS");
        terminalView.AppendLine("  [ID]       - OPEN CONTACT");
        terminalView.AppendLine("  BACK       - RETURN TO ROOT");
        terminalView.AppendLine("  CLEAR      - CLEAR SCREEN");
        terminalView.AppendLine("  EXIT       - CLOSE TERMINAL");
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendMailContactHelp()
    {
        terminalView.AppendLine("CONTACT COMMANDS:");
        terminalView.AppendLine("  DIR/LIST    - SHOW MESSAGES");
        terminalView.AppendLine("  [MSG ID]    - OPEN MESSAGE");
        terminalView.AppendLine("  BACK        - RETURN TO MAIL");
        terminalView.AppendLine("  CLEAR       - CLEAR SCREEN");
        terminalView.AppendLine("  EXIT        - CLOSE TERMINAL");
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendMailMessageHelp()
    {
        terminalView.AppendLine("MESSAGE COMMANDS:");
        terminalView.AppendLine("  DIR/LIST - RE-DISPLAY MESSAGE");
        terminalView.AppendLine("  BACK     - RETURN TO CONTACT");
        terminalView.AppendLine("  CLEAR    - CLEAR SCREEN");
        terminalView.AppendLine("  EXIT     - CLOSE TERMINAL");
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendDiaryHelp()
    {
        terminalView.AppendLine("DIARY COMMANDS:");
        terminalView.AppendLine("  BACK  - RETURN TO ROOT");
        terminalView.AppendLine("  CLEAR - CLEAR SCREEN");
        terminalView.AppendLine("  EXIT  - CLOSE TERMINAL");
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendRootSystems()
    {
        terminalView.AppendLine("");
        terminalView.AppendLine("AVAILABLE SYSTEMS:");
        terminalView.AppendLine("");
        terminalView.AppendLine("  MAIL      SYS");
        terminalView.AppendLine("  DIARY     SYS");
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void AppendDiaryUnavailable()
    {
        terminalView.AppendLine("");
        terminalView.AppendLine("DIARY SYSTEM NOT AVAILABLE.");
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
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
        terminalView.UpdateLiveInputLine(terminalView.currentPrompt, "");
        terminalView.FocusInput();
    }

    private void BadCommand()
    {
        terminalView.AppendLine("BAD COMMAND OR FILE NAME.");
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
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
                TerminalLayer.MailContact => $"ARCADIA:\\MAIL\\{mailSystem?.GetContactName(currentContactId)}>",
                TerminalLayer.MailMessage => $"ARCADIA:\\MAIL\\{mailSystem?.GetContactName(currentContactId)}\\{currentMessageId}>",
                _ => "ARCADIA:\\>"
            };
        }
    }
}