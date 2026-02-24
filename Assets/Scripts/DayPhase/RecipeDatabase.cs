using UnityEngine;

public class RecipeDatabase : MonoBehaviour
{
    public static RecipeDatabase Instance { get; private set; }

    [SerializeField] private RecipeSO[] recipes;
    [SerializeField] private IngredientSO[] ingredients;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public RecipeSO GetRecipe(int index)
    {
        if (index < 0 || index >= recipes.Length) return null;
        return recipes[index];
    }

    public IngredientSO GetIngredient(int index)
    {
        if (index < 0 || index >= ingredients.Length) return null;
        return ingredients[index];
    }

    public int GetRecipeIndex(RecipeSO recipe)
    {
        for (int i = 0; i < recipes.Length; i++)
            if (recipes[i] == recipe) return i;
        return -1;
    }

    public int GetIngredientIndex(IngredientSO ingredient)
    {
        for (int i = 0; i < ingredients.Length; i++)
            if (ingredients[i] == ingredient) return i;
        return -1;
    }

    public int RecipeCount => recipes.Length;
    public int IngredientCount => ingredients.Length;
}
