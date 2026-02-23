using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeSelectUI : MonoBehaviour
{
    public static RecipeSelectUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject recipeButtonPrefab;

    private CookingStation currentStation;
    private PlayerInteraction playerInteraction;

    private void Awake()
    {
        // W1 — singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        panel.SetActive(false);
    }

    public void Open(CookingStation station)
    {
        currentStation = station;

        // W8 — null-check chain
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClient == null || nm.LocalClient.PlayerObject == null) return;
        playerInteraction = nm.LocalClient.PlayerObject.GetComponent<PlayerInteraction>();

        // Clear old buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        // Create buttons for compatible recipes
        var db = RecipeDatabase.Instance;
        for (int i = 0; i < db.RecipeCount; i++)
        {
            var recipe = db.GetRecipe(i);
            if (recipe.requiredStation != CookingStationType.Any &&
                recipe.requiredStation != station.StationType)
                continue;

            int recipeIndex = i;
            var btnGO = Instantiate(recipeButtonPrefab, buttonContainer);

            var label = btnGO.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"{recipe.recipeName} ({recipe.cookingTime}s) - {recipe.sellPrice}₺";

            var btn = btnGO.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnRecipeSelected(recipeIndex));
        }

        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        currentStation = null;
    }

    private void OnRecipeSelected(int recipeIndex)
    {
        if (currentStation == null || playerInteraction == null) return;

        playerInteraction.RequestStartCooking(currentStation, recipeIndex);
        Close();
    }

    private void Update()
    {
        // Close on Escape
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }
}
