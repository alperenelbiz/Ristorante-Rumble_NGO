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
    public bool IsIdle => StationState.Value.Status == CookingStatus.Idle;
    public bool IsCooking => StationState.Value.Status == CookingStatus.Cooking;
    public bool IsDone => StationState.Value.Status == CookingStatus.Done;
    public bool IsBurned => StationState.Value.Status == CookingStatus.Burned;

    private float burnTimer;
    private float syncTimer;
    private const float SYNC_INTERVAL = 0.1f;
    private float localProgress;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StationState.Value = new CookingStationData
            {
                StationId = stationId,
                RecipeIndex = -1,
                CookProgress = 0f,
                Status = CookingStatus.Idle
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

        // W4 — use localProgress instead of stale state.CookProgress
        localProgress += Time.deltaTime / recipe.cookingTime;
        float progress = localProgress;

        if (progress >= 1f)
        {
            StationState.Value = new CookingStationData
            {
                StationId = stationId,
                RecipeIndex = state.RecipeIndex,
                CookProgress = 1f,
                Status = CookingStatus.Done
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
                Status = CookingStatus.Cooking
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
                Status = CookingStatus.Burned
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
            Status = CookingStatus.Cooking
        };

        localProgress = 0f;
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

        localProgress = 0f;
        StationState.Value = new CookingStationData
        {
            StationId = stationId,
            RecipeIndex = -1,
            CookProgress = 0f,
            Status = CookingStatus.Idle
        };
    }

    private void OnDayPhaseCleanup()
    {
        if (IsServer)
            ClearStation();
    }
}
