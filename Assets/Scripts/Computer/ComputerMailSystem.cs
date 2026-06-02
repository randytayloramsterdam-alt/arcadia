using System;
using System.Collections.Generic;
using UnityEngine;

public class ComputerMailSystem : MonoBehaviour
{
    [Header("Config")]
    public MailSystemConfig mailSystemConfig;

    [Header("Runtime Data")]
    public List<MailContactData> contacts = new List<MailContactData>();

    private Dictionary<string, MailContactData> contactMap;
    private string localUserName = "LOCAL USER";

    public void Initialize()
    {
        if (mailSystemConfig != null)
            LoadFromConfig();
        else
            BuildTestData();

        contactMap = new Dictionary<string, MailContactData>();
        foreach (var contact in contacts)
            contactMap[contact.id] = contact;
    }

    private void LoadFromConfig()
    {
        localUserName = mailSystemConfig.localUserName;
        currentMailDate = mailSystemConfig.currentMailDate;

        contacts = new List<MailContactData>();
        foreach (var cfg in mailSystemConfig.contacts)
        {
            var contact = new MailContactData
            {
                id = cfg.id,
                name = cfg.displayName,
                enableAIReply = cfg.enableAIReply,
                aiProfileName = cfg.aiProfileName,
                aiSystemPrompt = cfg.aiSystemPrompt,
                aiMemoryNote = cfg.aiMemoryNote,
                messages = new List<MailMessageData>()
            };

            foreach (var msgCfg in cfg.initialMessages)
            {
                contact.messages.Add(new MailMessageData
                {
                    id = msgCfg.id,
                    date = msgCfg.date,
                    from = msgCfg.from,
                    to = msgCfg.to,
                    status = msgCfg.status.ToString(),
                    subject = msgCfg.subject,
                    body = msgCfg.body
                });
            }

            contacts.Add(contact);
        }
    }

    private void BuildTestData()
    {
        localUserName = "LOCAL USER";
        currentMailDate = "1983-10-07";

        contacts = new List<MailContactData>
        {
            new MailContactData
            {
                id = "001",
                name = "A. MORRISON",
                enableAIReply = false,
                messages = new List<MailMessageData>
                {
                    new MailMessageData
                    {
                        id = "001",
                        date = "1983-10-02",
                        from = "A. MORRISON",
                        to = "LOCAL USER",
                        status = "READ",
                        subject = "CHECK THE SUB-BASEMENT",
                        body = "There is something wrong with the sealed door in sector 7.\nDo not open it."
                    }
                }
            },
            new MailContactData
            {
                id = "002",
                name = "L. CARTER",
                enableAIReply = false,
                messages = new List<MailMessageData>
                {
                    new MailMessageData
                    {
                        id = "001",
                        date = "1983-10-05",
                        from = "L. CARTER",
                        to = "LOCAL USER",
                        status = "UNREAD",
                        subject = "RE: YOUR REQUEST",
                        body = "I have not heard back from you since the incident.\nPlease confirm you are safe."
                    }
                }
            },
            new MailContactData
            {
                id = "003",
                name = "E. BENSON",
                enableAIReply = true,
                aiProfileName = "E. BENSON",
                messages = new List<MailMessageData>
                {
                    new MailMessageData
                    {
                        id = "001",
                        date = "1983-10-02",
                        from = "E. BENSON",
                        to = "LOCAL USER",
                        status = "READ",
                        subject = "ARE YOU SAFE?",
                        body = "Are you still there?\nPlease respond when you can."
                    },
                    new MailMessageData
                    {
                        id = "002",
                        date = "1983-10-03",
                        from = "LOCAL USER",
                        to = "E. BENSON",
                        status = "SENT",
                        subject = "RE: ARE YOU SAFE?",
                        body = "I am here. What happened?"
                    },
                    new MailMessageData
                    {
                        id = "003",
                        date = "1983-10-07",
                        from = "E. BENSON",
                        to = "LOCAL USER",
                        status = "UNREAD",
                        subject = "PLEASE ANSWER",
                        body = "Please answer me.\nI know you are still there."
                    }
                }
            },
            new MailContactData
            {
                id = "004",
                name = "M. KELLER",
                enableAIReply = false,
                messages = new List<MailMessageData>
                {
                    new MailMessageData
                    {
                        id = "001",
                        date = "1983-09-29",
                        from = "M. KELLER",
                        to = "LOCAL USER",
                        status = "READ",
                        subject = "STORAGE PROTOCOL",
                        body = "All personnel must follow the new storage protocol.\nReport any anomalies to sector 3."
                    }
                }
            },
            new MailContactData
            {
                id = "005",
                name = "J. REED",
                enableAIReply = false,
                messages = new List<MailMessageData>
                {
                    new MailMessageData
                    {
                        id = "001",
                        date = "1983-09-18",
                        from = "J. REED",
                        to = "LOCAL USER",
                        status = "READ",
                        subject = "SHIFT REMINDER",
                        body = "Your shift starts at 06:00.\nDo not be late."
                    }
                }
            }
        };
    }

