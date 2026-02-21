using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera dayCamera;
    [SerializeField] private CinemachineCamera nightCamera;

    [Header("Priority")]
    [SerializeField] private int activePriority = 10;
    [SerializeField] private int inactivePriority = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        ActivateDayCamera();
    }

    private void OnGameStateChanged(GameState prev, GameState current)
    {
        switch (current)
        {
            case GameState.WaitingForPlayers:
            case GameState.Starting:
            case GameState.DayPhase:
                ActivateDayCamera();
                break;
            case GameState.Transition:
            case GameState.NightPhase:
                ActivateNightCamera();
                break;
        }
    }

    private void ActivateDayCamera()
    {
        if (dayCamera != null)
            dayCamera.Priority = activePriority;
        if (nightCamera != null)
            nightCamera.Priority = inactivePriority;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[CameraManager] Day camera activated");
    }

    private void ActivateNightCamera()
    {
        if (dayCamera != null)
            dayCamera.Priority = inactivePriority;
        if (nightCamera != null)
            nightCamera.Priority = activePriority;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[CameraManager] Night camera activated");
    }

    public void SetFollowTarget(Transform target)
    {
        if (dayCamera != null)
            dayCamera.Follow = target;
        if (nightCamera != null)
            nightCamera.Follow = target;

        Debug.Log($"[CameraManager] Follow target set: {target.name}");
    }
}