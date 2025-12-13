using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;
    
    private Lobby lobby;

    public void Setup(Lobby lobby)
    {
        this.lobby = lobby;
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        
        joinButton.onClick.AddListener(JoinLobby);
    }

    private void OnDestroy()
    {
        joinButton.onClick.RemoveListener(JoinLobby);
    }

    private async void JoinLobby()
    {
        await LobbyManager.Instance.JoinLobbyById(lobby.Id);
    }
}

