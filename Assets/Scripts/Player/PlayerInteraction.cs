using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactLayer;

    public NetworkVariable<CarriedItemData> CarriedItem = new(
        CarriedItemData.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private InputAction interactAction;
    private InputAction dropAction;
    private Camera mainCam;

    // C5 — interaction cooldown
    private float interactionCooldown;
    private const float INTERACTION_COOLDOWN = 0.25f;

    public bool IsCarrying => !CarriedItem.Value.IsEmpty;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        interactAction = InputSystem.actions.FindAction("Interact");
        dropAction = InputSystem.actions.FindAction("Drop");
        mainCam = Camera.main;
    }

    private void Update()
    {
        // C5 — tick cooldown
        if (interactionCooldown > 0f)
        {
            interactionCooldown -= Time.deltaTime;
            return;
        }

        if (interactAction != null && interactAction.WasPressedThisFrame())
            TryInteract();

        if (dropAction != null && dropAction.WasPressedThisFrame() && IsCarrying)
        {
            DropItemRpc();
            interactionCooldown = INTERACTION_COOLDOWN;
        }
    }

    private void TryInteract()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
        if (hits.Length == 0) return;

        // Find closest
        Collider closest = null;
        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit;
            }
        }

        if (closest == null) return;

        // CookingStation interaction
        if (closest.TryGetComponent(out CookingStation station))
        {
            HandleCookingStation(station);
            interactionCooldown = INTERACTION_COOLDOWN;
            return;
        }

        // ServingCounter interaction
        if (closest.TryGetComponent(out ServingCounter counter))
        {
            HandleServingCounter(counter);
            interactionCooldown = INTERACTION_COOLDOWN;
            return;
        }

        // IngredientSource interaction
        if (closest.TryGetComponent(out IngredientSource source))
        {
            HandleIngredientSource(source);
            interactionCooldown = INTERACTION_COOLDOWN;
            return;
        }
    }

    private void HandleCookingStation(CookingStation station)
    {
        if (station.IsIdle)
        {
            if (RecipeSelectUI.Instance != null)
                RecipeSelectUI.Instance.Open(station);
        }
        else if (station.IsDone && !IsCarrying)
        {
            CollectDishRpc(station.NetworkObjectId);
        }
        else if (station.IsBurned)
        {
            ClearStationRpc(station.NetworkObjectId);
        }
    }

    private void HandleServingCounter(ServingCounter counter)
    {
        if (IsCarrying && CarriedItem.Value.ItemType == CarriedItemType.Dish)
        {
            PlaceDishRpc(counter.NetworkObjectId, CarriedItem.Value.ItemIndex);
        }
    }

    private void HandleIngredientSource(IngredientSource source)
    {
        if (!IsCarrying)
            PickUpIngredientRpc(source.IngredientIndex);
    }

    // --- Server validation helpers ---

    // W5 — team validation
    private bool ValidateTeam(ulong senderClientId, int stationTeamId)
    {
        int playerTeam = TeamManager.Instance.GetPlayerTeam(senderClientId);
        if (playerTeam != stationTeamId)
        {
            Debug.Log($"[PlayerInteraction] Team mismatch: player {senderClientId} team {playerTeam} vs station team {stationTeamId}");
            return false;
        }
        return true;
    }

    // W6 — proximity validation
    private bool ValidateProximity(Vector3 targetPos)
    {
        var playerObj = NetworkManager.SpawnManager.GetPlayerNetworkObject(OwnerClientId);
        if (playerObj == null) return false;
        float dist = Vector3.Distance(playerObj.transform.position, targetPos);
        return dist <= interactRange * 1.5f;
    }

    // --- RPCs ---

    [Rpc(SendTo.Server)]
    private void StartCookingRpc(ulong stationNetId, int recipeIndex)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(stationNetId, out var netObj)) return;
        var station = netObj.GetComponent<CookingStation>();
        if (station == null || !station.IsIdle) return;

        // W5 — team check
        if (!ValidateTeam(OwnerClientId, station.TeamId)) return;

        // W6 — proximity check
        if (!ValidateProximity(netObj.transform.position)) return;

        station.StartCooking(recipeIndex);
        Debug.Log($"[PlayerInteraction] Player {OwnerClientId} started cooking recipe {recipeIndex} on station {station.StationId}");
    }

    [Rpc(SendTo.Server)]
    private void CollectDishRpc(ulong stationNetId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(stationNetId, out var netObj)) return;
        var station = netObj.GetComponent<CookingStation>();
        if (station == null || !station.IsDone) return;

        // W5 — team check
        if (!ValidateTeam(OwnerClientId, station.TeamId)) return;

        // W6 — proximity check
        if (!ValidateProximity(netObj.transform.position)) return;

        int recipeIndex = station.CollectDish();
        if (recipeIndex < 0) return;

        CarriedItem.Value = new CarriedItemData { ItemType = CarriedItemType.Dish, ItemIndex = recipeIndex };
        Debug.Log($"[PlayerInteraction] Player {OwnerClientId} collected dish: recipe {recipeIndex}");
    }

    [Rpc(SendTo.Server)]
    private void ClearStationRpc(ulong stationNetId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(stationNetId, out var netObj)) return;
        var station = netObj.GetComponent<CookingStation>();
        if (station == null) return;

        // W5 — team check
        if (!ValidateTeam(OwnerClientId, station.TeamId)) return;

        // W6 — proximity check
        if (!ValidateProximity(netObj.transform.position)) return;

        station.ClearStation();
    }

    [Rpc(SendTo.Server)]
    private void PlaceDishRpc(ulong counterNetId, int recipeIndex)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(counterNetId, out var netObj)) return;
        var counter = netObj.GetComponent<ServingCounter>();
        if (counter == null) return;

        // W5 — team check
        if (!ValidateTeam(OwnerClientId, counter.TeamId)) return;

        // W6 — proximity check
        if (!ValidateProximity(netObj.transform.position)) return;

        if (counter.AddDish(recipeIndex))
            CarriedItem.Value = CarriedItemData.Empty;
    }

    [Rpc(SendTo.Server)]
    private void PickUpIngredientRpc(int ingredientIndex)
    {
        if (IsCarrying) return;

        // W6 — bounds check
        if (ingredientIndex < 0 || ingredientIndex >= RecipeDatabase.Instance.IngredientCount) return;

        // TODO: proximity check skipped for MVP — infinite sources, low exploit risk

        CarriedItem.Value = new CarriedItemData { ItemType = CarriedItemType.Ingredient, ItemIndex = ingredientIndex };
        Debug.Log($"[PlayerInteraction] Player {OwnerClientId} picked up ingredient {ingredientIndex}");
    }

    [Rpc(SendTo.Server)]
    private void DropItemRpc()
    {
        CarriedItem.Value = CarriedItemData.Empty;
    }

    /// <summary>Called by RecipeSelectUI when player picks a recipe.</summary>
    public void RequestStartCooking(CookingStation station, int recipeIndex)
    {
        StartCookingRpc(station.NetworkObjectId, recipeIndex);
    }
}
