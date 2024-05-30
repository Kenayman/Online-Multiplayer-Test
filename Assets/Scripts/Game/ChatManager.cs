using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Singleton;

    [SerializeField] ChatMessage chatMessagePrefab;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] TMP_InputField chatInputField;

    public string playerName;

    void Awake() => ChatManager.Singleton = this;

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return)))
        {
            SendChatMessage(chatInputField.text, playerName);
            chatInputField.text = "";
        }
    }


    public void SendChatMessage(string message, string fromWho = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        string S = fromWho + ": " + message;
        SendChatMessageServerRpc(S);
    }


    void AddMessage(string message)
    {
        ChatMessage chatMessage = Instantiate(chatMessagePrefab, chatContent.transform);
        chatMessage.SetText(message);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRpc(string message)
    {
        RecieveChatMessageClientRpc(message);
    }

    [ClientRpc]
    void RecieveChatMessageClientRpc(string message)
    {
        ChatManager.Singleton.AddMessage(message);
    }
}
