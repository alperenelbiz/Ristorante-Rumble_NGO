using UnityEngine;

[CreateAssetMenu(fileName = "DayPhaseSettings", menuName = "RistoranteRumble/DayPhaseSettings")]
public class DayPhaseSettingsSO : ScriptableObject
{
    [Header("Economy")]
    public int startingMoney = 200;

    [Header("Kitchen")]
    public int maxDishesOnCounter = 3;
    public float burnTimeAfterDone = 15f;

    [Header("MVP")]
    [Tooltip("true = infinite ingredient sources, fridge/shop deferred to Faz 4")]
    public bool ingredientSourceInfinite = true;
}
