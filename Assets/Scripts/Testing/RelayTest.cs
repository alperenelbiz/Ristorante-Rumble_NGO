using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelayTest : MonoBehaviour
{
    [SerializeField] private Button createLobbyBtn;
    [SerializeField] private Button listLobbiesBtn;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private TMP_InputField lobbyCodeInput;
    [SerializeField] private Button joinByCodeBtn;
    
    private void Start()
    {
        createLobbyBtn?.onClick.AddListener(async () =>
        {
            Debug.Log("Create Lobby button clicked");
            await LobbyManager.Instance.CreateLobby("Test Lobby", 10);
        });
        
        listLobbiesBtn?.onClick.AddListener(async () =>
        {
            Debug.Log("List Lobbies button clicked");
            var lobbies = await LobbyManager.Instance.ListLobbies();
            foreach (var lobby in lobbies)
            {
                Debug.Log($"Lobby: {lobby.Name} | Players: {lobby.Players.Count}/{lobby.MaxPlayers} | Code: {lobby.LobbyCode}");
            }
        });
        
        startGameBtn?.onClick.AddListener(async () =>
        {
            Debug.Log("Start Game button clicked");
            await LobbyManager.Instance.StartGame();
        });
        
        joinByCodeBtn?.onClick.AddListener(async () =>
        {
            Debug.Log("Join By Code button clicked");
            if (!string.IsNullOrEmpty(lobbyCodeInput?.text))
            {
                Debug.Log("Joining lobby by code: " + lobbyCodeInput.text);
                await LobbyManager.Instance.JoinLobbyByCode(lobbyCodeInput.text);
            }
        });
    }
}