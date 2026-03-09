using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "RistoranteRumble/RecipeSO")]
public class RecipeSO : ScriptableObject
{
    public string recipeName;
    public Sprite icon;

    [Header("Ingredients")]
    public IngredientAmount[] requiredIngredients;

    [Header("Cooking")]
    public float cookingTime = 5f;
    public CookingStationType requiredStation = CookingStationType.Any;

    [Header("Economy")]
    public int sellPrice = 30;
    public float reputationGain = 0.05f;
}

[System.Serializable]
public struct IngredientAmount
{
    public IngredientSO ingredient;
    public int amount;
}
