using UnityEngine;

public class LightingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light directionalLight;

    [Header("Day Settings")]
    [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.8f);
    [SerializeField] private float dayIntensity = 1f;

    [Header("Night Settings")]
    [SerializeField] private Color nightColor = new Color(0.2f, 0.2f, 0.4f);
    [SerializeField] private float nightIntensity = 0.3f;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 2f;

    private Color targetColor;
    private float targetIntensity;

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
        targetColor = dayColor;
        targetIntensity = dayIntensity;

        if (directionalLight != null)
        {
            directionalLight.color = dayColor;
            directionalLight.intensity = dayIntensity;
        }
    }

    private void OnGameStateChanged(GameState prev, GameState current)
    {
        switch (current)
        {
            case GameState.WaitingForPlayers:
            case GameState.Starting:
            case GameState.DayPhase:
                targetColor = dayColor;
                targetIntensity = dayIntensity;
                break;
            case GameState.Transition:
            case GameState.NightPhase:
                targetColor = nightColor;
                targetIntensity = nightIntensity;
                break;
        }
    }

    private void Update()
    {
        if (directionalLight == null) return;

        directionalLight.color = Color.Lerp(directionalLight.color, targetColor, transitionSpeed * Time.deltaTime);
        directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, transitionSpeed * Time.deltaTime);
    }
}