using Unity.Netcode;
using UnityEngine;

public class CookingStation : NetworkBehaviour
{
    [SerializeField] private CookingStationType stationType;
    [SerializeField] private int stationId;
    [SerializeField] private int teamId;
    [SerializeField] private DayPhaseSettingsSO settings;

    public NetworkVariable<CookingStationData> StationState = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public CookingStationType StationType => stationType;
    public int StationId => stationId;
    public int TeamId => teamId;
    public bool IsIdle => StationState.Value.Status == 0;
    public bool IsCooking => StationState.Value.Status == 1;
    public bool IsDone => StationState.Value.Status == 2;
    public bool IsBurned => StationState.Value.Status == 3;

    private float burnTimer;
    private float syncTimer;
    private const float SYNC_INTERVAL = 0.1f;

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

    private void Update()
    {
        if (!IsServer) return;

        if (IsCooking)
            TickCooking();
        else if (IsDone)
            TickBurnTimer();
    }

    private void TickCooking()
    {
        var state = StationState.Value;
        var recipe = RecipeDatabase.Instance.GetRecipe(state.RecipeIndex);
        if (recipe == null) { ClearStation(); return; }

        float progress = state.CookProgress + Time.deltaTime / recipe.cookingTime;

        if (progress >= 1f)
        {
            StationState.Value = new CookingStationData
            {
                StationId = stationId,
                RecipeIndex = state.RecipeIndex,
                CookProgress = 1f,
                Status = 2
            };
            burnTimer = settings.burnTimeAfterDone;
            GameEvents.CookingCompleted(teamId, stationId, state.RecipeIndex);
            Debug.Log($"[CookingStation] Station {stationId} done — recipe {state.RecipeIndex}");
            return;
        }

        // Throttle NetworkVariable writes to ~10Hz
        syncTimer += Time.deltaTime;
        if (syncTimer >= SYNC_INTERVAL)
        {
            syncTimer = 0f;
            StationState.Value = new CookingStationData
            {
                StationId = stationId,
                RecipeIndex = state.RecipeIndex,
                CookProgress = progress,
                Status = 1
            };
        }
    }

    private void TickBurnTimer()
    {
        burnTimer -= Time.deltaTime;
        if (burnTimer <= 0f)
        {
            var state = StationState.Value;
            StationState.Value = new CookingStationData
            {
                StationId = stationId,
                RecipeIndex = state.RecipeIndex,
                CookProgress = 1f,
                Status = 3
            };
            Debug.Log($"[CookingStation] Station {stationId} BURNED!");
        }
    }

    public void StartCooking(int recipeIndex)
    {
        if (!IsServer || !IsIdle) return;

        var recipe = RecipeDatabase.Instance.GetRecipe(recipeIndex);
        if (recipe == null) return;

        if (recipe.requiredStation != CookingStationType.Any && recipe.requiredStation != stationType)
        {
            Debug.Log($"[CookingStation] Station {stationId} wrong type for recipe {recipeIndex}");
            return;
        }

        StationState.Value = new CookingStationData
        {
            StationId = stationId,
            RecipeIndex = recipeIndex,
            CookProgress = 0f,
            Status = 1
        };

        syncTimer = 0f;
        GameEvents.CookingStarted(teamId, stationId);
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
