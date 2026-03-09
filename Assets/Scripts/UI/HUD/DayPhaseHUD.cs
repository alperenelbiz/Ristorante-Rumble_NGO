using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DayPhaseHUD : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject hudPanel;

    [Header("Money")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Carried Item")]
    [SerializeField] private GameObject carriedItemPanel;
    [SerializeField] private Image carriedItemIcon;
    [SerializeField] private TMP_Text carriedItemName;

    [Header("Phase Timer")]
    [SerializeField] private TMP_Text timerText;

    private PlayerInteraction localPlayer;

    private void OnEnable()
    {
        GameEvents.OnMoneyChanged += OnMoneyChanged;
        GameEvents.OnPhaseTimerUpdated += OnTimerUpdated;
        GameEvents.OnGameStateChanged += OnGameStateChanged;

        // I2 — late-joiner: check current state so HUD isn't stuck hidden
        if (GameManager.Instance != null)
        {
            var state = GameManager.Instance.CurrentState.Value;
            bool show = state == GameState.DayPhase;
            if (hudPanel != null) hudPanel.SetActive(show);
        }
    }

    private void OnDisable()
    {
        GameEvents.OnMoneyChanged -= OnMoneyChanged;
        GameEvents.OnPhaseTimerUpdated -= OnTimerUpdated;
        GameEvents.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        // Find local player if not cached
        if (localPlayer == null)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
                localPlayer = nm.LocalClient.PlayerObject.GetComponent<PlayerInteraction>();
        }

        UpdateCarriedItem();
    }

    private void UpdateCarriedItem()
    {
        if (localPlayer == null)
        {
            if (carriedItemPanel.activeSelf) carriedItemPanel.SetActive(false);
            return;
        }

        var carried = localPlayer.CarriedItem.Value;
        bool carrying = !carried.IsEmpty;

        if (carriedItemPanel.activeSelf != carrying)
            carriedItemPanel.SetActive(carrying);

        if (!carrying) return;

        var db = RecipeDatabase.Instance;
        if (carried.ItemType == CarriedItemType.Ingredient)
        {
            var ingredient = db.GetIngredient(carried.ItemIndex);
            if (ingredient != null)
            {
                carriedItemIcon.sprite = ingredient.icon;
                carriedItemName.text = ingredient.ingredientName;
            }
        }
        else if (carried.ItemType == CarriedItemType.Dish)
        {
            var recipe = db.GetRecipe(carried.ItemIndex);
            if (recipe != null)
            {
                carriedItemIcon.sprite = recipe.icon;
                carriedItemName.text = recipe.recipeName;
            }
        }
    }

    private void OnMoneyChanged(int teamId, int newAmount)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClient == null) return;

        int myTeam = TeamManager.Instance.GetPlayerTeam(nm.LocalClientId);
        if (teamId == myTeam && moneyText != null)
            moneyText.text = $"{newAmount}₺";
    }

    private void OnTimerUpdated(float time)
    {
        if (timerText == null) return;
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, time));
        timerText.text = $"{seconds / 60}:{seconds % 60:D2}";
    }

    // C4 — toggle child panel instead of gameObject so events stay subscribed
    private void OnGameStateChanged(GameState prev, GameState cur)
    {
        bool show = cur == GameState.DayPhase;
        if (hudPanel != null)
            hudPanel.SetActive(show);
    }
}
