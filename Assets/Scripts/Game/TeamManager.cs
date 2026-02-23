using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance { get; private set; }

    public const int TEAM_A = 0;
    public const int TEAM_B = 1;

    private NetworkList<ulong> teamAPlayers;
    private NetworkList<ulong> teamBPlayers;

    // W9 — stored delegates for proper unsub
    private NetworkList<ulong>.OnListChangedDelegate onTeamAChanged;
    private NetworkList<ulong>.OnListChangedDelegate onTeamBChanged;

    private void Awake()
    {
        // W1 — singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        teamAPlayers = new NetworkList<ulong>();
        teamBPlayers = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        onTeamAChanged = _ => Debug.Log($"[TeamManager] TeamA count: {teamAPlayers.Count}");
        onTeamBChanged = _ => Debug.Log($"[TeamManager] TeamB count: {teamBPlayers.Count}");

        teamAPlayers.OnListChanged += onTeamAChanged;
        teamBPlayers.OnListChanged += onTeamBChanged;
    }

    public override void OnNetworkDespawn()
    {
        teamAPlayers.OnListChanged -= onTeamAChanged;
        teamBPlayers.OnListChanged -= onTeamBChanged;
    }

    public void AutoAssignPlayer(ulong clientId)
    {
        if (!IsServer) return;

        int team = teamAPlayers.Count <= teamBPlayers.Count ? TEAM_A : TEAM_B;
        AssignPlayerToTeam(clientId, team);
    }

    public void AssignPlayerToTeam(ulong clientId, int team)
    {
        if (!IsServer) return;

        RemoveFromAllTeams(clientId);

        if (team == TEAM_A)
            teamAPlayers.Add(clientId);
        else
            teamBPlayers.Add(clientId);

        GameEvents.PlayerTeamAssigned(clientId, team);
        Debug.Log($"[TeamManager] Player {clientId} assigned to Team {(team == TEAM_A ? "A" : "B")}");
    }

    public void RemovePlayer(ulong clientId)
    {
        if (!IsServer) return;
        RemoveFromAllTeams(clientId);
    }

    private void RemoveFromAllTeams(ulong clientId)
    {
        if (teamAPlayers.Contains(clientId))
            teamAPlayers.Remove(clientId);
        if (teamBPlayers.Contains(clientId))
            teamBPlayers.Remove(clientId);
    }

    public int GetPlayerTeam(ulong clientId)
    {
        if (teamAPlayers.Contains(clientId)) return TEAM_A;
        if (teamBPlayers.Contains(clientId)) return TEAM_B;
        return -1;
    }

    public int GetTeamCount(int team)
    {
        return team == TEAM_A ? teamAPlayers.Count : teamBPlayers.Count;
    }
}
