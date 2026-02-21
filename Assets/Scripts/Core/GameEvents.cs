using System;

public enum GameState
{
    WaitingForPlayers,
    Starting,
    DayPhase,
    Transition,
    NightPhase,
    RoundEnd,
    GameOver
}

public static class GameEvents
{
    public static event Action<GameState, GameState> OnGameStateChanged;
    public static event Action<float> OnPhaseTimerUpdated;
    public static event Action<ulong, int> OnPlayerTeamAssigned;
    public static event Action OnAllPlayersReady;
    public static event Action<string> OnSceneLoaded;

    public static void GameStateChanged(GameState prev, GameState cur) => OnGameStateChanged?.Invoke(prev, cur);
    public static void PhaseTimerUpdated(float t) => OnPhaseTimerUpdated?.Invoke(t);
    public static void PlayerTeamAssigned(ulong id, int team) => OnPlayerTeamAssigned?.Invoke(id, team);
    public static void AllPlayersReady() => OnAllPlayersReady?.Invoke();
    public static void SceneLoaded(string name) => OnSceneLoaded?.Invoke(name);
}
