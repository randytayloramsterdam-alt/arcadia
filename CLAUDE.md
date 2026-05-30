# CLAUDE.md — ARCADIA Computer Terminal System Design Guide

> This file is the global design and implementation guide for Claude Code when working on the in-game computer terminal system.
> Unity version: 2022.3.62f3c1
> Project style: 3D low-poly / low-fi psychological horror game.
> Current target: implement the terminal UI and mail system first. Diary system is reserved for later.

---

## 0. Core Principle

The computer system is an in-game old terminal interface inspired by 1980s command-line operating systems such as IBM PC DOS / CP/M-style text interfaces.

However, it is not required to be a historically accurate DOS emulator.

It should be a **gameplay-friendly pseudo-command-line system**:

- Looks like an old computer terminal.
- Uses typed commands.
- Uses fixed hierarchy prompts such as `ARCADIA:\>`.
- Supports simple, forgiving command input.
- Later can add clickable UI, autocomplete, and shortcut buttons.
- Current phase should focus on pure command-line operation.

---

## 1. Current Confirmed Terminal Hierarchy

The current terminal hierarchy is fixed as follows:

```text
Main menu level:
ARCADIA:\>

Mail system level:
ARCADIA:\MAIL>

Diary system level:
ARCADIA:\DIARY>

Mail contact level:
ARCADIA:\MAIL\Name>

Mail message level:
ARCADIA:\MAIL\Name\ID>
```

Examples:

```text
ARCADIA:\>
ARCADIA:\MAIL>
ARCADIA:\DIARY>
ARCADIA:\MAIL\E.BENSON>
ARCADIA:\MAIL\E.BENSON\003>
```

Important rule:

- Commands must be interpreted according to the current terminal level.
- The same input can mean different things at different levels.
- Example:
  - At `ARCADIA:\MAIL>`, input `003` means entering contact `[003]`.
  - At `ARCADIA:\MAIL\E.BENSON>`, input `003` means entering message `[003]`.
  - At `ARCADIA:\MAIL\E.BENSON\003>`, input `003` should be invalid unless explicitly supported later.

---

## 2. Confirmed Global Commands

These commands should work at the appropriate levels.

### 2.1 Help

```text
HELP
```

Function:

- Show available commands for the current level.
- Later can show contextual help.

### 2.2 List Content

```text
DIR
LIST
```

Function:

- At main level: show available systems.
- At mail level: show contact list.
- At contact level: show message list for that contact.
- At message level: can either re-display current message or show invalid command depending on implementation choice.

### 2.3 Enter Mail System

Supported at main level:

```text
MAIL
CD MAIL
OPEN MAIL
```

Result:

```text
ARCADIA:\MAIL>
```

Upon entering, automatically show the mail contact list.

### 2.4 Enter Diary System

Supported at main level:

```text
DIARY
CD DIARY
OPEN DIARY
```

Result:

```text
ARCADIA:\DIARY>
```

Diary system is not implemented in the current phase. It can show placeholder text:

```text
DIARY SYSTEM NOT AVAILABLE.
```

### 2.5 Back / Return

```text
BACK
RETURN
```

Function:

```text
ARCADIA:\MAIL\E.BENSON\003> BACK
=> ARCADIA:\MAIL\E.BENSON>

ARCADIA:\MAIL\E.BENSON> BACK
=> ARCADIA:\MAIL>

ARCADIA:\MAIL> BACK
=> ARCADIA:\>

ARCADIA:\DIARY> BACK
=> ARCADIA:\>
```

### 2.6 Clear Screen

```text
CLEAR
CLS
```

Function:

- Clear terminal output.
- Keep the current hierarchy prompt.

### 2.7 Exit Computer

```text
EXIT
QUIT
```

Function:

- Close the computer UI and return control to the 3D game.
- Should call the existing computer UI close flow instead of only hiding terminal text.

### 2.8 Unknown Command

Any unsupported command should output exactly:

```text
BAD COMMAND OR FILE NAME.
```

---

## 3. Mail System Design

The mail system has multiple levels:

```text
ARCADIA:\MAIL>
```

Shows contact list.

```text
ARCADIA:\MAIL\Name>
```

