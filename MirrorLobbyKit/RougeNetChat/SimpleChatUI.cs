// FILE: SimpleChatUI.cs
// PURPOSE: very small UI handler – single text block + input field.

using Mirror;
using TMPro;
using UnityEngine;

public class SimpleChatUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public TMP_InputField inputField;
    public TMP_Text logText;

    static SimpleChatUI _instance;

    void Awake() => _instance = this;

    // ------------ static helper so NetworkPlayer can call Append() -----
    public static void Append(string line)
    {
        if (_instance == null) return;

        _instance.logText.text += line + "\n";

        // optional: scroll to bottom if using a ScrollRect
    }

    // ------------ user hits Enter --------------------------------------
    void Start()
    {
        inputField.onSubmit.AddListener(OnSubmit);
    }

    void OnSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // get local NetworkPlayer and send Command
        var np = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (np) np.CmdSendChat(text);

        inputField.text = "";
        inputField.ActivateInputField(); // keep focus
    }
}
