using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class CustomNetworkManager : NetworkManager
{
    [Header("Scene Paths (pick in Inspector)")]
    public string gameplayScenePath = "Assets/Scenes/Gameplay/GameplayScene.unity";

    [Header("Player Prefabs")]
    public GameObject networkPlayerPrefab;
    public GameObject gameplayPlayerPrefab;

    // ── cached short names ───────────────────────────────────────────────
    string lobbySceneShort;
    string gameplaySceneShort;
    // ─────────────────────────────────────────────────────────────────────

    // --------------------------------------------------------------------
    // 0. Awake  (runs in Splash)
    // --------------------------------------------------------------------
    public override void Awake()
    {
        base.Awake();
        lobbySceneShort = ShortName(onlineScene);
        gameplaySceneShort = ShortName(gameplayScenePath);
        Debug.Log($"[0] Awake  | onlineScene = {onlineScene}  →  {lobbySceneShort}");
        Debug.Log($"[0] Awake  | gameplayScenePath = {gameplayScenePath}  →  {gameplaySceneShort}");
    }

    // --------------------------------------------------------------------
    // 1. Host started
    // --------------------------------------------------------------------
    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("[1] OnStartHost  (Mirror will now auto‑load Lobby)");
    }

    // --------------------------------------------------------------------
    // 2. Client connects (fires on both host‑side & join‑side)
    // --------------------------------------------------------------------
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log($"[2] OnClientConnect  | isServer={NetworkServer.active}  | ready={NetworkClient.ready}");

        if (!NetworkClient.ready)
        {
            NetworkClient.Ready();
            Debug.Log("[2]   → Sent Ready()");
        }

        if (!NetworkServer.active)   // join‑client
        {
            NetworkClient.AddPlayer();
            Debug.Log("[2]   → Sent AddPlayer()");
        }
    }

    // --------------------------------------------------------------------
    // 3. Server gets Add‑Player message
    // --------------------------------------------------------------------
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[3] OnServerAddPlayer  | connId={conn.connectionId}  | identity null? {conn.identity == null}");

        if (conn.identity != null)
        {
            Debug.Log("[3]   → ABORT (already has identity)");
            return;
        }

        GameObject obj = Instantiate(networkPlayerPrefab);
        var lp = obj.GetComponent<NetworkPlayer>();
        lp.isHost = numPlayers == 0;

        NetworkServer.AddPlayerForConnection(conn, obj);
        Debug.Log("[3]   → Spawned LobbyPlayer prefab");
    }

    // --------------------------------------------------------------------
    // 4. Scene finished loading on server
    // --------------------------------------------------------------------
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        // normalise to short file‑name
        string shortScene = Path.GetFileNameWithoutExtension(sceneName);
        Debug.Log($"[SceneChanged]  full=\"{sceneName}\"  short=\"{shortScene}\"");

        // ────────────────────────────  LOBBY  ─────────────────────────────
        if (shortScene == lobbySceneShort)
        {
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                // If we’re coming back from Gameplay, conn may still hold a GameplayAvatar.
                // We ONLY care whether the connection already has our persistent player object.
                bool hasPersistent = conn.identity && conn.identity.GetComponent<NetworkPlayer>();

                Debug.Log($"   Lobby check  conn {conn.connectionId}  hasPersistent={hasPersistent}");

                if (!hasPersistent)
                {
                    // Destroy leftover gameplay identity if any
                    if (conn.identity != null)
                        NetworkServer.Destroy(conn.identity.gameObject);

                    // Spawn—or respawn—NetworkPlayer/LobbyAvatar
                    AddMissingLobbyPlayer(conn);
                }
            }
            return;     // done with lobby handling
        }

        // ────────────────────────  GAMEPLAY (unchanged)  ──────────────────
        if (shortScene == gameplaySceneShort)
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null) continue;
                SwapToGameplayPlayer(conn);
            }
        }
    }



    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------
    void AddMissingLobbyPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[4]   → Spawning missing LobbyPlayer for conn {conn.connectionId}");
        GameObject obj = Instantiate(networkPlayerPrefab);
        NetworkServer.AddPlayerForConnection(conn, obj);
    }

    void SwapToGameplayPlayer(NetworkConnectionToClient conn)
    {
        var np = conn.identity.GetComponent<NetworkPlayer>();   // persistent
        string nick = np ? np.playerName : $"Player{conn.connectionId}";
        Vector3 pos = GetStartPosition() ? GetStartPosition().position
                                          : Vector3.zero;

        // 1) create the round‑avatar
        GameObject go = Instantiate(gameplayPlayerPrefab, pos, Quaternion.identity);

        // 2) pass data to it (name, etc.)
       // var gp = go.GetComponent<GameplayPlayer>();
        //gp.playerName = nick;

        // 3) give ownership to this connection *without* replacing identity
        NetworkServer.Spawn(go, conn);        // <— key line

        // 4) remember it on the NetworkPlayer for later cleanup
       // np.roundAvatar = go.GetComponent<NetworkIdentity>();

        Debug.Log($"[4]  Spawned GameplayAvatar for conn {conn.connectionId}");
    }

    [Server]
    public void ReturnToLobbyScene()
    {
        string lobbyName = Path.GetFileNameWithoutExtension(onlineScene);
        Debug.Log($"[CNM] Host requested return to Lobby → loading {lobbyName}");
        ServerChangeScene(lobbyName);
    }




    [Server]
    public void StartGameplayScene()
    {
        // derive the actual scene name from the path:
        string sceneName = Path.GetFileNameWithoutExtension(gameplayScenePath);
        Debug.Log($"[CNM] Starting game—loading scene: {sceneName}");
        ServerChangeScene(sceneName);
    }

    static string ShortName(string pathOrName) =>
        Path.GetFileNameWithoutExtension(pathOrName);
}