Shows all messages exchanged with that contact.

```text
ARCADIA:\MAIL\Name\ID>
```

Shows a specific message.

The mail system is not a single flat inbox. It is a **contact-based message thread system**.

---

## 4. Mail Contact List Level

Prompt:

```text
ARCADIA:\MAIL>
```

Entering this level automatically displays the contact list. No extra `DIR` command is required, although `DIR` and `LIST` should re-display it.

### 4.1 Contact List Display Format

Use this format:

```text
MAIL CONTACTS

[001] A. MORRISON          READ        LAST: 1983-10-04
[002] L. CARTER            UNREAD      LAST: 1983-10-05
[003] E. BENSON            UNREAD      LAST: 1983-10-07
[004] M. KELLER            READ        LAST: 1983-09-29
[005] J. REED              READ        LAST: 1983-09-18
```

### 4.2 Contact List Fields

```text
ID       Contact ID used by commands.
CONTACT  Contact name displayed to the player.
STATUS   READ or UNREAD only.
LAST     Date of latest message in that contact's thread.
```

Contact status rule:

- If any message under this contact has status `UNREAD`, contact status is `UNREAD`.
- Otherwise contact status is `READ`.

No `LOCKED`, `AIABLE`, or system contact type should be shown in the UI at this stage.

### 4.3 Commands at Mail Contact List Level

At `ARCADIA:\MAIL>`, entering a contact supports:

```text
CD XXX
XXX
OPEN XXX
```

Where `XXX` is the contact ID, such as:

```text
CD 003
003
OPEN 003
```

These should enter:

```text
ARCADIA:\MAIL\E.BENSON>
```

Also supported:

```text
DIR
LIST
BACK
RETURN
CLEAR
CLS
EXIT
QUIT
HELP
```

Invalid command:

```text
BAD COMMAND OR FILE NAME.
```

---

## 5. Mail Contact Thread Level

Prompt example:

```text
ARCADIA:\MAIL\E.BENSON>
```

Entering this level automatically displays the message list for that contact. No extra `DIR` command is required, although `DIR` and `LIST` should re-display it.

### 5.1 Message List Display Format

Use this format:

```text
CONTACT: E. BENSON

[001] 1983-10-02    FROM: E. BENSON       READ
      SUBJECT: ARE YOU SAFE?

[002] 1983-10-03    FROM: LOCAL USER      SENT
      SUBJECT: RE: ARE YOU SAFE?

[003] 1983-10-07    FROM: E. BENSON       UNREAD
      SUBJECT: PLEASE ANSWER
```

### 5.2 Message Status

Message statuses for now:

```text
READ
UNREAD
SENT
```

Meaning:

- `READ`: incoming message already read.
- `UNREAD`: incoming message not yet read.
- `SENT`: outgoing message created by LOCAL USER.

### 5.3 Commands at Contact Thread Level

At `ARCADIA:\MAIL\Name>`, entering a specific message supports:

```text
CD XXX
XXX
OPEN XXX
READ XXX
```

Where `XXX` is the message ID, such as:

```text
CD 003
003
OPEN 003
READ 003
```

These should enter:

```text
ARCADIA:\MAIL\E.BENSON\003>
```

Also supported:

```text
SEND message content...
DIR
LIST
BACK
RETURN
CLEAR
CLS
EXIT
QUIT
HELP
```

Invalid command:

```text
BAD COMMAND OR FILE NAME.
```

---

## 6. Mail Message Level

Prompt example:

```text
ARCADIA:\MAIL\E.BENSON\003>
```

Entering this level automatically displays the full message body.

### 6.1 Full Message Display Format

Recommended display:

```text
MESSAGE 003

FROM    : E. BENSON
TO      : LOCAL USER
DATE    : 1983-10-07
STATUS  : UNREAD
SUBJECT : PLEASE ANSWER

-----------------------------------------------------
Please answer me.

I know you are there.
-----------------------------------------------------

MESSAGE MARKED AS READ.
```

Reading rule:

- If the message status is `UNREAD`, entering/reading the message should mark it as `READ`.
- After marking it read, the parent contact's status should update automatically if no unread messages remain.

### 6.2 Commands at Message Level

