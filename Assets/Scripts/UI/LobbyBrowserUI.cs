using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyBrowserUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject lobbyBrowserPanel;
    [SerializeField] private GameObject createLobbyPanel;
    [SerializeField] private GameObject lobbyRoomPanel;
    
    [Header("Lobby List")]
    [SerializeField] private Transform lobbyListContent;
    [SerializeField] private GameObject lobbyItemPrefab;
    
    [Header("Buttons")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button backButton;
    
    [Header("Join By Code")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinByCodeButton;

    private void OnEnable()
    {
        refreshButton.onClick.AddListener(RefreshLobbies);
        createLobbyButton.onClick.AddListener(OpenCreateLobbyPanel);
        quickJoinButton.onClick.AddListener(QuickJoin);
        backButton.onClick.AddListener(GoBack);
        joinByCodeButton.onClick.AddListener(JoinByCode);
        
        LobbyManager.Instance.OnLobbyListUpdated += UpdateLobbyList;
        LobbyManager.Instance.OnLobbyJoined += OnLobbyJoined;
        
        RefreshLobbies();
    }

    private void OnDisable()
    {
        refreshButton.onClick.RemoveListener(RefreshLobbies);
        createLobbyButton.onClick.RemoveListener(OpenCreateLobbyPanel);
        quickJoinButton.onClick.RemoveListener(QuickJoin);
        backButton.onClick.RemoveListener(GoBack);
        joinByCodeButton.onClick.RemoveListener(JoinByCode);
        
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyListUpdated -= UpdateLobbyList;
            LobbyManager.Instance.OnLobbyJoined -= OnLobbyJoined;
        }
    }

    private void OnLobbyJoined(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        lobbyBrowserPanel.SetActive(false);
        if (lobbyRoomPanel != null)
            lobbyRoomPanel.SetActive(true);
    }

    private async void RefreshLobbies()
    {
        await LobbyManager.Instance.ListLobbies();
    }

    private void UpdateLobbyList(List<Lobby> lobbies)
    {
        foreach (Transform child in lobbyListContent)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var lobby in lobbies)
        {
            GameObject item = Instantiate(lobbyItemPrefab, lobbyListContent);
            item.GetComponent<LobbyItemUI>().Setup(lobby);
        }
    }

    private void OpenCreateLobbyPanel()
    {
        lobbyBrowserPanel.SetActive(false);
        createLobbyPanel.SetActive(true);
    }

    private async void QuickJoin()
    {
        await LobbyManager.Instance.QuickJoin();
    }

    private async void JoinByCode()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (!string.IsNullOrEmpty(code))
        {
            await LobbyManager.Instance.JoinLobbyByCode(code);
        }
    }

    private void GoBack()
    {
        FindObjectOfType<MainMenuUI>().ShowMainMenu();
    }
}

