using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public event Action<Lobby> OnLobbyCreated;
    public event Action<Lobby> OnLobbyJoined;
    public event Action<Lobby> OnLobbyUpdated;
    public event Action OnLobbyLeft;
    public event Action<List<Lobby>> OnLobbyListUpdated;
    public event Action OnGameStarting;

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float pollTimer;
    private bool gameStarted;
    private bool isHeartbeating;
    private bool isPolling;

    private const string KEY_RELAY_CODE = "RelayJoinCode";
    private const float HEARTBEAT_INTERVAL = 15f;
    private const float POLL_INTERVAL = 1.5f;

    public Lobby JoinedLobby => joinedLobby;
    public bool IsHost => hostLobby != null;
    public bool IsInLobby => joinedLobby != null;
    public string PlayerId => AuthenticationService.Instance?.PlayerId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await InitializeServices();
    }

    private void Update()
    {
        HandleHeartbeat();
        HandleLobbyPoll();
    }

    #region Initialization

    private async Task InitializeServices()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
            
                // for unity editor, use different profile for each clone
                #if UNITY_EDITOR
                if (ParrelSync.ClonesManager.IsClone())
                {
                    string customArgument = ParrelSync.ClonesManager.GetArgument();
                    options.SetProfile($"Clone_{customArgument}");
                }
                #endif
                // can be deleted later, only for testing
            
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"[LobbyManager] Signed in as: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] Init failed: {e.Message}");
        }
    }

    #endregion

    #region Heartbeat & Polling

    private async void HandleHeartbeat()
    {
        if (hostLobby == null || gameStarted || isHeartbeating) return;

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = HEARTBEAT_INTERVAL;
            isHeartbeating = true;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] Heartbeat failed: {e.Message}");
            }
            finally
            {
                isHeartbeating = false;
            }
        }
    }

    private async void HandleLobbyPoll()
    {
        if (joinedLobby == null || gameStarted || isPolling) return;

        pollTimer -= Time.deltaTime;
        if (pollTimer <= 0f)
        {
            pollTimer = POLL_INTERVAL;
            isPolling = true;
            try
            {
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                OnLobbyUpdated?.Invoke(joinedLobby);

                if (!IsHost && joinedLobby.Data != null &&
                    joinedLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData) &&
                    !string.IsNullOrEmpty(relayData.Value))
                {
                    gameStarted = true;
                    await JoinGame(relayData.Value);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] Poll failed: {e.Message}");
            }
            finally
            {
                isPolling = false;
            }
        }
    }

    #endregion

    #region Lobby Operations

    public async Task<bool> CreateLobby(string lobbyName, int maxPlayers = 10)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, "") }
                }
            };

            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            joinedLobby = hostLobby;

            Debug.Log($"[LobbyManager] Lobby created: {hostLobby.Name} | Code: {hostLobby.LobbyCode}");
            OnLobbyCreated?.Invoke(hostLobby);

            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Create lobby failed: {e.Message}");
            return false;
        }
    }

    public async Task<List<Lobby>> ListLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            OnLobbyListUpdated?.Invoke(response.Results);

            Debug.Log($"[LobbyManager] Found {response.Results.Count} lobbies");
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] List lobbies failed: {e.Message}");
            return new List<Lobby>();
        }
    }

    public async Task<bool> JoinLobbyById(string lobbyId)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log($"[LobbyManager] Joined lobby: {joinedLobby.Name}");
            OnLobbyJoined?.Invoke(joinedLobby);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Join by ID failed: {e.Message}");
            return false;
        }
    }

    public async Task<bool> JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log($"[LobbyManager] Joined lobby: {joinedLobby.Name}");
            OnLobbyJoined?.Invoke(joinedLobby);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Join by code failed: {e.Message}");
            return false;
        }
    }

    public async Task<bool> QuickJoin()
    {
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            Debug.Log($"[LobbyManager] Quick joined: {joinedLobby.Name}");
            OnLobbyJoined?.Invoke(joinedLobby);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Quick join failed: {e.Message}");
            return false;
        }
    }

    public async Task LeaveLobby()
    {
        if (joinedLobby == null) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, PlayerId);
            Debug.Log("[LobbyManager] Left lobby");

            hostLobby = null;
            joinedLobby = null;
            OnLobbyLeft?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Leave failed: {e.Message}");
        }
    }

    #endregion

    #region Game Start

    public async Task<bool> StartGame()
    {
        if (!IsHost)
        {
            Debug.LogError("[LobbyManager] Only host can start the game!");
            return false;
        }

        try
        {
            LoadingScreenUI.Instance?.Show("Preparing the game...");

            string relayCode = await RelayManager.Instance.CreateRelay(hostLobby.MaxPlayers - 1);
            if (string.IsNullOrEmpty(relayCode))
            {
                LoadingScreenUI.Instance?.Hide();
                Debug.LogError("[LobbyManager] Failed to create relay!");
                return false;
            }

            await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });

            NetworkManager.Singleton.StartHost();

            SceneController.Instance?.SubscribeToSceneEvents();
            SceneController.Instance?.LoadGameScene();

            gameStarted = true;
            OnGameStarting?.Invoke();
            Debug.Log("[LobbyManager] Game started as HOST!");

            return true;
        }
        catch (Exception e)
        {
            LoadingScreenUI.Instance?.Hide();
            Debug.LogError($"[LobbyManager] Start game failed: {e.Message}");
            return false;
        }
    }

    private async Task JoinGame(string relayCode)
    {
        try
        {
            LoadingScreenUI.Instance?.Show("Joining to the game...");

            bool success = await RelayManager.Instance.JoinRelay(relayCode);
            if (!success)
            {
                LoadingScreenUI.Instance?.Hide();
                Debug.LogError("[LobbyManager] Failed to join relay!");
                return;
            }

            NetworkManager.Singleton.StartClient();

            SceneController.Instance?.SubscribeToSceneEvents();

            OnGameStarting?.Invoke();
            Debug.Log("[LobbyManager] Joined game as CLIENT!");
        }
        catch (Exception e)
        {
            LoadingScreenUI.Instance?.Hide();
            Debug.LogError($"[LobbyManager] Join game failed: {e.Message}");
        }
    }

    #endregion

    #region Ready System

    public async Task SetPlayerReady(bool isReady)
    {
        if (joinedLobby == null) return;

        try
        {
            var options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady ? "1" : "0") }
                }
            };

            joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, PlayerId, options);
            Debug.Log($"[LobbyManager] Player ready: {isReady}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Set ready failed: {e.Message}");
        }
    }

    public bool AreAllPlayersReady()
    {
        if (joinedLobby == null) return false;
        if (joinedLobby.Players.Count < 2) return false;

        foreach (var player in joinedLobby.Players)
        {
            if (player.Data == null ||
                !player.Data.TryGetValue("ready", out var readyData) ||
                readyData.Value != "1")
            {
                return false;
            }
        }

        return true;
    }

    public bool IsPlayerReady(string playerId)
    {
        if (joinedLobby == null) return false;

        foreach (var player in joinedLobby.Players)
        {
            if (player.Id == playerId)
            {
                return player.Data != null &&
                       player.Data.TryGetValue("ready", out var readyData) &&
                       readyData.Value == "1";
            }
        }

        return false;
    }

    #endregion

    #region Cleanup

    public void Cleanup()
    {
        hostLobby = null;
        joinedLobby = null;
        gameStarted = false;
        isHeartbeating = false;
        isPolling = false;
        RelayManager.Instance?.Cleanup();
    }

    private void OnApplicationQuit()
    {
        if (joinedLobby != null)
        {
            _ = LeaveLobby();
        }
    }

    #endregion
}