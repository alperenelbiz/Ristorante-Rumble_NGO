using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateLobbyUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private TMP_Text maxPlayersText;

    [Header("Buttons")]
    [SerializeField] private Button createButton;
    [SerializeField] private Button cancelButton;

    [Header("Panels")]
    [SerializeField] private GameObject createLobbyPanel;
    [SerializeField] private GameObject lobbyBrowserPanel;
    [SerializeField] private GameObject lobbyRoomPanel;

    private void OnEnable()
    {
        maxPlayersSlider.onValueChanged.AddListener(UpdateMaxPlayersText);
        createButton.onClick.AddListener(CreateLobby);
        cancelButton.onClick.AddListener(Cancel);

        UpdateMaxPlayersText(maxPlayersSlider.value);
    }

    private void OnDisable()
    {
        maxPlayersSlider.onValueChanged.RemoveListener(UpdateMaxPlayersText);
        createButton.onClick.RemoveListener(CreateLobby);
        cancelButton.onClick.RemoveListener(Cancel);
    }

    private void UpdateMaxPlayersText(float value)
    {
        maxPlayersText.text = $"{(int)value} Players";
    }

    private async void CreateLobby()
    {
        string lobbyName = lobbyNameInput.text.Trim();
        if (string.IsNullOrEmpty(lobbyName))
        {
            lobbyName = $"Lobby_{Random.Range(1000, 9999)}";
        }

        int maxPlayers = (int)maxPlayersSlider.value;

        bool success = await LobbyManager.Instance.CreateLobby(lobbyName, maxPlayers);
        if (success)
        {
            createLobbyPanel.SetActive(false);
            if (lobbyRoomPanel != null)
                lobbyRoomPanel.SetActive(true);
        }
    }

    private void Cancel()
    {
        createLobbyPanel.SetActive(false);
        lobbyBrowserPanel.SetActive(true);
    }
}

