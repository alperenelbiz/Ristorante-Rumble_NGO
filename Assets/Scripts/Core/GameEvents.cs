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

    // Day Phase events
    public static event Action<int, int> OnCookingStarted;
    public static event Action<int, int, int> OnCookingCompleted;
    public static event Action<int, int> OnMoneyChanged;
    public static event Action OnDayPhaseCleanup;

    public static void GameStateChanged(GameState prev, GameState cur) => OnGameStateChanged?.Invoke(prev, cur);
    public static void PhaseTimerUpdated(float t) => OnPhaseTimerUpdated?.Invoke(t);
    public static void PlayerTeamAssigned(ulong id, int team) => OnPlayerTeamAssigned?.Invoke(id, team);
    public static void AllPlayersReady() => OnAllPlayersReady?.Invoke();
    public static void SceneLoaded(string name) => OnSceneLoaded?.Invoke(name);

    // Day Phase fire methods
    public static void CookingStarted(int teamId, int stationId) => OnCookingStarted?.Invoke(teamId, stationId);
    public static void CookingCompleted(int teamId, int stationId, int recipeIndex) => OnCookingCompleted?.Invoke(teamId, stationId, recipeIndex);
    public static void MoneyChanged(int teamId, int newAmount) => OnMoneyChanged?.Invoke(teamId, newAmount);
    public static void DayPhaseCleanup() => OnDayPhaseCleanup?.Invoke();

    // D-04 — null all delegates for clean session reset
    public static void ResetAll()
    {
        OnGameStateChanged = null;
        OnPhaseTimerUpdated = null;
        OnPlayerTeamAssigned = null;
        OnAllPlayersReady = null;
        OnSceneLoaded = null;
        OnCookingStarted = null;
        OnCookingCompleted = null;
        OnMoneyChanged = null;
        OnDayPhaseCleanup = null;
    }
}
