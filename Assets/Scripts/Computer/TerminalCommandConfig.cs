using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Arcadia/Terminal Command Config", fileName = "TerminalCommandConfig")]
public class TerminalCommandConfig : ScriptableObject
{
    [Header("Layout")]
    public int helpDescriptionWidth = 28;

    [Header("Commands")]
    public List<TerminalCommandEntry> commands;

    private Dictionary<TerminalCommandId, TerminalCommandEntry> entryMap;

    public void Initialize()
    {
        entryMap = new Dictionary<TerminalCommandId, TerminalCommandEntry>();
        foreach (var entry in commands)
        {
            if (entry != null)
                entryMap[entry.commandId] = entry;
        }
    }

    public TerminalCommandEntry GetEntry(TerminalCommandId id)
    {
        if (entryMap == null)
            Initialize();
        return entryMap.TryGetValue(id, out var entry) ? entry : null;
    }

    public bool Matches(TerminalCommandId id, string normalizedInput)
    {
        var entry = GetEntry(id);
        if (entry == null || string.IsNullOrEmpty(normalizedInput))
            return false;
        foreach (var alias in entry.aliases)
        {
            if (string.Equals(alias, normalizedInput, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public bool StartsWithAlias(TerminalCommandId id, string normalizedInput, out string remainingText)
    {
        remainingText = "";
        var entry = GetEntry(id);
        if (entry == null || string.IsNullOrEmpty(normalizedInput))
            return false;

        foreach (var alias in entry.aliases)
        {
            if (normalizedInput.Equals(alias, StringComparison.OrdinalIgnoreCase))
            {
                remainingText = "";
                return true;
            }

            string prefix = alias + " ";
            if (normalizedInput.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                remainingText = normalizedInput.Substring(prefix.Length);
                return true;
            }
        }
        return false;
    }

    public string GetAliasDisplay(TerminalCommandId id)
    {
        var entry = GetEntry(id);
        if (entry == null || entry.aliases == null || entry.aliases.Count == 0)
            return "";
        return string.Join(" / ", entry.aliases);
    }

    public static List<TerminalCommandEntry> GetDefaultCommands()
    {
        return new List<TerminalCommandEntry>
        {
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.Help,
                description = "SHOW THIS LIST",
                aliases = new List<string> { "HELP" },
                primaryAlias = "HELP",
                completionPriority = 80,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.List,
                description = "SHOW CURRENT CONTENT",
                aliases = new List<string> { "DIR", "LIST" },
                primaryAlias = "DIR",
                completionPriority = 30,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.OpenMail,
                description = "OPEN MAIL SYSTEM",
                aliases = new List<string> { "CD MAIL", "MAIL", "OPEN MAIL" },
                primaryAlias = "MAIL",
                completionPriority = 10,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.OpenDiary,
                description = "OPEN DIARY SYSTEM",
                aliases = new List<string> { "CD DIARY", "DIARY", "OPEN DIARY" },
                primaryAlias = "DIARY",
                completionPriority = 20,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.Back,
                description = "RETURN TO PREVIOUS LEVEL",
                aliases = new List<string> { "BACK", "RETURN" },
                primaryAlias = "BACK",
                completionPriority = 50,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.Clear,
                description = "CLEAR SCREEN",
                aliases = new List<string> { "CLEAR", "CLS" },
                primaryAlias = "CLEAR",
                completionPriority = 90,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.Exit,
                description = "CLOSE TERMINAL",
                aliases = new List<string> { "EXIT", "QUIT" },
                primaryAlias = "EXIT",
                completionPriority = 100,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.Refresh,
                description = "REFRESH CURRENT VIEW",
                aliases = new List<string> { "REFRESH", "RELOAD" },
                primaryAlias = "REFRESH",
                completionPriority = 40,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.GoRoot,
                description = "RETURN TO ROOT",
                aliases = new List<string> { "HOME", "ROOT", "MAIN" },
                primaryAlias = "HOME",
                completionPriority = 45,
                showInHelp = true,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.OpenItem,
                description = "OPEN ITEM",
                aliases = new List<string> { "CD", "OPEN" },
                primaryAlias = "OPEN",
                completionPriority = 10,
                showInHelp = false,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.ReadMessage,
                description = "READ MESSAGE",
                aliases = new List<string> { "READ" },
                primaryAlias = "READ",
                completionPriority = 10,
                showInHelp = false,
                showInCompletion = true
            },
            new TerminalCommandEntry
            {
                commandId = TerminalCommandId.SendMessage,
                description = "SEND MESSAGE",
                aliases = new List<string> { "SEND" },
                primaryAlias = "SEND",
                completionPriority = 20,
                showInHelp = false,
                showInCompletion = true
            }
        };
    }
}

[Serializable]
public class TerminalCommandEntry
{
    public TerminalCommandId commandId;
    public string description;
    public List<string> aliases;
    public bool showInHelp = true;

    public string primaryAlias;
    public int completionPriority = 100;
    public bool showInCompletion = true;

    public string GetPrimaryAlias()
    {
        if (!string.IsNullOrEmpty(primaryAlias))
            return primaryAlias;
        return aliases != null && aliases.Count > 0 ? aliases[0] : "";
    }
}