using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Arcadia/Mail System Config", fileName = "MailSystemConfig")]
public class MailSystemConfig : ScriptableObject
{
    [Header("System")]
    public string currentMailDate = "1983-10-07";
    public string localUserName = "LOCAL USER";

    [Header("Contacts")]
    public List<MailContactConfig> contacts = new List<MailContactConfig>();
}

[Serializable]
public class MailContactConfig
{
    public string id;
    public string displayName;
    public bool enableAIReply;
    public string aiProfileName;
    [TextArea(3, 10)] public string aiSystemPrompt;
    [TextArea(3, 10)] public string aiMemoryNote;
    public List<MailMessageConfig> initialMessages = new List<MailMessageConfig>();
}

[Serializable]
public class MailMessageConfig
{
    public string id;
    public string date;
    public string from;
    public string to;
    public MailMessageStatus status = MailMessageStatus.UNREAD;
    public string subject;
    [TextArea(3, 12)] public string body;
}