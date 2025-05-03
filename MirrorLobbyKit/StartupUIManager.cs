using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;
public class StartupUIManager : MonoBehaviour
{
    [Header("Assign these in the Inspector")]
    public TMP_InputField addressField;
    public TMP_InputField portField;      // new!
    public TMP_InputField userName;
    public Button hostButton;
    public Button joinButton;

    void Start()
    {
        // —— Defaults ——
        if (string.IsNullOrWhiteSpace(addressField.text))
            addressField.text = "127.0.0.1";
        if (string.IsNullOrWhiteSpace(portField.text))
            portField.text = "7777";

        hostButton.onClick.AddListener(() =>
        {
            SetPlayerName();
            ApplyNetworkPort();
            NetworkManager.singleton.StartHost();
        });

        joinButton.onClick.AddListener(() =>
        {
            Debug.Log("[StartupUI] Join clicked");
            SetPlayerName();
            ApplyNetworkPort();
            NetworkManager.singleton.networkAddress = addressField.text;
            NetworkManager.singleton.StartClient();
        });

    }

    private void SetPlayerName()
    {
      /*  var nameInput = userName.text;
        PlayerInfoCache.PlayerName = string.IsNullOrWhiteSpace(nameInput)
            ? "Player" + Random.Range(1000, 9999)
            : nameInput.Trim();*/
    }

    private void ApplyNetworkPort()
    {
        // default port
        int port = 7777;

        // only override if the field is really wired up
        if (portField != null &&
            int.TryParse(portField.text, out var parsed))
        {
            port = parsed;
        }

        if (NetworkManager.singleton.transport is TelepathyTransport tele)
        {
            tele.port = (ushort)port;
            Debug.Log($"[StartupUI] Set transport port to {port}");
        }
        else
        {
            Debug.LogWarning("[StartupUI] Can't set port, transport is not Telepathy");
        }
    }

}

