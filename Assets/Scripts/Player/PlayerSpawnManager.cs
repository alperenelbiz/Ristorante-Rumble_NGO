using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] teamASpawnPoints;
    [SerializeField] private Transform[] teamBSpawnPoints;

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    private Dictionary<ulong, NetworkObject> spawnedPlayers = new();
    private int teamAIndex;
    private int teamBIndex;

    private void Awake()
    {
        // W1 — singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        // Host is already connected when scene loads, spawn immediately
        if (IsHost)
            OnClientConnected(NetworkManager.LocalClientId);
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (spawnedPlayers.ContainsKey(clientId)) return;

        TeamManager.Instance.AutoAssignPlayer(clientId);
        int team = TeamManager.Instance.GetPlayerTeam(clientId);
        Vector3 spawnPos = GetSpawnPosition(team);

        SpawnPlayer(clientId, spawnPos, team);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (spawnedPlayers.TryGetValue(clientId, out var netObj))
        {
            if (netObj != null)
                netObj.Despawn();

            spawnedPlayers.Remove(clientId);
        }

        TeamManager.Instance.RemovePlayer(clientId);
        Debug.Log($"[SpawnManager] Player {clientId} disconnected and cleaned up");
    }

    private Vector3 GetSpawnPosition(int team)
    {
        Transform[] spawnPoints = team == TeamManager.TEAM_A ? teamASpawnPoints : teamBSpawnPoints;
        ref int index = ref (team == TeamManager.TEAM_A ? ref teamAIndex : ref teamBIndex);

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[SpawnManager] No spawn points for team {team}, using origin");
            return Vector3.zero;
        }

        Vector3 pos = spawnPoints[index % spawnPoints.Length].position;
        index++;
        return pos;
    }

    private void SpawnPlayer(ulong clientId, Vector3 position, int team)
    {
        GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);

        spawnedPlayers[clientId] = netObj;
        Debug.Log($"[SpawnManager] Player {clientId} spawned at Team {(team == TeamManager.TEAM_A ? "A" : "B")}");
    }
}
