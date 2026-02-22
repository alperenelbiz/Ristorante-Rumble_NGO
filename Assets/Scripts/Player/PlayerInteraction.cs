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
        if (interactAction != null && interactAction.WasPressedThisFrame())
            TryInteract();

        if (dropAction != null && dropAction.WasPressedThisFrame() && IsCarrying)
            DropItemRpc();
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
            return;
        }

        // ServingCounter interaction
        if (closest.TryGetComponent(out ServingCounter counter))
        {
            HandleServingCounter(counter);
            return;
        }

        // IngredientSource interaction (future use)
        if (closest.TryGetComponent(out IngredientSource source))
        {
            HandleIngredientSource(source);
            return;
        }
    }

    private void HandleCookingStation(CookingStation station)
    {
        if (station.IsIdle)
        {
            // Open recipe select UI
            if (RecipeSelectUI.Instance != null)
                RecipeSelectUI.Instance.Open(station);
        }
        else if (station.IsDone && !IsCarrying)
        {
            // Pick up finished dish
            CollectDishRpc(station.NetworkObjectId);
        }
        else if (station.IsBurned)
        {
            // Clear burned station
            ClearStationRpc(station.NetworkObjectId);
        }
    }

    private void HandleServingCounter(ServingCounter counter)
    {
        if (IsCarrying && CarriedItem.Value.ItemType == 2)
        {
            // Place dish on counter
            PlaceDishRpc(counter.NetworkObjectId, CarriedItem.Value.ItemIndex);
        }
    }

    private void HandleIngredientSource(IngredientSource source)
    {
        if (!IsCarrying)
            PickUpIngredientRpc(source.IngredientIndex);
    }

    // --- RPCs ---

    [Rpc(SendTo.Server)]
    private void StartCookingRpc(ulong stationNetId, int recipeIndex)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(stationNetId, out var netObj)) return;
        var station = netObj.GetComponent<CookingStation>();
        if (station == null || !station.IsIdle) return;

        station.StartCooking(recipeIndex);
        Debug.Log($"[PlayerInteraction] Player {OwnerClientId} started cooking recipe {recipeIndex} on station {station.StationId}");
    }

    [Rpc(SendTo.Server)]
    private void CollectDishRpc(ulong stationNetId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(stationNetId, out var netObj)) return;
        var station = netObj.GetComponent<CookingStation>();
        if (station == null || !station.IsDone) return;

        int recipeIndex = station.CollectDish();
        if (recipeIndex < 0) return;

        CarriedItem.Value = new CarriedItemData { ItemType = 2, ItemIndex = recipeIndex };
        Debug.Log($"[PlayerInteraction] Player {OwnerClientId} collected dish: recipe {recipeIndex}");
    }

    [Rpc(SendTo.Server)]
    private void ClearStationRpc(ulong stationNetId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(stationNetId, out var netObj)) return;
        var station = netObj.GetComponent<CookingStation>();
        if (station == null) return;

        station.ClearStation();
    }

    [Rpc(SendTo.Server)]
    private void PlaceDishRpc(ulong counterNetId, int recipeIndex)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(counterNetId, out var netObj)) return;
        var counter = netObj.GetComponent<ServingCounter>();
        if (counter == null) return;

        if (counter.AddDish(recipeIndex))
            CarriedItem.Value = CarriedItemData.Empty;
    }

    [Rpc(SendTo.Server)]
    private void PickUpIngredientRpc(int ingredientIndex)
    {
        if (IsCarrying) return;
        CarriedItem.Value = new CarriedItemData { ItemType = 1, ItemIndex = ingredientIndex };
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
