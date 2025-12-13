using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyRoomUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject lobbyRoomPanel;
    [SerializeField] private GameObject lobbyBrowserPanel;

    [Header("Info")]
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private Button copyCodeButton;

    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    [Header("Ready State")]
    [SerializeField] private TMP_Text readyButtonText;

    private bool isReady = false;

    private void OnEnable()
    {
        LobbyManager.Instance.OnLobbyCreated += OnLobbyJoined;
        LobbyManager.Instance.OnLobbyJoined += OnLobbyJoined;
        LobbyManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
        LobbyManager.Instance.OnGameStarting += OnGameStarting;

        readyButton.onClick.AddListener(ToggleReady);
        startGameButton.onClick.AddListener(StartGame);
        leaveButton.onClick.AddListener(LeaveLobby);
        copyCodeButton.onClick.AddListener(CopyLobbyCode);

        if (LobbyManager.Instance.IsInLobby)
        {
            isReady = false;
            if (readyButtonText != null)
                readyButtonText.text = "Ready";
            UpdateUI();
        }
    }

    private void OnDisable()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyCreated -= OnLobbyJoined;
            LobbyManager.Instance.OnLobbyJoined -= OnLobbyJoined;
            LobbyManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
            LobbyManager.Instance.OnGameStarting -= OnGameStarting;
        }

        readyButton.onClick.RemoveListener(ToggleReady);
        startGameButton.onClick.RemoveListener(StartGame);
        leaveButton.onClick.RemoveListener(LeaveLobby);
        copyCodeButton.onClick.RemoveListener(CopyLobbyCode);
    }

    public void Show()
    {
        lobbyRoomPanel.SetActive(true);
        isReady = false;
        if (readyButtonText != null)
            readyButtonText.text = "Ready";
        UpdateUI();
    }

    private void OnLobbyJoined(Lobby lobby)
    {
        Show();
    }

    private void OnLobbyUpdated(Lobby lobby)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        var lobby = LobbyManager.Instance.JoinedLobby;
        if (lobby == null) return;

        lobbyNameText.text = lobby.Name;
        lobbyCodeText.text = $"Code: {lobby.LobbyCode}";

        startGameButton.gameObject.SetActive(LobbyManager.Instance.IsHost);

        UpdatePlayerList(lobby.Players);
    }

    private void UpdatePlayerList(List<Player> players)
    {
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var player in players)
        {
            if (playerItemPrefab != null)
            {
                GameObject item = Instantiate(playerItemPrefab, playerListContent);
                var text = item.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    string displayId = player.Id.Length > 8 ? player.Id.Substring(0, 8) + "..." : player.Id;
                    bool isHost = player.Id == LobbyManager.Instance.JoinedLobby?.HostId;
                    text.text = isHost ? $"{displayId} (Host)" : displayId;
                }
            }
        }
    }

    private void ToggleReady()
    {
        isReady = !isReady;
        if (readyButtonText != null)
            readyButtonText.text = isReady ? "Not Ready" : "Ready";
    }

    private async void StartGame()
    {
        if (!LobbyManager.Instance.IsHost) return;
        startGameButton.interactable = false;
        await LobbyManager.Instance.StartGame();
        startGameButton.interactable = true;
    }

    private async void LeaveLobby()
    {
        leaveButton.interactable = false;
        await LobbyManager.Instance.LeaveLobby();
        leaveButton.interactable = true;
        
        lobbyRoomPanel.SetActive(false);
        if (lobbyBrowserPanel != null)
            lobbyBrowserPanel.SetActive(true);
    }

    private void CopyLobbyCode()
    {
        var lobby = LobbyManager.Instance.JoinedLobby;
        if (lobby != null)
        {
            GUIUtility.systemCopyBuffer = lobby.LobbyCode;
            Debug.Log($"[LobbyRoomUI] Copied code: {lobby.LobbyCode}");
        }
    }

    private void OnGameStarting()
    {
        Debug.Log("[LobbyRoomUI] Game is starting!");
    }
}

