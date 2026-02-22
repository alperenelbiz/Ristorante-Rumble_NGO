using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DayPhaseHUD : MonoBehaviour
{
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
        if (carried.ItemType == 1) // ingredient
        {
            var ingredient = db.GetIngredient(carried.ItemIndex);
            if (ingredient != null)
            {
                carriedItemIcon.sprite = ingredient.icon;
                carriedItemName.text = ingredient.ingredientName;
            }
        }
        else if (carried.ItemType == 2) // dish
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
        // Show own team money
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

    private void OnGameStateChanged(GameState prev, GameState cur)
    {
        bool show = cur == GameState.DayPhase;
        gameObject.SetActive(show);
    }
}
