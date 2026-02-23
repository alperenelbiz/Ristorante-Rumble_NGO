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

    // C2 — throttle PhaseTimer writes
    private float localTimer;
    private float timerSyncTimer;
    private const float TIMER_SYNC_INTERVAL = 0.1f;

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
        CurrentState.OnValueChanged += OnStateValueChanged;

        // C3 — clients get timer via OnValueChanged
        PhaseTimer.OnValueChanged += OnTimerValueChanged;

        if (IsServer)
            CurrentState.Value = GameState.WaitingForPlayers;
    }

    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnStateValueChanged;
        PhaseTimer.OnValueChanged -= OnTimerValueChanged;
    }

    private void OnStateValueChanged(GameState prev, GameState current)
    {
        GameEvents.GameStateChanged(prev, current);
        Debug.Log($"[GameManager] {prev} -> {current}");
    }

    // C3 — clients receive timer updates via network sync
    private void OnTimerValueChanged(float prev, float current)
    {
        if (!IsServer)
            GameEvents.PhaseTimerUpdated(current);
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
            localTimer = settings.startCountdown;
            PhaseTimer.Value = localTimer;
            Debug.Log($"[GameManager] Min players reached, starting countdown: {settings.startCountdown}s");
        }
    }

    private void TickCountdown()
    {
        localTimer -= Time.deltaTime;
        GameEvents.PhaseTimerUpdated(localTimer); // server fires at full rate
        SyncTimerThrottled();

        if (localTimer <= 0f)
            StartDayPhase();
    }

    private void StartDayPhase()
    {
        CurrentState.Value = GameState.DayPhase;
        localTimer = settings.dayPhaseDuration;
        PhaseTimer.Value = localTimer;
        Debug.Log($"[GameManager] Day phase started (Round {CurrentRound.Value})");
    }

    private void StartTransition()
    {
        GameEvents.DayPhaseCleanup();
        CurrentState.Value = GameState.Transition;
        localTimer = settings.transitionDuration;
        PhaseTimer.Value = localTimer;
        Debug.Log("[GameManager] Transition started");
    }

    private void StartNightPhase()
    {
        CurrentState.Value = GameState.NightPhase;
        localTimer = settings.nightPhaseDuration;
        PhaseTimer.Value = localTimer;
        Debug.Log($"[GameManager] Night phase started (Round {CurrentRound.Value})");
    }

    private void TickPhaseTimer()
    {
        localTimer -= Time.deltaTime;
        GameEvents.PhaseTimerUpdated(localTimer);
        SyncTimerThrottled();

        if (localTimer <= 0f)
        {
            if (CurrentState.Value == GameState.DayPhase)
                StartTransition();
            else
                EndRound();
        }
    }

    private void TickTransition()
    {
        localTimer -= Time.deltaTime;
        GameEvents.PhaseTimerUpdated(localTimer);
        SyncTimerThrottled();

        if (localTimer <= 0f)
            StartNightPhase();
    }

    // C2 — throttle NetworkVariable writes to ~10Hz
    private void SyncTimerThrottled()
    {
        timerSyncTimer += Time.deltaTime;
        if (timerSyncTimer >= TIMER_SYNC_INTERVAL)
        {
            timerSyncTimer = 0f;
            PhaseTimer.Value = localTimer;
        }
    }

    private void EndRound()
    {
        CurrentRound.Value++;
        Debug.Log($"[GameManager] Round ended, starting round {CurrentRound.Value}");
        StartDayPhase();
    }
}
