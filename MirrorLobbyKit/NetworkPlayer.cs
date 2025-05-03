using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))] public string playerName;
    [SyncVar(hook = nameof(OnReadyChanged))] public bool isReady;
    [SyncVar(hook = nameof(OnHostChanged))] public bool isHost;
    [SyncVar(hook = nameof(OnPingChanged))] public int pingMs = -1;

    void OnPingChanged(int oldVal, int newVal) => GameplayUIManager.UpdatePingFor(this); // optional helper to update UI
    
    void OnNameChanged(string _old, string _new) => lobbyRowGO?.GetComponent<PlayerLobbyUIEntry>()?.Refresh();
    void OnReadyChanged(bool _old, bool _new) => lobbyRowGO?.GetComponent<PlayerLobbyUIEntry>()?.Refresh();
    void OnHostChanged(bool _old, bool _new) => lobbyRowGO.GetComponent<PlayerLobbyUIEntry>().Bind(this);


    float pingTimer;

    void Update()
    {
        if (!isLocalPlayer) return;

        pingTimer += Time.deltaTime;
        if (pingTimer >= 1f)
        {
            pingTimer = 0f;

            int newPing = Mathf.RoundToInt((float)(NetworkTime.rtt * 1000f));
            if (pingMs != newPing)
            {
                CmdReportPing(newPing);
            }
        }
    }

    [Command]
    void CmdReportPing(int value)
    {
        pingMs = value;
    }


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }



    // ─────────────────────────  Sync’d profile data  ──────────────────────

    [SyncVar] public int xpLevel;
  
    

    // ─────────────────────────  Inspector refs  ───────────────────────────
    [Header("Lobby‑row prefab (UI)")]
    [Tooltip("Drag the PlayerLobbyUIEntry prefab here")]
    public GameObject lobbyRowPrefab;

    // ─────────────────────────  Runtime refs  ─────────────────────────────
    GameObject lobbyRowGO;  // instantiated UI row

    // ─────────────────────────  Local‑player init  ────────────────────────
    public override void OnStartLocalPlayer()
    {
        CmdInitProfile(LocalPlayerCache.Name, 1);
    }

   

    #region ─────────── Server‑only Commands ────────────────────────────────
    [Command]
    void CmdInitProfile(string name, int level)
    {
        playerName = string.IsNullOrWhiteSpace(name) ? $"Player{connectionToClient.connectionId}" : name;
        xpLevel = level;
        isHost = NetworkServer.connections.Count == 1;
    }

    [Command] public void CmdToggleReady() => isReady = !isReady;

    [Command]
    public void CmdStartGame()
    {
        if (!isHost) return;
        ((CustomNetworkManager)NetworkManager.singleton).StartGameplayScene();
    }

    [Command]
    public void CmdReturnToLobby()
    {
        if (!isHost) return;                 // only host allowed
        ((CustomNetworkManager)NetworkManager.singleton).ReturnToLobbyScene();
    }
    #endregion

    // ─────────────────────────  Scene‑watch  ──────────────────────────────
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // run once for the current scene
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDisable() =>
        SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // turn path or name into a short scene name
        string shortName = Path.GetFileNameWithoutExtension(scene.name);

        if (shortName.Contains("Lobby"))
            SpawnLobbyRow();
        else
            DestroyLobbyRow();
    }

    // ─────────────────────────  Lobby‑row helpers  ────────────────────────
    void SpawnLobbyRow()
    {
        if (lobbyRowGO != null) return;
        if (lobbyRowPrefab == null || LobbyUIRegistry.PlayerListParent == null) return;

        lobbyRowGO = Instantiate(
            lobbyRowPrefab,
            LobbyUIRegistry.PlayerListParent);
        StartCoroutine(BindRowWhenReady());   // bind after SyncVars arrive
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

    }

    System.Collections.IEnumerator BindRowWhenReady()
    {
        // wait until name arrives (Mirror sends SyncVars on first tick)
        while (string.IsNullOrEmpty(playerName))
            yield return null;            // wait one frame

        // if row already spawned, bind once with real data
        if (lobbyRowGO)
            lobbyRowGO.GetComponent<PlayerLobbyUIEntry>().Bind(this);
    }

    void DestroyLobbyRow()
    {
        if (lobbyRowGO) Destroy(lobbyRowGO);
        lobbyRowGO = null;
    }

    // -------- Client → Server ------------------------------------------
    [Command]
    public void CmdSendChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        // prepend sender name
        string line = $"{playerName}: {message.Trim()}";
        RpcReceiveChat(line);
    }

    // -------- Server → Everyone ----------------------------------------
    [ClientRpc]
    void RpcReceiveChat(string line)
    {
        SimpleChatUI.Append(line);
    }
}

public static class LocalPlayerCache
{
    // Set this from your Main‑menu UI before you join/host
    public static string Name = "Player" + UnityEngine.Random.Range(1000, 9999);
}