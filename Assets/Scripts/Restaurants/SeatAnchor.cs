using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class SeatAnchor : NetworkBehaviour
{
    [SerializeField] private Transform sitPoint;
    [SerializeField] private Restaurant restaurant;

    // Occupancy state replicated to all clients.
    private NetworkVariable<ulong> _occupantClientId = new NetworkVariable<ulong>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
            _occupantClientId.Value = 0;
        }
    }

    public bool IsFreeServer()
    {
        if (!IsServer) return false; // server is source of truth
        return _occupantClientId.Value == 0;
    }

    // Returns true if claim succeeded
    public bool TryClaimServer(ulong occupantNetworkObjectId)
    {
        if (!IsServer) return false;
        if (_occupantClientId.Value != 0) return false;
        _occupantClientId.Value = occupantNetworkObjectId;
        return true;
    }

    public void ReleaseServer(ulong occupantNetworkObjectId)
    {
        if (!IsServer) return;
        if (_occupantClientId.Value == occupantNetworkObjectId)
        {
            _occupantClientId.Value = 0;
        }
    }

    public bool IsOccupiedClient() => _occupantClientId.Value != 0;
}