    public MailContactData GetContact(string contactId)
    {
        if (contactMap == null || !contactMap.ContainsKey(contactId))
            return null;
        return contactMap[contactId];
    }

    public List<string> GetAllContactIds()
    {
        var result = new List<string>();
        foreach (var c in contacts)
        {
            if (!string.IsNullOrEmpty(c.id))
                result.Add(c.id);
        }
        return result;
    }

    public List<string> GetMessageIds(string contactId)
    {
        var contact = GetContact(contactId);
        if (contact == null)
            return new List<string>();
        var result = new List<string>();
        foreach (var m in contact.messages)
        {
            if (!string.IsNullOrEmpty(m.id))
                result.Add(m.id);
        }
        return result;
    }

    public string GetContactName(string contactId)
    {
        var contact = GetContact(contactId);
        return contact != null ? contact.name : "";
    }

    public bool GetContactEnableAIReply(string contactId)
    {
        var contact = GetContact(contactId);
        return contact != null && contact.enableAIReply;
    }

    public string ComputeContactStatus(string contactId)
    {
        var contact = GetContact(contactId);
        if (contact == null || contact.messages.Count == 0)
            return "READ";

        foreach (var msg in contact.messages)
        {
            if (msg.status == "UNREAD")
                return "UNREAD";
        }
        return "READ";
    }

    public string ComputeContactLastDate(string contactId)
    {
        var contact = GetContact(contactId);
        if (contact == null || contact.messages.Count == 0)
            return "";

        return contact.messages[contact.messages.Count - 1].date;
    }

    public MailMessageData GetMessage(string contactId, string messageId)
    {
        var contact = GetContact(contactId);
        if (contact == null)
            return null;

        foreach (var msg in contact.messages)
        {
            if (msg.id == messageId)
                return msg;
        }
        return null;
    }

    public void MarkMessageRead(string contactId, string messageId)
    {
        var msg = GetMessage(contactId, messageId);
        if (msg != null && msg.status == "UNREAD")
            msg.status = "READ";
    }

    [Header("Mail Settings")]
    public string currentMailDate = "1983-10-07";

    [Header("Mail Layout Settings")]
    public int contactNameWidth = 23;
    public int contactStatusWidth = 8;

    public int messageFromWidth = 12;
    public int messageStatusWidth = 8;
    public int subjectIndentSpaces = 6;

    public int messageSeparatorLength = 53;
    public bool showBlankLineBetweenMessages = true;
    public bool showMessageMarkedAsReadNotice = false;

    [Header("Mail Highlight Settings")]
    public bool useRichTextHighlight = true;
    public string unreadHighlightColorHex = "#08FFFF";
    public bool highlightUnreadContacts = true;
    public bool highlightUnreadMessages = true;

    public MailMessageData AddSentMessage(string contactId, string body)
    {
        var contact = GetContact(contactId);
        if (contact == null)
            return null;

        int nextId = contact.messages.Count + 1;
        string id = nextId.ToString("D3");

        string subject = body.Length > 24 ? body.Substring(0, 24) + "..." : body;
        if (string.IsNullOrWhiteSpace(subject))
            subject = "NO SUBJECT";

        var msg = new MailMessageData
        {
            id = id,
            date = currentMailDate,
            from = localUserName,
            to = contact.name,
            status = "SENT",
            subject = subject,
            body = body
        };

        contact.messages.Add(msg);
        return msg;
    }

    public MailMessageData AddIncomingMessage(string contactId, string fromName, string body)
    {
        var contact = GetContact(contactId);
        if (contact == null)
            return null;

        int nextId = contact.messages.Count + 1;
        string id = nextId.ToString("D3");

        string subject = body.Length > 24 ? body.Substring(0, 24) + "..." : body;
        if (string.IsNullOrWhiteSpace(subject))
            subject = "NO SUBJECT";

        var msg = new MailMessageData
        {
            id = id,
            date = currentMailDate,
            from = fromName,
            to = localUserName,
            status = "UNREAD",
            subject = subject,
            body = body
        };

        contact.messages.Add(msg);
        return msg;
    }

