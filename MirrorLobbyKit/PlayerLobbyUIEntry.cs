using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyUIEntry : MonoBehaviour
{
    public TMP_Text nameText;
    public Image icon;

    private NetworkPlayer player;
    public Button actionButton;
 
    public void Refresh()
    {
        if (!player) return;

        // name / tick / host icon
        nameText.text = player.playerName + (player.isReady ? " ✔" : "");
        icon.color = player.isHost ? Color.yellow : Color.white;

        // buttons
        bool isMine = NetworkClient.localPlayer &&
                      player.netId == NetworkClient.localPlayer.netId;

        if (isMine && player.isHost)
        {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<TMP_Text>().text = "Start";
        }
        else if (isMine)
        {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<TMP_Text>().text = "Ready";
        }
       
    }



    // File: PlayerLobbyUIEntry.cs
    // Function: public void Bind(NetworkPlayer customPlayer)

    public void Bind(NetworkPlayer customPlayer)
    {
       
        player = customPlayer;

        // 🔍 DEBUG #1: Entry into Bind
        Debug.Log($"[Bind] ▶▶▶ Called Bind() for netId={player.netId} name={player.playerName} host={player.isHost}");

        actionButton.onClick.RemoveAllListeners();

        bool isMine = player != null
            && NetworkClient.localPlayer != null
            && player.netId == NetworkClient.localPlayer.netId;

        // 🔍 DEBUG #2: isMine / isHost evaluation
        Debug.Log($"[Bind]    isMine={isMine}  isHost={player.isHost}");

        if (isMine && player.isHost)
        {
            Debug.Log($"[Bind]    Setting START button for {player.playerName}");
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<TMP_Text>().text = "Start";
            actionButton.onClick.AddListener(() =>
            {
                Debug.Log("[Bind] ▶ START clicked");
                if (CustomLobbySystem.AllPlayersReady())
                {
                    Debug.Log("[Bind] ▶ All ready, calling CmdStartGame()");
                    player.CmdStartGame();   // <— use the Command
                }
                else
                {
                    Debug.Log("[Bind] ▶ Not all players ready");
                }
            });
        }
        else if (isMine)
        {
            Debug.Log($"[Bind]    Setting READY button for {player.playerName}");
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<TMP_Text>().text = "Ready";
            actionButton.onClick.AddListener(() =>
            {
                Debug.Log("[Bind] ▶ READY clicked, calling CmdToggleReady()");
                player.CmdToggleReady();  // <— use the Command
            });
        }
       

        // 🔍 DEBUG #3: After wiring, refresh the visuals
        UpdateUI();
        Debug.Log($"[Bind]    Finished Bind() for {player.playerName}");
    }



    private void UpdateUI()
    {
        if (nameText != null)
            nameText.text = player.playerName + (player.isReady ? " ✔" : "");

        if (icon != null)
            icon.color = player.isHost ? Color.yellow : Color.white;
    }

}
