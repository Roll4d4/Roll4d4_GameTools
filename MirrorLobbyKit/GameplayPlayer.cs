using Mirror;
using UnityEngine;

public class GameplayPlayer : NetworkBehaviour
{
    [SyncVar] public string playerName;

    void Start()
    {
        if (isLocalPlayer)
            Debug.Log($"Spawned Gameplay player: {playerName}");
    }
}