    private string FitText(string text, int width)
    {
        if (string.IsNullOrEmpty(text))
            return Spaces(width);
        if (width <= 0)
            return "";
        if (text.Length <= width)
            return text.PadRight(width);
        if (width <= 3)
            return text.Substring(0, width);
        return text.Substring(0, width - 3) + "...";
    }

    private string Spaces(int count)
    {
        if (count <= 0)
            return "";
        return new string(' ', count);
    }

    private string SeparatorLine()
    {
        return new string('-', messageSeparatorLength);
    }

    private string Colorize(string text, string colorHex)
    {
        if (!useRichTextHighlight)
            return text;
        if (string.IsNullOrWhiteSpace(colorHex))
            return text;
        return $"<color={colorHex}>{text}</color>";
    }

    public string RenderContactList()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("MAIL CONTACTS");
        sb.AppendLine("");

        foreach (var contact in contacts)
        {
            string status = ComputeContactStatus(contact.id);
            string lastDate = ComputeContactLastDate(contact.id);
            string name = FitText(contact.name, contactNameWidth);
            string stat = FitText(status, contactStatusWidth);
            string line = $"  [{contact.id}] {name} {stat} LAST: {lastDate}";
            if (highlightUnreadContacts && status == "UNREAD")
                line = Colorize(line, unreadHighlightColorHex);
            line = $"<link=\"CONTACT:{contact.id}\">{line}</link>";
            sb.AppendLine(line);
        }

        return sb.ToString().TrimEnd('\n', '\r');
    }

    public string RenderMessageList(string contactId)
    {
        var contact = GetContact(contactId);
        if (contact == null)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"CONTACT: {contact.name}");
        sb.AppendLine("");

        for (int i = 0; i < contact.messages.Count; i++)
        {
            var msg = contact.messages[i];
            string from = FitText(msg.from, messageFromWidth);
            string stat = FitText(msg.status, messageStatusWidth);
            string headerLine = $"  [{msg.id}] {msg.date}    FROM: {from} {stat}";
            string subjectLine = Spaces(2 + subjectIndentSpaces) + $"SUBJECT: {msg.subject}";
            string linkId = $"MESSAGE:{contactId}:{msg.id}";

            if (highlightUnreadMessages && msg.status == "UNREAD")
            {
                string coloredHeader = Colorize(headerLine, unreadHighlightColorHex);
                string coloredSubject = Colorize(subjectLine, unreadHighlightColorHex);
                sb.AppendLine($"<link=\"{linkId}\">{coloredHeader}</link>");
                sb.AppendLine($"<link=\"{linkId}\">{coloredSubject}</link>");
            }
            else
            {
                sb.AppendLine($"<link=\"{linkId}\">{headerLine}</link>");
                sb.AppendLine($"<link=\"{linkId}\">{subjectLine}</link>");
            }

            if (showBlankLineBetweenMessages && i < contact.messages.Count - 1)
                sb.AppendLine("");
        }

        return sb.ToString().TrimEnd('\n', '\r');
    }

    public string RenderMessageBody(string contactId, string messageId)
    {
        var msg = GetMessage(contactId, messageId);
        if (msg == null)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"MESSAGE {messageId}");
        sb.AppendLine("");
        sb.AppendLine($"FROM    : {msg.from}");
        sb.AppendLine($"TO      : {msg.to}");
        sb.AppendLine($"DATE    : {msg.date}");
        sb.AppendLine($"STATUS  : {msg.status}");
        sb.AppendLine($"SUBJECT : {msg.subject}");
        sb.AppendLine("");
        sb.AppendLine(SeparatorLine());
        sb.AppendLine(msg.body);
        sb.AppendLine(SeparatorLine());
        sb.AppendLine("");

        if (showMessageMarkedAsReadNotice && msg.status == "UNREAD")
            sb.AppendLine("MESSAGE MARKED AS READ.");

        return sb.ToString().TrimEnd('\n', '\r');
    }

    [Serializable]
    public class MailContactData
    {
        public string id;
        public string name;
        public bool enableAIReply;
        public string aiProfileName;
        public string aiSystemPrompt;
        public string aiMemoryNote;
        public List<MailMessageData> messages = new List<MailMessageData>();
    }

    [Serializable]
    public class MailMessageData
    {
        public string id;
        public string date;
        public string from;
        public string to;
        public string status;
        public string subject;
        public string body;
    }
}