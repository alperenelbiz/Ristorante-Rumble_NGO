using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Restaurant : NetworkBehaviour
{
    [Tooltip("Optional: used to bias customers toward visible/accessible entry points.")]
    public Transform[] entryPoints;

    private readonly List<SeatAnchor> _seats = new List<SeatAnchor>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            RestaurantRegistry.Register(this);
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        RestaurantRegistry.Register(this);
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        RestaurantRegistry.Unregister(this);
    }

    internal void RegisterSeat(SeatAnchor seat)
    {
        if (!_seats.Contains(seat))
            _seats.Add(seat);
    }

    internal void UnregisterSeat(SeatAnchor seat)
    {
        _seats.Remove(seat);
    }

    public IReadOnlyList<SeatAnchor> Seats => _seats.AsReadOnly();

    public SeatAnchor GetRandomFreeSeat()
    {
        // Build a list of currently free seats
        var free = _seats.FindAll(s => s && s.IsFreeServer());
        if (free.Count == 0) return null;
        return free[Random.Range(0, free.Count)];
    }

    public Transform GetRandomEntry()
    {
        if (entryPoints != null && entryPoints.Length > 0)
            return entryPoints[Random.Range(0, entryPoints.Length)];
        return transform; // fallback
    }
}