Supported:

```text
BACK
RETURN
CLEAR
CLS
EXIT
QUIT
HELP
```

Optional later:

```text
NEXT
PREV
REPLY
```

Do not implement optional commands unless explicitly requested.

Invalid command:

```text
BAD COMMAND OR FILE NAME.
```

---

## 7. SEND Command Design

The `SEND` command works only at the mail contact thread level:

```text
ARCADIA:\MAIL\E.BENSON> SEND Are you still there?
```

### 7.1 SEND Format

```text
SEND message content...
```

Everything after `SEND` is treated as the player's outgoing email body.

If the player types only `SEND` with no content, output an error such as:

```text
EMPTY MESSAGE.
```

### 7.2 Outgoing Message Creation

When the player sends a message:

- Create a new message under the current contact.
- Assign the next available message ID.
- `FROM` should be `LOCAL USER`.
- `TO` should be the current contact name.
- `STATUS` should be `SENT`.
- `DATE` should use the current in-game date if available. If not available, use a placeholder such as `--/--/----` for now.
- `SUBJECT` should be automatically generated from the body.

### 7.3 Subject Generation Rule

For preset game emails, subject is manually authored.

For player-sent emails:

- If the message has content, use the first part of the body.
- Suggested rule:
  - Use first 24 characters.
  - Trim whitespace.
  - If longer than 24 characters, append `...`.
- If empty, use:

```text
NO SUBJECT
```

Example:

```text
SEND Are you still there?
```

Creates:

```text
SUBJECT: Are you still there?
```

Example:

```text
SEND I found something behind the sealed door and I don't know what it means.
```

Creates:

```text
SUBJECT: I found something behind...
```

### 7.4 AI Reply Rule

Most contacts contain preset historical emails only.

Some special contacts may later receive AI-generated replies after the player sends email. This should not be exposed in the UI with fields such as `AIABLE`.

Implementation note:

- Do not show whether a contact can reply.
- If a contact has no reply behavior, simply no reply appears.
- If a contact has AI reply behavior, a new incoming message may appear later.
- Current first implementation can create only the player's outgoing message and not implement AI replies yet.

---

## 8. Data Structure Recommendation

The user is not required to manually design the technical data structure. Claude should implement a clean structure based on this design.

Recommended C# concepts:

### 8.1 MailContactData

A contact should contain at least:

```text
id
name
messages
```

Recommended fields:

```csharp
public class MailContactData
{
    public string id;
    public string name;
    public List<MailMessageData> messages;
}
```

The contact status should be computed automatically:

```text
UNREAD if any message.status == UNREAD
otherwise READ
```

The contact last entry date should be computed from the latest message.

### 8.2 MailMessageData

A message should contain at least:

```text
id
date
from
to
status
subject
body
```

Recommended fields:

```csharp
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
```

For the first version, using strings for status is acceptable:

```text
READ
UNREAD
SENT
```

Later this can be replaced by enum if needed.

### 8.3 Data Storage for First Version

For the first prototype:

- It is acceptable to hard-code sample contact and message data in C#.
- Do not over-engineer JSON / ScriptableObject unless requested.
- Keep the system easy to test and modify.
- Later, data can be moved into ScriptableObjects or JSON files.

---

## 9. UI / Terminal Output Rules

### 9.1 General Output Flow

Recommended behavior:

- Boot sequence plays first.
- After boot, enter `ARCADIA:\>`.
- Entering a new level clears or refreshes the terminal display and shows that level's content automatically.
- `DIR` / `LIST` re-displays current level content.
- `CLEAR` / `CLS` clears output but keeps current level.
- `BACK` returns to previous level and displays that level's content.
- `EXIT` closes the computer UI.

### 9.2 Input Parsing Rules

Input should be forgiving:

- Case-insensitive.
- Trim leading and trailing spaces.
- Collapse or tolerate multiple spaces.
- Commands are English only for now.
- IDs are formatted as three digits, such as `001`, `002`, `003`.
- Numeric ID input without command should be accepted only at:
  - `ARCADIA:\MAIL>` for contact entry.
  - `ARCADIA:\MAIL\Name>` for message entry.

### 9.3 Do Not Implement Yet

