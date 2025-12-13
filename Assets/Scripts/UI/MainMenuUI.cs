using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyBrowserPanel;
    
    private void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        
        ShowMainMenu();
    }
    
    private void OnPlayClicked()
    {
        mainMenuPanel.SetActive(false);
        lobbyBrowserPanel.SetActive(true);
    }
    
    private void OnSettingsClicked()
    {
        // TODO: Settings panel
    }
    
    private void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        lobbyBrowserPanel.SetActive(false);
    }
}

