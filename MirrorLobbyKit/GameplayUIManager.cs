using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance;

    [Header("Assign in Inspector")]
    public Button backToLobbyBtn;
    public Transform playerListParent;    // ScrollView Content
    public GameObject playerEntryPrefab;  // Prefab with GameplayPlayerUIEntry on root

    // Maps the persistent NetworkPlayer to its row UI
    private readonly Dictionary<NetworkPlayer, GameplayPlayerUIEntry> entries
        = new Dictionary<NetworkPlayer, GameplayPlayerUIEntry>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        backToLobbyBtn.gameObject.SetActive(false);

        if (NetworkClient.localPlayer != null)
            Init();
        else
            NetworkClient.RegisterHandler<ReadyMessage>(_ => Init());
    }

    void Init()
    {
        var np = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
        if (np != null && np.isHost)
        {
            backToLobbyBtn.gameObject.SetActive(true);
            backToLobbyBtn.onClick.AddListener(() =>
            {
                Debug.Log("[GameplayUI] Back-to-Lobby clicked");
                np.CmdReturnToLobby();
            });
        }

        BuildPlayerList();
    }

    void BuildPlayerList()
    {
        // Destroy all existing rows
        foreach (var entry in entries.Values)
            Destroy(entry.gameObject);
        entries.Clear();

        // Spawn a new row for each NetworkPlayer
        foreach (var np in FindObjectsOfType<NetworkPlayer>())
        {
            GameObject row = Instantiate(playerEntryPrefab, playerListParent);
            var entry = row.GetComponent<GameplayPlayerUIEntry>();
            entry.Bind(np);
            entries[np] = entry;
        }
    }

    // Called by NetworkPlayer.OnPingChanged hook
    public static void UpdatePingFor(NetworkPlayer player)
    {
        if (Instance == null) return;

        if (Instance.entries.TryGetValue(player, out var entry))
        {
            entry.UpdatePingDisplay(player.pingMs);
        }
    }
}
