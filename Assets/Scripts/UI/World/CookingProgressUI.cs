using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CookingProgressUI : MonoBehaviour
{
    [SerializeField] private CookingStation station;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text statusText;

    // W10 — cached camera
    private Camera cachedCamera;

    private void Start()
    {
        cachedCamera = Camera.main;
    }

    private void Update()
    {
        if (station == null) return;

        var state = station.StationState.Value;
        bool showUI = state.Status != CookingStatus.Idle;

        if (progressPanel.activeSelf != showUI)
            progressPanel.SetActive(showUI);

        if (!showUI) return;

        progressFill.fillAmount = state.CookProgress;

        switch (state.Status)
        {
            case CookingStatus.Cooking:
                progressFill.color = Color.yellow;
                var recipe = RecipeDatabase.Instance.GetRecipe(state.RecipeIndex);
                statusText.text = recipe != null ? recipe.recipeName : "Cooking...";
                break;
            case CookingStatus.Done:
                progressFill.color = Color.green;
                statusText.text = "Ready!";
                break;
            case CookingStatus.Burned:
                progressFill.color = Color.red;
                statusText.text = "Burned!";
                break;
        }

        // Billboard — face camera
        if (cachedCamera != null)
            transform.forward = cachedCamera.transform.forward;
    }
}
