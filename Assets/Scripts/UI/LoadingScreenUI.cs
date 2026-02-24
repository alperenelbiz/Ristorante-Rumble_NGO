using UnityEngine;
using TMPro;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text statusText;

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

    private void Start()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void Show(string status = "Yukleniyor...")
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (statusText != null)
            statusText.text = status;
    }

    public void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    public void Hide()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
}
