using UnityEngine;

[CreateAssetMenu(fileName = "New Ingredient", menuName = "RistoranteRumble/IngredientSO")]
public class IngredientSO : ScriptableObject
{
    public string ingredientName;
    public Sprite icon;

    [Header("Economy")]
    public int purchasePrice = 10;
}
