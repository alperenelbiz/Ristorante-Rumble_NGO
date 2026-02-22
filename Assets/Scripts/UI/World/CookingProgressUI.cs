using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CookingProgressUI : MonoBehaviour
{
    [SerializeField] private CookingStation station;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text statusText;

    private void Update()
    {
        if (station == null) return;

        var state = station.StationState.Value;
        bool showUI = state.Status != 0; // show when not idle

        if (progressPanel.activeSelf != showUI)
            progressPanel.SetActive(showUI);

        if (!showUI) return;

        progressFill.fillAmount = state.CookProgress;

        switch (state.Status)
        {
            case 1: // cooking
                progressFill.color = Color.yellow;
                var recipe = RecipeDatabase.Instance.GetRecipe(state.RecipeIndex);
                statusText.text = recipe != null ? recipe.recipeName : "Cooking...";
                break;
            case 2: // done
                progressFill.color = Color.green;
                statusText.text = "Ready!";
                break;
            case 3: // burned
                progressFill.color = Color.red;
                statusText.text = "Burned!";
                break;
        }

        // Billboard — face camera
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }
}
