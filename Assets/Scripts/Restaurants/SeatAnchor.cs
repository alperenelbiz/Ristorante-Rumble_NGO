using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class SeatAnchor : NetworkBehaviour
{
    public const ulong EMPTY_SEAT = ulong.MaxValue;

    [SerializeField] private Transform sitPoint;
    [SerializeField] private Restaurant restaurant;

    // Occupancy state replicated to all clients.
    // Stores the NetworkObjectId of the occupant, or EMPTY_SEAT if unoccupied.
    private NetworkVariable<ulong> _occupantNetworkObjectId = new NetworkVariable<ulong>(
        EMPTY_SEAT, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Transform SitPoint => sitPoint != null ? sitPoint : transform;

    private void Reset()
    {
        if (sitPoint == null) sitPoint = transform;
        if (restaurant == null) restaurant = GetComponentInParent<Restaurant>();
    }

    public override void OnNetworkSpawn()
    {
        if (restaurant == null) restaurant = GetComponentInParent<Restaurant>();
        if (IsServer && restaurant != null)
        {
            restaurant.RegisterSeat(this);
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (restaurant == null) restaurant = GetComponentInParent<Restaurant>();
            restaurant?.RegisterSeat(this);
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            restaurant?.UnregisterSeat(this);
            // If disabled while occupied, release logically (customer should react to loss)
            _occupantNetworkObjectId.Value = EMPTY_SEAT;
        }
    }

    public bool IsFreeServer()
    {
        if (!IsServer) return false; // server is source of truth
        return _occupantNetworkObjectId.Value == EMPTY_SEAT;
    }

    // Returns true if claim succeeded
    public bool TryClaimServer(ulong occupantNetworkObjectId)
    {
        if (!IsServer) return false;
        if (_occupantNetworkObjectId.Value != EMPTY_SEAT) return false;
        _occupantNetworkObjectId.Value = occupantNetworkObjectId;
        return true;
    }

    public void ReleaseServer(ulong occupantNetworkObjectId)
    {
        if (!IsServer) return;
        if (_occupantNetworkObjectId.Value == occupantNetworkObjectId)
        {
            _occupantNetworkObjectId.Value = EMPTY_SEAT;
        }
    }

    public bool IsOccupiedClient() => _occupantNetworkObjectId.Value != EMPTY_SEAT;
}