Do not implement these unless explicitly requested later:

- Diary system details.
- Attachments.
- Search.
- Multi-line email compose mode.
- Deleting messages.
- Reply / forward commands.
- Mouse clicking.
- Autocomplete.
- AI replies.
- Save/load integration.
- Final puzzle integration.

---

## 10. Recommended Script Responsibility

Do not put all logic in one giant script.

Recommended split:

```text
ComputerUIController
- Existing open/close UI behavior.
- Should not be broken.

ComputerBootSequence
- Boot animation text.
- Calls terminal ready after boot.

ComputerTerminalController
- Handles input field.
- Parses commands.
- Tracks current terminal level.
- Updates prompt text.
- Sends mail-related commands to ComputerMailSystem.

ComputerMailSystem
- Stores contacts and messages.
- Renders contact list.
- Renders message list.
- Renders message body.
- Handles READ behavior.
- Handles SEND behavior.

Optional later:
ComputerDiarySystem
ComputerTerminalAudio
ComputerCommandAutocomplete
```

---

## 11. Implementation Priorities

Build this system step by step.

### Phase 1 — Terminal Skeleton

Implement:

```text
Boot finishes
Prompt appears
Input command
Output text
HELP
CLEAR / CLS
EXIT / QUIT
BAD COMMAND OR FILE NAME.
```

### Phase 2 — Navigation

Implement:

```text
ARCADIA:\>
MAIL / CD MAIL / OPEN MAIL
DIARY placeholder
BACK / RETURN
DIR / LIST
Prompt switching
```

### Phase 3 — Mail Contacts

Implement:

```text
ARCADIA:\MAIL>
Automatic contact list display
Contact entry by 001 / CD 001 / OPEN 001
```

### Phase 4 — Mail Messages

Implement:

```text
ARCADIA:\MAIL\E.BENSON>
Automatic message list display
Message entry by 001 / CD 001 / OPEN 001 / READ 001
UNREAD -> READ when opened
Contact status auto-updates
```

### Phase 5 — SEND

Implement:

```text
SEND message content...
Create SENT message under current contact
Auto-generate subject
Refresh message list
```

### Phase 6 — AI Reply Integration

Do not implement until requested.

---

## 12. Visual Style Notes

The UI should reference early 1980s command-line systems.

Style reference:

```text
Black background
White or pale green text
Monospace bitmap font
Compact spacing
No modern chat bubbles
No rounded modern panels
Command prompt at bottom
```

Recommended English fonts to test:

```text
Perfect DOS VGA 437
PxPlus IBM VGA8
```

Chinese localization can be considered later with fonts such as:

```text
Zpix
GNU Unifont
```

Current phase is English only.

---

## 13. Confirmed Sample Data

Use this contact list for the first mail prototype:

```text
MAIL CONTACTS

[001] A. MORRISON          READ        LAST: 1983-10-04
[002] L. CARTER            UNREAD      LAST: 1983-10-05
[003] E. BENSON            UNREAD      LAST: 1983-10-07
[004] M. KELLER            READ        LAST: 1983-09-29
[005] J. REED              READ        LAST: 1983-09-18
```

Use this message list format for contact threads:

```text
CONTACT: E. BENSON

[001] 1983-10-02    FROM: E. BENSON       READ
      SUBJECT: ARE YOU SAFE?

[002] 1983-10-03    FROM: LOCAL USER      SENT
      SUBJECT: RE: ARE YOU SAFE?

[003] 1983-10-07    FROM: E. BENSON       UNREAD
      SUBJECT: PLEASE ANSWER
```

---

## 14. Safety / Preservation Rules

When modifying the Unity project:

- Do not modify unrelated player controller scripts.
- Do not modify existing 3D interaction logic unless explicitly requested.
- Do not break existing `ComputerUIController` open/close behavior.
- Do not auto-generate or replace the user's manually designed UI unless requested.
- Use Inspector references for UI fields.
- Keep debug logs behind an Inspector `enableDebugLogs` toggle if logs become frequent.
- Preserve existing naming as much as possible.
- Ask before deleting or replacing existing files.
- Avoid editing main scene if the task can be done through prefabs or scripts.
