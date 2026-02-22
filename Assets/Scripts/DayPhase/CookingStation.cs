using Unity.Netcode;
using UnityEngine;

public class CookingStation : NetworkBehaviour
{
    [SerializeField] private CookingStationType stationType;
    [SerializeField] private int stationId;

    public NetworkVariable<CookingStationData> StationState = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public CookingStationType StationType => stationType;
    public int StationId => stationId;
    public bool IsIdle => StationState.Value.Status == 0;
    public bool IsCooking => StationState.Value.Status == 1;
    public bool IsDone => StationState.Value.Status == 2;
    public bool IsBurned => StationState.Value.Status == 3;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StationState.Value = new CookingStationData
            {
                StationId = stationId,
                RecipeIndex = -1,
                CookProgress = 0f,
                Status = 0
            };
        }

        GameEvents.OnDayPhaseCleanup += OnDayPhaseCleanup;
    }

    public override void OnNetworkDespawn()
    {
        GameEvents.OnDayPhaseCleanup -= OnDayPhaseCleanup;
    }

    /// <summary>Start cooking a recipe. Full logic in Faz 3c.</summary>
    public void StartCooking(int recipeIndex)
    {
        if (!IsServer || !IsIdle) return;

        StationState.Value = new CookingStationData
        {
            StationId = stationId,
            RecipeIndex = recipeIndex,
            CookProgress = 0f,
            Status = 1
        };

        Debug.Log($"[CookingStation] Station {stationId} started cooking recipe {recipeIndex}");
    }

    /// <summary>Collect finished dish, returns recipe index. -1 if not done.</summary>
    public int CollectDish()
    {
        if (!IsServer || !IsDone) return -1;

        int recipe = StationState.Value.RecipeIndex;
        ClearStation();
        return recipe;
    }

    public void ClearStation()
    {
        if (!IsServer) return;

        StationState.Value = new CookingStationData
        {
            StationId = stationId,
            RecipeIndex = -1,
            CookProgress = 0f,
            Status = 0
        };
    }

    private void OnDayPhaseCleanup()
    {
        if (IsServer)
            ClearStation();
    }
}
