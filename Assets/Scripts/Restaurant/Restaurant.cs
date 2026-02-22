using UnityEngine;

public class Restaurant : MonoBehaviour
{
    [SerializeField] private int teamId;
    [SerializeField] private CookingStation[] cookingStations;
    [SerializeField] private IngredientSource[] ingredientSources;
    [SerializeField] private ServingCounter servingCounter;
    [SerializeField] private Table[] tables;
    [SerializeField] private Transform doorPosition;
    [SerializeField] private Transform exitPosition;

    public int TeamId => teamId;
    public CookingStation[] CookingStations => cookingStations;
    public IngredientSource[] IngredientSources => ingredientSources;
    public ServingCounter ServingCounter => servingCounter;
    public Table[] Tables => tables;
    public Transform DoorPosition => doorPosition;
    public Transform ExitPosition => exitPosition;
}
