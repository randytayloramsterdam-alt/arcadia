using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BackendChatClient : MonoBehaviour
{
    [Header("Backend")]
    [Tooltip("后端地址")]
    public string baseUrl = "http://127.0.0.1:8000";

    [Tooltip("聊天端点路径")]
    public string chatEndpoint = "/chat";

    /// <summary>
    /// 发送消息到后端，返回 reply。
    /// </summary>
    /// <param name="message">玩家输入的消息</param>
    /// <param name="onSuccess">成功回调，参数为后端返回的 reply</param>
    /// <param name="onError">失败回调，参数为错误信息</param>
    public IEnumerator SendMessage(string message, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            onError?.Invoke("Input is empty.");
            yield break;
        }

        // 构造请求体
        var requestBody = new ChatRequest { message = message };
        string jsonBody = JsonUtility.ToJson(requestBody);

        // 创建 POST 请求
        string url = baseUrl.TrimEnd('/') + chatEndpoint;
        using (var request = new UnityWebRequest(url, "POST"))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.Send();

            // 检查网络错误
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg;
                if (request.responseCode == 0)
                    errorMsg = "Cannot connect to server. Make sure the backend is running.";
                else
                    errorMsg = $"Server error ({request.responseCode}): {request.error}";

                onError?.Invoke(errorMsg);
                yield break;
            }

            // 解析 JSON
            string responseText = request.downloadHandler.text;
            try
            {
                var response = JsonUtility.FromJson<ChatResponse>(responseText);
                if (string.IsNullOrEmpty(response.reply))
                {
                    onError?.Invoke("Empty reply from server.");
                    yield break;
                }
                onSuccess?.Invoke(response.reply);
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Failed to parse response: {ex.Message}\nRaw: {responseText}");
            }
        }
    }

    [Serializable]
    private class ChatRequest
    {
        public string message;
    }

    [Serializable]
    private class ChatResponse
    {
        public string reply;
    }
}