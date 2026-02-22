using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private PhaseSettingsSO settings;

    public NetworkVariable<GameState> CurrentState = new(
        GameState.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> PhaseTimer = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentRound = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += OnStateValueChanged;

        if (IsServer)
            CurrentState.Value = GameState.WaitingForPlayers;
    }

    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnStateValueChanged;
    }

    private void OnStateValueChanged(GameState prev, GameState current)
    {
        GameEvents.GameStateChanged(prev, current);
        Debug.Log($"[GameManager] {prev} -> {current}");
    }

    private void Update()
    {
        if (!IsServer) return;

        switch (CurrentState.Value)
        {
            case GameState.WaitingForPlayers:
                CheckMinPlayers();
                break;
            case GameState.Starting:
                TickCountdown();
                break;
            case GameState.DayPhase:
            case GameState.NightPhase:
                TickPhaseTimer();
                break;
            case GameState.Transition:
                TickTransition();
                break;
        }
    }

    private void CheckMinPlayers()
    {
        if (NetworkManager.ConnectedClientsIds.Count >= settings.minPlayersToStart)
        {
            CurrentState.Value = GameState.Starting;
            PhaseTimer.Value = settings.startCountdown;
            Debug.Log($"[GameManager] Min players reached, starting countdown: {settings.startCountdown}s");
        }
    }

    private void TickCountdown()
    {
        PhaseTimer.Value -= Time.deltaTime;
        GameEvents.PhaseTimerUpdated(PhaseTimer.Value);

        if (PhaseTimer.Value <= 0f)
            StartDayPhase();
    }

    private void StartDayPhase()
    {
        CurrentState.Value = GameState.DayPhase;
        PhaseTimer.Value = settings.dayPhaseDuration;
        Debug.Log($"[GameManager] Day phase started (Round {CurrentRound.Value})");
    }

    private void StartTransition()
    {
        GameEvents.DayPhaseCleanup();
        CurrentState.Value = GameState.Transition;
        PhaseTimer.Value = settings.transitionDuration;
        Debug.Log("[GameManager] Transition started");
    }

    private void StartNightPhase()
    {
        CurrentState.Value = GameState.NightPhase;
        PhaseTimer.Value = settings.nightPhaseDuration;
        Debug.Log($"[GameManager] Night phase started (Round {CurrentRound.Value})");
    }

    private void TickPhaseTimer()
    {
        PhaseTimer.Value -= Time.deltaTime;
        GameEvents.PhaseTimerUpdated(PhaseTimer.Value);

        if (PhaseTimer.Value <= 0f)
        {
            if (CurrentState.Value == GameState.DayPhase)
                StartTransition();
            else
                EndRound();
        }
    }

    private void TickTransition()
    {
        PhaseTimer.Value -= Time.deltaTime;
        GameEvents.PhaseTimerUpdated(PhaseTimer.Value);

        if (PhaseTimer.Value <= 0f)
            StartNightPhase();
    }

    private void EndRound()
    {
        CurrentRound.Value++;
        Debug.Log($"[GameManager] Round ended, starting round {CurrentRound.Value}");
        StartDayPhase();
    }
}
