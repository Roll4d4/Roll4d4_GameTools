using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayPlayerUIEntry : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public TMP_Text nameText;
    public Image pingIcon;

    private NetworkPlayer player;

    public void Bind(NetworkPlayer networkPlayer)
    {
        player = networkPlayer;
        nameText.text = player.playerName;
    }

    void Update()
    {
        if (player == null) return;

        float ping = EstimatePing(player);
        pingIcon.color = PingToColor(ping);
    }

    public void UpdatePingDisplay(int ping)
    {
        pingIcon.color = PingToColor(ping);
    }


    float EstimatePing(NetworkPlayer p)
    {
        if (p.isLocalPlayer && NetworkServer.active)
        {
            // Host: local client + server = best possible ping
            return 10f;
        }

        return (float)(NetworkTime.rtt * 1000f);
    }

    public static void UpdatePingFor(NetworkPlayer player)
    {
     
    }

    public bool IsBoundTo(NetworkPlayer target) => player == target;


    Color PingToColor(float ms)
    {
        float t = Mathf.InverseLerp(300f, 0f, ms); // green = good, red = bad
        return Color.Lerp(Color.red, Color.green, t);
    }
}
