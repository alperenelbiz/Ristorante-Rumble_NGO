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
    public event Action OnLobbyLeft;
    public event Action<List<Lobby>> OnLobbyListUpdated;
    public event Action OnGameStarting;

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float pollTimer;

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
        if (hostLobby == null) return;

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = HEARTBEAT_INTERVAL;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] Heartbeat failed: {e.Message}");
            }
        }
    }

    private async void HandleLobbyPoll()
    {
        if (joinedLobby == null || IsHost) return;

        pollTimer -= Time.deltaTime;
        if (pollTimer <= 0f)
        {
            pollTimer = POLL_INTERVAL;
            try
            {
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                if (joinedLobby.Data != null &&
                    joinedLobby.Data.TryGetValue(KEY_RELAY_CODE, out var relayData) &&
                    !string.IsNullOrEmpty(relayData.Value))
                {
                    await JoinGame(relayData.Value);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyManager] Poll failed: {e.Message}");
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
            string relayCode = await RelayManager.Instance.CreateRelay(hostLobby.MaxPlayers - 1);
            if (string.IsNullOrEmpty(relayCode))
            {
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
            
            OnGameStarting?.Invoke();
            Debug.Log("[LobbyManager] Game started as HOST!");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] Start game failed: {e.Message}");
            return false;
        }
    }

    private async Task JoinGame(string relayCode)
    {
        try
        {
            bool success = await RelayManager.Instance.JoinRelay(relayCode);
            if (!success)
            {
                Debug.LogError("[LobbyManager] Failed to join relay!");
                return;
            }

            NetworkManager.Singleton.StartClient();
            
            OnGameStarting?.Invoke();
            Debug.Log("[LobbyManager] Joined game as CLIENT!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] Join game failed: {e.Message}");
        }
    }

    #endregion

    #region Cleanup

    public void Cleanup()
    {
        hostLobby = null;
        joinedLobby = null;
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