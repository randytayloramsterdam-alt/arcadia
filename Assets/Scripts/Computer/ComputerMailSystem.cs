using System;
using System.Collections.Generic;
using UnityEngine;

public class ComputerMailSystem : MonoBehaviour
{
    [Header("Contacts")]
    public List<MailContactData> contacts = new List<MailContactData>();

    private Dictionary<string, MailContactData> contactMap;

    public void Initialize()
    {
        BuildTestData();
        contactMap = new Dictionary<string, MailContactData>();
        foreach (var contact in contacts)
            contactMap[contact.id] = contact;
    }

    private void BuildTestData()
    {
        contacts = new List<MailContactData>
        {
            new MailContactData
            {
                id = "001",
                name = "A. MORRISON",
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

    public string GetContactName(string contactId)
    {
        var contact = GetContact(contactId);
        return contact != null ? contact.name : "";
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
            from = "LOCAL USER",
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
            to = "LOCAL USER",
            status = "UNREAD",
            subject = subject,
            body = body
        };

        contact.messages.Add(msg);
        return msg;
    }

    public string RenderContactList()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("");
        sb.AppendLine("MAIL CONTACTS");
        sb.AppendLine("");

        foreach (var contact in contacts)
        {
            string status = ComputeContactStatus(contact.id);
            string lastDate = ComputeContactLastDate(contact.id);
            string line = $"[{contact.id}] {contact.name,-23} {status,-8} LAST: {lastDate}";
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
        sb.AppendLine("");
        sb.AppendLine($"CONTACT: {contact.name}");
        sb.AppendLine("");

        foreach (var msg in contact.messages)
        {
            string from = msg.from.Length > 12 ? msg.from.Substring(0, 12) : msg.from;
            string line = $"[{msg.id}] {msg.date}    FROM: {from,-12} {msg.status}";
            sb.AppendLine(line);
            sb.AppendLine($"      SUBJECT: {msg.subject}");
        }

        return sb.ToString().TrimEnd('\n', '\r');
    }

    public string RenderMessageBody(string contactId, string messageId)
    {
        var msg = GetMessage(contactId, messageId);
        if (msg == null)
            return "";

        bool wasUnread = msg.status == "UNREAD";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("");
        sb.AppendLine($"MESSAGE {messageId}");
        sb.AppendLine("");
        sb.AppendLine($"FROM    : {msg.from}");
        sb.AppendLine($"TO      : {msg.to}");
        sb.AppendLine($"DATE    : {msg.date}");
        sb.AppendLine($"STATUS  : {msg.status}");
        sb.AppendLine($"SUBJECT : {msg.subject}");
        sb.AppendLine("");
        sb.AppendLine("-----------------------------------------------------");
        sb.AppendLine(msg.body);
        sb.AppendLine("-----------------------------------------------------");
        sb.AppendLine("");

        if (wasUnread)
            sb.AppendLine("MESSAGE MARKED AS READ.");

        return sb.ToString().TrimEnd('\n', '\r');
    }

    [Serializable]
    public class MailContactData
    {
        public string id;
        public string name;
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