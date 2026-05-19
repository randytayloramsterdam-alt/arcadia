using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComputerChatUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField chatInputField;
    public TMP_Text chatOutputText;
    public Button sendButton;

    [Header("Chat Client")]
    public BackendChatClient chatClient;

    [Header("Messages")]
    public string processingText = "PROCESSING...";
    public string emptyInputWarning = "Input cannot be empty.";

    private bool isProcessing;

    void Start()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendButtonClicked);

        if (chatInputField != null)
            chatInputField.onEndEdit.AddListener(OnInputEndEdit);
    }

    void OnSendButtonClicked()
    {
        if (isProcessing) return;
        SendMessage();
    }

    void OnInputEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isProcessing) return;
            SendMessage();
        }
    }

    void SendMessage()
    {
        if (chatInputField == null || chatClient == null) return;

        string input = chatInputField.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            AppendOutput(emptyInputWarning);
            return;
        }

        StartCoroutine(SendAndDisplay(input));
    }

    IEnumerator SendAndDisplay(string message)
    {
        isProcessing = true;
        SetInputInteractable(false);
        AppendOutput($"> {message}");
        AppendOutput(processingText);

        bool done = false;
        string replyText = null;
        string errorText = null;

        yield return chatClient.SendMessage(
            message,
            reply =>
            {
                replyText = reply;
                done = true;
            },
            error =>
            {
                errorText = error;
                done = true;
            }
        );

        // 等待协程完成
        while (!done) yield return null;

        // 移除 PROCESSING... 行
        RemoveLastLine();

        if (!string.IsNullOrEmpty(errorText))
        {
            AppendOutput($"[ERROR] {errorText}");
        }
        else if (!string.IsNullOrEmpty(replyText))
        {
            AppendOutput($"> {replyText}");
        }

        isProcessing = false;
        SetInputInteractable(true);
        ClearInput();
    }

    void AppendOutput(string text)
    {
        if (chatOutputText == null) return;

        if (!string.IsNullOrEmpty(chatOutputText.text))
            chatOutputText.text += "\n";
        chatOutputText.text += text;
    }

    void RemoveLastLine()
    {
        if (chatOutputText == null) return;

        string text = chatOutputText.text;
        int lastNewline = text.LastIndexOf('\n');
        if (lastNewline >= 0)
            chatOutputText.text = text.Substring(0, lastNewline);
        else
            chatOutputText.text = "";
    }

    void SetInputInteractable(bool interactable)
    {
        if (chatInputField != null)
            chatInputField.interactable = interactable;
        if (sendButton != null)
            sendButton.interactable = interactable;
    }

    void ClearInput()
    {
        if (chatInputField != null)
            chatInputField.text = "";
    }
}