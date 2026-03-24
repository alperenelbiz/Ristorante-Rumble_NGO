#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Reusable debug panel: live status display + action buttons.
/// Toggle with F1. Wrap in UNITY_EDITOR/DEVELOPMENT_BUILD.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private const float REFRESH_INTERVAL = 0.25f;
    private const float PANEL_WIDTH = 320f;
    private const float BUTTON_HEIGHT = 30f;
    private const int FONT_SIZE = 14;
    private const int SORT_ORDER = 100;

    private GameObject panelRoot;
    private TextMeshProUGUI statusLabel;

    private void OnEnable()
    {
        BuildUI();
        panelRoot.SetActive(showOnStart);
        InvokeRepeating(nameof(RefreshStatus), 0f, REFRESH_INTERVAL);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshStatus));
        if (panelRoot != null)
            Destroy(panelRoot);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame && panelRoot != null)
            panelRoot.SetActive(!panelRoot.activeSelf);
    }

    private void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("DebugCanvas");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORT_ORDER;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem required for button clicks
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        panelRoot = canvasGo;

        // Panel background
        var panel = CreateRect("Panel", canvasGo.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(10f, -10f);
        panelRect.sizeDelta = new Vector2(PANEL_WIDTH, 0f);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.75f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Status label
        statusLabel = CreateLabel("StatusLabel", panel.transform, "Initializing...");

        // Section header
        CreateLabel("ActionsHeader", panel.transform, "--- Actions ---");

        // Action buttons
        CreateButton(panel.transform, "Force -> DayPhase", () => ForceState(GameState.DayPhase));
        CreateButton(panel.transform, "Force -> NightPhase", () => ForceState(GameState.NightPhase));
        CreateButton(panel.transform, "Force -> RoundEnd", () => ForceState(GameState.RoundEnd));
        CreateButton(panel.transform, "Force -> GameOver", () => ForceState(GameState.GameOver));
        CreateButton(panel.transform, "+100 TeamA Money", () => AddTeamMoney(TeamManager.TEAM_A, 100));
        CreateButton(panel.transform, "+100 TeamB Money", () => AddTeamMoney(TeamManager.TEAM_B, 100));
        CreateButton(panel.transform, "Reset Events", () =>
        {
            GameEvents.ResetAll();
            Debug.Log("[DebugPanel] Events reset");
        });
    }

    private void RefreshStatus()
    {
        if (statusLabel == null) return;

        var sb = new System.Text.StringBuilder(256);

        // GameManager
        if (GameManager.Instance != null)
        {
            var gm = GameManager.Instance;
            sb.AppendLine($"<color=green>GameManager:</color> {gm.CurrentState.Value}");
            sb.AppendLine($"Timer: {gm.PhaseTimer.Value:F1}s | Round: {gm.CurrentRound.Value}");
        }
        else
        {
            sb.AppendLine("<color=red>GameManager: MISSING</color>");
        }

        // EconomyManager
        if (EconomyManager.Instance != null)
        {
            var em = EconomyManager.Instance;
            sb.AppendLine($"TeamA: ${em.TeamAMoney.Value} | TeamB: ${em.TeamBMoney.Value}");
        }
        else
        {
            sb.AppendLine("<color=red>EconomyManager: MISSING</color>");
        }

        // TeamManager
        if (TeamManager.Instance != null)
        {
            var tm = TeamManager.Instance;
            sb.AppendLine($"TeamManager: {tm.GetTeamCount(TeamManager.TEAM_A)}/{tm.GetTeamCount(TeamManager.TEAM_B)} players");
        }
        else
        {
            sb.AppendLine("<color=red>TeamManager: MISSING</color>");
        }

        // NetworkManager
        if (NetworkManager.Singleton != null)
        {
            var nm = NetworkManager.Singleton;
            string role = nm.IsHost ? "Host" : nm.IsClient ? "Client" : "Disconnected";
            sb.Append($"NetworkManager: {role}");
        }
        else
        {
            sb.Append("<color=red>NetworkManager: MISSING</color>");
        }

        statusLabel.text = sb.ToString();
    }

    private void ForceState(GameState state)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[DebugPanel] GameManager.Instance is null");
            return;
        }
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[DebugPanel] Only server can force state");
            return;
        }
        GameManager.Instance.DebugForceState(state);
        Debug.Log($"[DebugPanel] Forced state -> {state}");
    }

    private void AddTeamMoney(int teamId, int amount)
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogWarning("[DebugPanel] EconomyManager.Instance is null");
            return;
        }
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[DebugPanel] Only server can add money");
            return;
        }
        EconomyManager.Instance.AddMoney(teamId, amount);
        Debug.Log($"[DebugPanel] +{amount} to Team {teamId}");
    }

    // --- UI Helper Methods ---

    private static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text)
    {
        var go = CreateRect(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = FONT_SIZE;
        tmp.richText = true;
        tmp.color = Color.white;
        return tmp;
    }

    private static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = CreateRect($"Btn_{label}", parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = BUTTON_HEIGHT;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txtGo = CreateRect("Label", go.transform);
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = FONT_SIZE;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
    }
}
#endif
