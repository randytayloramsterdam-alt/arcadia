using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("AI Chat")]
    public BackendChatClient backendChatClient;

    [Header("Mail Notification")]
    public ComputerMailNotification mailNotification;

    [Header("AI Reply Timing")]
    public float minAIReplyDelay = 3f;

    [Header("Command Config")]
    public TerminalCommandConfig commandConfig;

    [Header("Tab Completion")]
    public bool enableTabCompletion = true;
    public string completionSuggestionColorHex = "#666666";

    private bool tabHeldWithEmptyInput;

    [Header("Focus Settings")]
    public bool keepInputFocused = true;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    [Header("Root Welcome")]
    [TextArea(3, 12)]
    public string rootWelcomeText =
        "ARCADIA TERMINAL READY.\n\nAVAILABLE SYSTEMS:\n\n  MAIL      SYS\n  DIARY     SYS\n\nTYPE HELP FOR COMMAND LIST.";

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
    private bool mailInitialized = false;
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

    private class CompletionCandidate
    {
        public TerminalCommandId commandId;
        public string alias;
        public int priority;
        public bool isIdCandidate;
        public string displayText;
        public string insertText;

        public CompletionCandidate(TerminalCommandId commandId, string alias, int priority)
        {
            this.commandId = commandId;
            this.alias = alias;
            this.priority = priority;
            this.isIdCandidate = false;
            this.displayText = alias;
            this.insertText = alias;
        }

        public CompletionCandidate(string displayText, string insertText, int priority, bool isIdCandidate)
        {
            this.commandId = TerminalCommandId.Help;
            this.alias = "";
            this.priority = priority;
            this.isIdCandidate = isIdCandidate;
            this.displayText = displayText;
            this.insertText = insertText;
        }
    }

    private void Update()
    {
        if (!keepInputFocused || computerUIController == null || !computerUIController.IsOpen)
            return;
        if (terminalView == null || terminalView.inputField == null)
            return;
        if (!terminalView.inputField.interactable)
            return;

        if (enableTabCompletion)
        {
            string rawInput = terminalView != null ? terminalView.GetInputText() : "";

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (!string.IsNullOrEmpty(rawInput))
                    HandleTabComplete();
            }
            else
            {
                bool tabHeld = Input.GetKey(KeyCode.Tab);
                bool inputEmpty = string.IsNullOrEmpty(rawInput);

                if (tabHeld && inputEmpty)
                {
                    tabHeldWithEmptyInput = true;
                    string suggestion = GetEmptyInputSuggestion();
                    terminalView.SetCompletionSuggestion(suggestion, completionSuggestionColorHex);
                }
                else if (tabHeldWithEmptyInput && !tabHeld)
                {
                    tabHeldWithEmptyInput = false;
                    terminalView.ClearCompletionSuggestion();
                }

                if (!tabHeld)
                {
                    UpdateCompletionSuggestion(rawInput);
                }
            }
        }

        GameObject selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        if (selected != terminalView.inputField.gameObject)
            terminalView.FocusInput();
    }

    private void Start()
    {
        InitializeMailSystemIfNeeded();
        InitializeCommandConfig();

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

    private void InitializeCommandConfig()
    {
        if (commandConfig != null)
            commandConfig.Initialize();
    }

    private void InitializeMailSystemIfNeeded()
    {
        if (mailInitialized)
            return;
        if (mailSystem != null)
            mailSystem.Initialize();
        mailInitialized = true;
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
        UpdateCompletionSuggestion(value);
    }

    private void UpdateCompletionSuggestion(string input)
    {
        if (!enableTabCompletion)
        {
            terminalView.ClearCompletionSuggestion();
            return;
        }

        var idMatches = GetIdCompletionMatches(input);

        if (idMatches.Count > 0)
        {
            string richBody = BuildIdRichBody(input, idMatches);
            terminalView.SetLiveInputRichBodyOverride(richBody);
            return;
        }

        terminalView.ClearLiveInputRichBodyOverride();

        string suggestion = GetCompletionSuggestion(input);
        if (suggestion.Contains("|"))
        {
            string[] parts = suggestion.Split('|');
            string pre = parts[0];
            string post = parts.Length > 1 ? parts[1] : "";
            terminalView.SetCompletionSuggestionParts(pre, post, completionSuggestionColorHex);
        }
        else
        {
            terminalView.SetCompletionSuggestion(suggestion, completionSuggestionColorHex);
        }
    }

    private string GetCompletionSuggestion(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        var matches = GetCompletionMatches(input);
        var idMatches = GetIdCompletionMatches(input);

        bool useIdMatches = idMatches.Count > 0;

        if (useIdMatches)
        {
            return BuildIdCompletionSuggestion(input, idMatches);
        }

        var displayParts = new List<string>();
        for (int i = 0; i < matches.Count; i++)
        {
            string full = ApplyInputCaseStyle(matches[i].alias, input);
            if (CompletionShouldEndWithSpace(matches[i].alias))
                full += " ";

            if (full.Length <= input.Length)
                continue;

            if (i == 0)
            {
                displayParts.Add(full.Substring(input.Length));
            }
            else
            {
                displayParts.Add(full);
            }
        }

        if (displayParts.Count == 0)
            return "";

        return string.Join(" / ", displayParts);
    }

    private string BuildIdCompletionSuggestion(string rawInput, List<CompletionCandidate> idMatches)
    {
        if (idMatches.Count == 0 || !TryGetIdCompletionContext(rawInput, out string prefixBeforeId, out string idQuery))
            return "";

        string preSuggestion = "";
        var postParts = new List<string>();

        for (int i = 0; i < idMatches.Count; i++)
        {
            string fullId = idMatches[i].displayText;

            if (i == 0)
            {
                int idQueryIndex = fullId.IndexOf(idQuery, StringComparison.OrdinalIgnoreCase);
                if (idQueryIndex == 0)
                {
                    preSuggestion = "";
                    postParts.Add(fullId.Substring(idQuery.Length));
                }
                else
                {
                    preSuggestion = fullId.Substring(0, idQueryIndex);
                    postParts.Add(fullId.Substring(idQueryIndex + idQuery.Length));
                }
            }
            else
            {
                postParts.Add(fullId);
            }
        }

        return preSuggestion + "|" + string.Join(" / ", postParts);
    }

    private string BuildIdRichBody(string rawInput, List<CompletionCandidate> idMatches)
    {
        if (idMatches.Count == 0 || !TryGetIdCompletionContext(rawInput, out string prefixBeforeId, out string idQuery))
            return rawInput;

        string preIdSuggestion = "";
        var postIdParts = new List<string>();

        for (int i = 0; i < idMatches.Count; i++)
        {
            string fullId = idMatches[i].displayText;

            if (i == 0)
            {
                int idQueryIndex = fullId.IndexOf(idQuery, StringComparison.OrdinalIgnoreCase);
                if (idQueryIndex == 0)
                {
                    preIdSuggestion = "";
                    postIdParts.Add(fullId.Substring(idQuery.Length));
                }
                else
                {
                    preIdSuggestion = fullId.Substring(0, idQueryIndex);
                    postIdParts.Add(fullId.Substring(idQueryIndex + idQuery.Length));
                }
            }
            else
            {
                postIdParts.Add(fullId);
            }
        }

        string body = prefixBeforeId;
        if (!string.IsNullOrEmpty(preIdSuggestion))
            body += "<color=" + completionSuggestionColorHex + ">" + preIdSuggestion + "</color>";
        body += idQuery;
        if (postIdParts.Count > 0 && !string.IsNullOrEmpty(postIdParts[0]))
            body += "<color=" + completionSuggestionColorHex + ">" + string.Join(" / ", postIdParts) + "</color>";

        return body;
    }

    private string GetEmptyInputSuggestion()
    {
        string suggestion;
        switch (currentLayer)
        {
            case TerminalLayer.Root:
                suggestion = "mail / diary / dir / refresh / home / help / clear / exit";
                break;
            case TerminalLayer.Mail:
                suggestion = "[id] / open [id] / dir / back / refresh / home / help / clear / exit";
                break;
            case TerminalLayer.MailContact:
                suggestion = "[id] / open [id] / send [text] / dir / back / refresh / home / help / clear / exit";
                break;
            case TerminalLayer.MailMessage:
                suggestion = "dir / back / refresh / home / help / clear / exit";
                break;
            case TerminalLayer.Diary:
                suggestion = "dir / back / refresh / home / help / clear / exit";
                break;
            default:
                suggestion = "";
                break;
        }
        return suggestion;
    }

    private int GetCommandPriority(string primaryAlias)
    {
        if (commandConfig == null)
            return 100;

        foreach (var entry in commandConfig.commands)
        {
            string pa = entry.GetPrimaryAlias();
            if (string.Equals(pa, primaryAlias, StringComparison.OrdinalIgnoreCase))
                return entry.completionPriority;
        }
        return 100;
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
        terminalView.ClearCompletionSuggestion();

        if (string.IsNullOrEmpty(rawInput))
        {
            ClearLiveInputLeadingBlank();
            terminalView.UpdateLiveInputLine(CurrentPrompt, "");
            terminalView.FocusInput();
            return;
        }

        string displayLine = CurrentPrompt + " " + rawInput.Trim();

        if (terminalView.LiveInputHasLeadingBlankLine())
        {
            terminalView.AppendLine("");
            terminalView.SetLiveInputLeadingBlankLine(false);
        }

        terminalView.AppendLine(displayLine);

        string normalized = rawInput.Trim().ToUpperInvariant();
        string[] parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string verb = parts.Length > 0 ? parts[0] : "";
        string arg = parts.Length > 1 ? parts[1] : "";
        string normalizedCommand = string.Join(" ", parts);

        string sendBody = "";
        if (StartsWithAlias(TerminalCommandId.SendMessage, normalizedCommand, out sendBody))
            sendBody = rawInput.Substring(FindAliasPrefixLength(TerminalCommandId.SendMessage, normalized)).Trim();

        ProcessGlobalCommand(verb, arg, normalizedCommand, sendBody);
    }

    private void ProcessGlobalCommand(string verb, string arg, string normalizedCommand, string sendBody = "")
    {
        if (MatchesCommand(TerminalCommandId.OpenMail, normalizedCommand))
        {
            EnterMail();
            return;
        }
        if (MatchesCommand(TerminalCommandId.OpenDiary, normalizedCommand))
        {
            EnterDiary();
            return;
        }
        if (MatchesCommand(TerminalCommandId.Clear, normalizedCommand))
        {
            ClearLiveInputLeadingBlank();
            terminalView.Clear();
            terminalView.ClearCompletionSuggestion();
            terminalView.UpdateLiveInputLine(CurrentPrompt, "");
            terminalView.FocusInput();
            return;
        }
        if (MatchesCommand(TerminalCommandId.Exit, normalizedCommand))
        {
            ExitComputer();
            return;
        }
        if (MatchesCommand(TerminalCommandId.Back, normalizedCommand))
        {
            HandleBack();
            return;
        }
        if (MatchesCommand(TerminalCommandId.Refresh, normalizedCommand))
        {
            HandleRefresh();
            return;
        }
        if (MatchesCommand(TerminalCommandId.GoRoot, normalizedCommand))
        {
            HandleGoRoot();
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

    private void HandleRefresh()
    {
        switch (currentLayer)
        {
            case TerminalLayer.Root:
                terminalView.ClearCompletionSuggestion();
                ShowRootMenu();
                break;
            case TerminalLayer.Mail:
                AppendMailContactList();
                break;
            case TerminalLayer.Diary:
                AppendDiaryUnavailable();
                break;
            case TerminalLayer.MailContact:
                AppendMessageList();
                break;
            case TerminalLayer.MailMessage:
                AppendMessageBody();
                break;
        }
        FinishCommandOutput();
    }

    private void HandleGoRoot()
    {
        currentLayer = TerminalLayer.Root;
        currentContactId = "";
        currentMessageId = "";
        terminalView.SetPrompt("ARCADIA:\\>");
        terminalView.ClearCompletionSuggestion();
        FinishCommandOutput();
    }

    private void HandleBack()
    {
        switch (currentLayer)
        {
            case TerminalLayer.Root:
                ClearLiveInputLeadingBlank();
                terminalView.ClearCompletionSuggestion();
                terminalView.UpdateLiveInputLine(terminalView.currentPrompt, "");
                terminalView.FocusInput();
                break;

            case TerminalLayer.Mail:
            case TerminalLayer.Diary:
                currentLayer = TerminalLayer.Root;
                currentContactId = "";
                currentMessageId = "";
                terminalView.SetPrompt("ARCADIA:\\>");
                terminalView.ClearCompletionSuggestion();
                FinishCommandOutput();
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
        if (MatchesCommand(TerminalCommandId.List, normalizedCommand))
        {
            AppendRootSystems();
        }
        else if (MatchesCommand(TerminalCommandId.Help, normalizedCommand))
        {
            AppendRootHelp();
        }
        else
        {
            BadCommand();
        }
    }

    private void ProcessMailCommand(string verb, string arg, string normalizedCommand)
    {
        if (MatchesCommand(TerminalCommandId.List, normalizedCommand))
        {
            AppendMailContactList();
        }
        else if (MatchesCommand(TerminalCommandId.Help, normalizedCommand))
        {
            AppendMailHelp();
        }
        else if (mailSystem != null && mailSystem.GetContact(normalizedCommand) != null)
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
    }

    private void ProcessMailContactCommand(string verb, string arg, string normalizedCommand, string sendBody = "")
    {
        if (verb == "SEND")
        {
            if (string.IsNullOrWhiteSpace(sendBody))
            {
                terminalView.AppendLine("EMPTY MESSAGE BUFFER.");
                FinishCommandOutput();
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

        if (MatchesCommand(TerminalCommandId.List, normalizedCommand))
        {
            AppendMessageList();
        }
        else if (MatchesCommand(TerminalCommandId.Help, normalizedCommand))
        {
            AppendMailContactHelp();
        }
        else
        {
            string openArg;
            if (StartsWithAlias(TerminalCommandId.OpenItem, normalizedCommand, out openArg) && IsMessageId(openArg))
            {
                EnterMailMessage(currentContactId, openArg);
                return;
            }

            if (IsMessageId(verb))
            {
                EnterMailMessage(currentContactId, verb);
                return;
            }

            BadCommand();
        }
    }

    private void ProcessMailMessageCommand(string verb, string arg, string normalizedCommand)
    {
        if (MatchesCommand(TerminalCommandId.List, normalizedCommand))
        {
            AppendMessageBody();
        }
        else if (MatchesCommand(TerminalCommandId.Help, normalizedCommand))
        {
            AppendMailMessageHelp();
        }
        else
        {
            BadCommand();
        }
    }

    private void ProcessDiaryCommand(string verb, string arg, string normalizedCommand)
    {
        if (MatchesCommand(TerminalCommandId.List, normalizedCommand))
        {
            AppendDiaryUnavailable();
        }
        else if (MatchesCommand(TerminalCommandId.Help, normalizedCommand))
        {
            AppendDiaryHelp();
        }
        else
        {
            BadCommand();
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
            FinishCommandOutput();
            return;
        }

        if (mailSystem == null || string.IsNullOrEmpty(currentContactId))
        {
            BadCommand();
            return;
        }

        mailSystem.AddSentMessage(currentContactId, body);
        string contactName = mailSystem.GetContactName(currentContactId);

        AppendMessageList();

        if (mailSystem.GetContactEnableAIReply(currentContactId) && backendChatClient != null)
        {
            StartCoroutine(HandleAIReplyInBackground(currentContactId, contactName, body));
        }
    }

    private IEnumerator HandleAIReplyInBackground(string contactId, string contactName, string body)
    {
        float startTime = Time.unscaledTime;
        bool success = false;
        string replyText = "";

        yield return backendChatClient.SendMessage(body,
            reply => {
                replyText = reply;
                success = true;
            },
            error => {
                if (enableDebugLogs)
                    Debug.LogWarning("[ComputerTerminal] AI reply failed: " + error);
            });

        if (!success)
            yield break;

        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minAIReplyDelay)
            yield return new WaitForSecondsRealtime(minAIReplyDelay - elapsed);

        mailSystem.AddIncomingMessage(contactId, contactName, replyText);

        if (mailNotification != null)
            mailNotification.PlayNewMailNotification();

        if (currentLayer == TerminalLayer.MailContact && currentContactId == contactId)
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
            FinishCommandOutput();
            return;
        }

        string output = mailSystem.RenderContactList();
        string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
            terminalView.AppendLine(line);

        FinishCommandOutput();
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

        FinishCommandOutput();
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

        FinishCommandOutput();
    }

    private void AppendRootHelp()
    {
        int width = commandConfig != null ? commandConfig.helpDescriptionWidth : 28;
        terminalView.AppendLine("AVAILABLE COMMANDS");
        terminalView.AppendLine("");

        AppendHelpLine(TerminalCommandId.Help);
        AppendHelpLine(TerminalCommandId.List);
        AppendHelpLine(TerminalCommandId.OpenMail);
        AppendHelpLine(TerminalCommandId.OpenDiary);
        AppendHelpLine(TerminalCommandId.Refresh);
        AppendHelpLine(TerminalCommandId.GoRoot);
        AppendHelpLine(TerminalCommandId.Clear);
        AppendHelpLine(TerminalCommandId.Exit);

        FinishCommandOutput();
    }

    private void AppendMailHelp()
    {
        terminalView.AppendLine("MAIL COMMANDS");
        terminalView.AppendLine("");

        AppendHelpLine(TerminalCommandId.Help);
        AppendHelpLine(TerminalCommandId.List);
        AppendHelpLine(TerminalCommandId.OpenItem);
        AppendHelpLine(TerminalCommandId.Refresh);
        AppendHelpLine(TerminalCommandId.GoRoot);
        AppendHelpLine(TerminalCommandId.Back);
        AppendHelpLine(TerminalCommandId.Clear);
        AppendHelpLine(TerminalCommandId.Exit);

        FinishCommandOutput();
    }

    private void AppendMailContactHelp()
    {
        terminalView.AppendLine("CONTACT COMMANDS");
        terminalView.AppendLine("");

        AppendHelpLine(TerminalCommandId.Help);
        AppendHelpLine(TerminalCommandId.List);
        AppendHelpLine(TerminalCommandId.OpenItem);
        AppendHelpLine(TerminalCommandId.SendMessage);
        AppendHelpLine(TerminalCommandId.Refresh);
        AppendHelpLine(TerminalCommandId.GoRoot);
        AppendHelpLine(TerminalCommandId.Back);
        AppendHelpLine(TerminalCommandId.Clear);
        AppendHelpLine(TerminalCommandId.Exit);

        FinishCommandOutput();
    }

    private void AppendMailMessageHelp()
    {
        terminalView.AppendLine("MESSAGE COMMANDS");
        terminalView.AppendLine("");

        AppendHelpLine(TerminalCommandId.Help);
        AppendHelpLine(TerminalCommandId.List);
        AppendHelpLine(TerminalCommandId.Refresh);
        AppendHelpLine(TerminalCommandId.GoRoot);
        AppendHelpLine(TerminalCommandId.Back);
        AppendHelpLine(TerminalCommandId.Clear);
        AppendHelpLine(TerminalCommandId.Exit);

        FinishCommandOutput();
    }

    private void AppendDiaryHelp()
    {
        terminalView.AppendLine("DIARY COMMANDS");
        terminalView.AppendLine("");

        AppendHelpLine(TerminalCommandId.Help);
        AppendHelpLine(TerminalCommandId.List);
        AppendHelpLine(TerminalCommandId.Refresh);
        AppendHelpLine(TerminalCommandId.GoRoot);
        AppendHelpLine(TerminalCommandId.Back);
        AppendHelpLine(TerminalCommandId.Clear);
        AppendHelpLine(TerminalCommandId.Exit);

        FinishCommandOutput();
    }

    private void AppendHelpLine(TerminalCommandId id)
    {
        AppendHelpLine(id, currentLayer);
    }

    private void AppendHelpLine(TerminalCommandId id, TerminalLayer layer)
    {
        var entry = commandConfig != null ? commandConfig.GetEntry(id) : null;
        string desc = entry != null ? entry.description : id.ToString().ToUpper();
        string aliases = commandConfig != null ? commandConfig.GetAliasDisplay(id) : "";
        int width = commandConfig != null ? commandConfig.helpDescriptionWidth : 28;

        desc = GetHelpDescription(id, layer, desc);
        aliases = GetHelpAliasDisplay(id, layer, aliases);

        if (string.IsNullOrEmpty(aliases))
        {
            terminalView.AppendLine(desc);
            return;
        }

        string padded = desc.PadRight(width);
        terminalView.AppendLine(padded + aliases);
    }

    private string GetHelpDescription(TerminalCommandId id, TerminalLayer layer, string defaultDesc)
    {
        if (layer == TerminalLayer.Mail && id == TerminalCommandId.OpenItem)
            return "OPEN CONTACT";
        if (layer == TerminalLayer.MailContact && id == TerminalCommandId.OpenItem)
            return "OPEN MESSAGE";
        return defaultDesc;
    }

    private string GetHelpAliasDisplay(TerminalCommandId id, TerminalLayer layer, string defaultAliases)
    {
        if (layer == TerminalLayer.Mail && id == TerminalCommandId.OpenItem)
            return "[ID] / CD [ID] / OPEN [ID]";
        if (layer == TerminalLayer.MailContact && id == TerminalCommandId.OpenItem)
            return "[ID] / CD [ID] / OPEN [ID]";
        if (layer == TerminalLayer.MailContact && id == TerminalCommandId.SendMessage)
            return "SEND [TEXT]";
        return defaultAliases;
    }

    private void AppendRootSystems()
    {
        terminalView.AppendLine("AVAILABLE SYSTEMS:");
        terminalView.AppendLine("");
        terminalView.AppendLine("  MAIL      SYS");
        terminalView.AppendLine("  DIARY     SYS");
        FinishCommandOutput();
    }

    private void AppendDiaryUnavailable()
    {
        terminalView.AppendLine("DIARY SYSTEM NOT AVAILABLE.");
        FinishCommandOutput();
    }

    private void ShowRootMenu()
    {
        terminalView.Clear();
        ClearLiveInputLeadingBlank();

        string normalized = rootWelcomeText.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None);
        foreach (var line in lines)
            terminalView.AppendLine(line);

        terminalView.SetLiveInputLeadingBlankLine(true);
        terminalView.UpdateLiveInputLine(terminalView.currentPrompt, "");
        terminalView.FocusInput();
    }

    private void BadCommand()
    {
        terminalView.AppendLine("BAD COMMAND OR FILE NAME.");
        FinishCommandOutput();
    }

    private void ExitComputer()
    {
        if (computerUIController != null)
            computerUIController.Close();
    }

    private void AppendBlankLineIfNeeded()
    {
        var lines = terminalView.TerminalLines;
        if (lines == null || lines.Count == 0 || lines[lines.Count - 1] != "")
            terminalView.AppendLine("");
    }

    private void FinishCommandOutput()
    {
        terminalView.SetLiveInputLeadingBlankLine(true);
        terminalView.UpdateLiveInputLine(CurrentPrompt, "");
        terminalView.FocusInput();
    }

    private void ClearLiveInputLeadingBlank()
    {
        terminalView.SetLiveInputLeadingBlankLine(false);
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

    private bool MatchesCommand(TerminalCommandId id, string normalizedInput)
    {
        if (commandConfig != null)
            return commandConfig.Matches(id, normalizedInput);

        return FallbackMatches(id, normalizedInput);
    }

    private bool StartsWithAlias(TerminalCommandId id, string normalizedInput, out string remainingText)
    {
        if (commandConfig != null)
            return commandConfig.StartsWithAlias(id, normalizedInput, out remainingText);

        remainingText = "";
        return FallbackStartsWithAlias(id, normalizedInput, ref remainingText);
    }

    private int FindAliasPrefixLength(TerminalCommandId id, string normalizedInput)
    {
        if (commandConfig != null)
        {
            var entry = commandConfig.GetEntry(id);
            if (entry == null || entry.aliases == null)
                return 0;

            foreach (var alias in entry.aliases)
            {
                string prefix = alias + " ";
                if (normalizedInput.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return prefix.Length;
            }
            if (normalizedInput.Equals("SEND", StringComparison.OrdinalIgnoreCase))
                return 5;
            return 0;
        }

        return FallbackFindAliasPrefixLength(id, normalizedInput);
    }

    private bool FallbackMatches(TerminalCommandId id, string normalizedInput)
    {
        switch (id)
        {
            case TerminalCommandId.Help: return normalizedInput == "HELP";
            case TerminalCommandId.List: return normalizedInput == "DIR" || normalizedInput == "LIST";
            case TerminalCommandId.OpenMail: return normalizedInput == "MAIL" || normalizedInput == "CD MAIL" || normalizedInput == "OPEN MAIL";
            case TerminalCommandId.OpenDiary: return normalizedInput == "DIARY" || normalizedInput == "CD DIARY" || normalizedInput == "OPEN DIARY";
            case TerminalCommandId.Back: return normalizedInput == "BACK" || normalizedInput == "RETURN";
            case TerminalCommandId.Clear: return normalizedInput == "CLEAR" || normalizedInput == "CLS";
            case TerminalCommandId.Exit: return normalizedInput == "EXIT" || normalizedInput == "QUIT";
            case TerminalCommandId.Refresh: return normalizedInput == "REFRESH" || normalizedInput == "RELOAD";
            case TerminalCommandId.GoRoot: return normalizedInput == "HOME" || normalizedInput == "ROOT" || normalizedInput == "MAIN";
            case TerminalCommandId.OpenItem: return normalizedInput == "CD" || normalizedInput == "OPEN";
            case TerminalCommandId.ReadMessage: return normalizedInput == "READ";
            case TerminalCommandId.SendMessage: return normalizedInput == "SEND";
        }
        return false;
    }

    private bool FallbackStartsWithAlias(TerminalCommandId id, string normalizedInput, ref string remainingText)
    {
        switch (id)
        {
            case TerminalCommandId.SendMessage:
                if (normalizedInput.StartsWith("SEND ", StringComparison.OrdinalIgnoreCase))
                {
                    remainingText = normalizedInput.Substring(5);
                    return true;
                }
                break;
            case TerminalCommandId.ReadMessage:
                if (normalizedInput.StartsWith("READ ", StringComparison.OrdinalIgnoreCase))
                {
                    remainingText = normalizedInput.Substring(5);
                    return true;
                }
                break;
            case TerminalCommandId.OpenItem:
                if (normalizedInput.StartsWith("CD ", StringComparison.OrdinalIgnoreCase))
                {
                    remainingText = normalizedInput.Substring(3);
                    return true;
                }
                if (normalizedInput.StartsWith("OPEN ", StringComparison.OrdinalIgnoreCase))
                {
                    remainingText = normalizedInput.Substring(5);
                    return true;
                }
                break;
        }
        return false;
    }

    private int FallbackFindAliasPrefixLength(TerminalCommandId id, string normalizedInput)
    {
        switch (id)
        {
            case TerminalCommandId.SendMessage:
                if (normalizedInput.StartsWith("SEND ", StringComparison.OrdinalIgnoreCase))
                    return 5;
                break;
        }
        return 0;
    }

    private void HandleTabComplete()
    {
        if (!enableTabCompletion)
            return;

        string rawInput = terminalView != null ? terminalView.GetInputText() : "";
        var idMatches = GetIdCompletionMatches(rawInput);

        if (idMatches.Count > 0)
        {
            string insertText = idMatches[0].insertText;
            terminalView.ClearInput();
            terminalView.inputField.text = insertText;
            terminalView.SetCompletionSuggestion("", completionSuggestionColorHex);
            terminalView.UpdateLiveInputLine(CurrentPrompt, insertText);
            terminalView.FocusInput();
            return;
        }

        var matches = GetCompletionMatches(rawInput);
        if (matches.Count == 0)
            return;

        string completion = matches[0].alias;
        string styled = ApplyInputCaseStyle(completion, rawInput);
        if (CompletionShouldEndWithSpace(completion))
            styled += " ";

        terminalView.ClearInput();
        terminalView.inputField.text = styled;
        terminalView.SetCompletionSuggestion("", completionSuggestionColorHex);
        terminalView.UpdateLiveInputLine(CurrentPrompt, styled);
        terminalView.FocusInput();
    }

    private List<CompletionCandidate> GetCompletionMatches(string input)
    {
        var candidates = GetCurrentLayerCompletionCandidates(false);
        if (string.IsNullOrEmpty(input))
            return new List<CompletionCandidate>();

        var resultMatches = new List<CompletionCandidate>();
        foreach (var c in candidates)
        {
            if (c.alias.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                resultMatches.Add(c);
        }

        resultMatches.Sort((a, b) =>
        {
            if (a.priority != b.priority)
                return a.priority.CompareTo(b.priority);
            if (a.alias.Length != b.alias.Length)
                return a.alias.Length.CompareTo(b.alias.Length);
            return string.Compare(a.alias, b.alias, StringComparison.OrdinalIgnoreCase);
        });

        return resultMatches;
    }

    private List<CompletionCandidate> GetCurrentLayerCompletionCandidates(bool primaryOnly)
    {
        var result = new List<CompletionCandidate>();
        TerminalCommandId[] ids;

        switch (currentLayer)
        {
            case TerminalLayer.Root:
                ids = new TerminalCommandId[] {
                    TerminalCommandId.Help, TerminalCommandId.List,
                    TerminalCommandId.OpenMail, TerminalCommandId.OpenDiary,
                    TerminalCommandId.Refresh, TerminalCommandId.GoRoot,
                    TerminalCommandId.Clear, TerminalCommandId.Exit
                };
                break;
            case TerminalLayer.Mail:
                ids = new TerminalCommandId[] {
                    TerminalCommandId.Help, TerminalCommandId.List,
                    TerminalCommandId.OpenItem, TerminalCommandId.Refresh,
                    TerminalCommandId.GoRoot, TerminalCommandId.Back,
                    TerminalCommandId.Clear, TerminalCommandId.Exit
                };
                break;
            case TerminalLayer.MailContact:
                ids = new TerminalCommandId[] {
                    TerminalCommandId.Help, TerminalCommandId.List,
                    TerminalCommandId.OpenItem,
                    TerminalCommandId.SendMessage, TerminalCommandId.Refresh,
                    TerminalCommandId.GoRoot, TerminalCommandId.Back,
                    TerminalCommandId.Clear, TerminalCommandId.Exit
                };
                break;
            case TerminalLayer.MailMessage:
                ids = new TerminalCommandId[] {
                    TerminalCommandId.Help, TerminalCommandId.List,
                    TerminalCommandId.Refresh, TerminalCommandId.GoRoot,
                    TerminalCommandId.Back, TerminalCommandId.Clear,
                    TerminalCommandId.Exit
                };
                break;
            case TerminalLayer.Diary:
                ids = new TerminalCommandId[] {
                    TerminalCommandId.Help, TerminalCommandId.List,
                    TerminalCommandId.Refresh, TerminalCommandId.GoRoot,
                    TerminalCommandId.Back, TerminalCommandId.Clear,
                    TerminalCommandId.Exit
                };
                break;
            default:
                return result;
        }

        var seenAliases = new HashSet<string>();

        foreach (var id in ids)
        {
            var entry = commandConfig != null ? commandConfig.GetEntry(id) : null;
            if (entry == null)
                continue;
            if (!entry.showInCompletion)
                continue;

            List<string> aliasesToUse;
            if (primaryOnly)
            {
                string pa = entry.GetPrimaryAlias();
                if (string.IsNullOrEmpty(pa))
                    continue;
                aliasesToUse = new List<string> { pa };
            }
            else
            {
                if (entry.aliases == null || entry.aliases.Count == 0)
                {
                    string pa = entry.GetPrimaryAlias();
                    if (!string.IsNullOrEmpty(pa))
                        aliasesToUse = new List<string> { pa };
                    else
                        continue;
                }
                else
                {
                    aliasesToUse = entry.aliases;
                }
            }

            foreach (var a in aliasesToUse)
            {
                string upper = a.ToUpperInvariant();
                if (seenAliases.Contains(upper))
                    continue;
                seenAliases.Add(upper);
                result.Add(new CompletionCandidate(id, upper, entry.completionPriority));
            }
        }

        return result;
    }

    private string ApplyInputCaseStyle(string completion, string input)
    {
        if (string.IsNullOrEmpty(input))
            return completion;

        char first = input[0];
        if (char.IsLower(first))
            return completion.ToLowerInvariant();
        if (char.IsUpper(first))
            return completion.ToUpperInvariant();
        return completion;
    }

    private List<string> GetCurrentLayerIdsForCompletion()
    {
        if (mailSystem == null)
            return new List<string>();

        if (currentLayer == TerminalLayer.Mail)
        {
            return mailSystem.GetAllContactIds();
        }

        if (currentLayer == TerminalLayer.MailContact && !string.IsNullOrEmpty(currentContactId))
        {
            return mailSystem.GetMessageIds(currentContactId);
        }

        return new List<string>();
    }

    private bool TryGetIdCompletionContext(string rawInput, out string prefixBeforeId, out string idQuery)
    {
        prefixBeforeId = "";
        idQuery = "";

        if (string.IsNullOrEmpty(rawInput))
            return false;

        if (char.IsDigit(rawInput[0]))
        {
            prefixBeforeId = "";
            idQuery = rawInput;
            return true;
        }

        var aliases = GetOpenItemAliases();
        foreach (var alias in aliases)
        {
            string aliasWithSpace = alias + " ";
            if (rawInput.Length > aliasWithSpace.Length &&
                rawInput.Substring(0, aliasWithSpace.Length).Equals(aliasWithSpace, StringComparison.OrdinalIgnoreCase))
            {
                string remaining = rawInput.Substring(aliasWithSpace.Length);
                if (IsDigitsOnly(remaining))
                {
                    prefixBeforeId = rawInput.Substring(0, aliasWithSpace.Length);
                    idQuery = remaining;
                    return true;
                }
            }
        }

        return false;
    }

    private List<string> GetOpenItemAliases()
    {
        if (commandConfig != null)
        {
            var entry = commandConfig.GetEntry(TerminalCommandId.OpenItem);
            if (entry != null && entry.aliases != null && entry.aliases.Count > 0)
                return entry.aliases;
        }
        return new List<string> { "CD", "OPEN" };
    }

    private bool IsDigitsOnly(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        foreach (char c in text)
        {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }

    private List<CompletionCandidate> GetIdCompletionMatches(string rawInput)
    {
        if (!TryGetIdCompletionContext(rawInput, out string prefixBeforeId, out string idQuery))
            return new List<CompletionCandidate>();

        if (string.IsNullOrEmpty(idQuery))
            return new List<CompletionCandidate>();

        var availableIds = GetCurrentLayerIdsForCompletion();
        if (availableIds.Count == 0)
            return new List<CompletionCandidate>();

        var matches = new List<CompletionCandidate>();
        for (int i = 0; i < availableIds.Count; i++)
        {
            string id = availableIds[i];
            if (id.Contains(idQuery))
            {
                string insertText = prefixBeforeId + id;
                matches.Add(new CompletionCandidate(id, insertText, i, true));
            }
        }

        return matches;
    }

    private bool CompletionShouldEndWithSpace(string completion)
    {
        string upper = completion.ToUpperInvariant().Trim();
        return upper == "SEND" || upper == "OPEN" || upper == "CD";
    }
}