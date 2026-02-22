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

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        TeamAMoney.OnValueChanged += (_, val) => GameEvents.MoneyChanged(TeamManager.TEAM_A, val);
        TeamBMoney.OnValueChanged += (_, val) => GameEvents.MoneyChanged(TeamManager.TEAM_B, val);

        if (IsServer)
        {
            TeamAMoney.Value = settings.startingMoney;
            TeamBMoney.Value = settings.startingMoney;
            Debug.Log($"[EconomyManager] Starting money: {settings.startingMoney}");
        }
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
