using Unity.Netcode;
using UnityEngine;

public class EconomyManager : NetworkBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [SerializeField] private DayPhaseSettingsSO settings;

    public NetworkVariable<int> TeamAMoney = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TeamBMoney = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // C1 — stored delegates for proper unsub
    private NetworkVariable<int>.OnValueChangedDelegate onTeamAMoneyChanged;
    private NetworkVariable<int>.OnValueChangedDelegate onTeamBMoneyChanged;

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
        onTeamAMoneyChanged = (_, val) => GameEvents.MoneyChanged(TeamManager.TEAM_A, val);
        onTeamBMoneyChanged = (_, val) => GameEvents.MoneyChanged(TeamManager.TEAM_B, val);

        TeamAMoney.OnValueChanged += onTeamAMoneyChanged;
        TeamBMoney.OnValueChanged += onTeamBMoneyChanged;

        if (IsServer)
        {
            TeamAMoney.Value = settings.startingMoney;
            TeamBMoney.Value = settings.startingMoney;
            Debug.Log($"[EconomyManager] Starting money: {settings.startingMoney}");
        }
    }

    public override void OnNetworkDespawn()
    {
        TeamAMoney.OnValueChanged -= onTeamAMoneyChanged;
        TeamBMoney.OnValueChanged -= onTeamBMoneyChanged;
    }

    public int GetMoney(int teamId)
    {
        return teamId == TeamManager.TEAM_A ? TeamAMoney.Value : TeamBMoney.Value;
    }

    public void AddMoney(int teamId, int amount)
    {
        if (!IsServer) return;

        if (teamId == TeamManager.TEAM_A)
            TeamAMoney.Value += amount;
        else
            TeamBMoney.Value += amount;

        Debug.Log($"[EconomyManager] Team {teamId} +{amount} = {GetMoney(teamId)}");
    }

    public bool SpendMoney(int teamId, int amount)
    {
        if (!IsServer) return false;

        int current = GetMoney(teamId);
        if (current < amount) return false;

        if (teamId == TeamManager.TEAM_A)
            TeamAMoney.Value -= amount;
        else
            TeamBMoney.Value -= amount;

        Debug.Log($"[EconomyManager] Team {teamId} -{amount} = {GetMoney(teamId)}");
        return true;
    }
}
