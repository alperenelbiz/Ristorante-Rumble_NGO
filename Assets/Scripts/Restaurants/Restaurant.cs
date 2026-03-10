using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace RistoranteRumble
{
    public class Restaurant : NetworkBehaviour
    {
        [Tooltip("Optional: used to bias customers toward visible/accessible entry points.")]
        [SerializeField] private Transform[] entryPoints;

        private readonly List<SeatAnchor> _seats = new List<SeatAnchor>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                RestaurantRegistry.Register(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                RestaurantRegistry.Unregister(this);
            }
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

        public IReadOnlyList<SeatAnchor> Seats => _seats;

        /// <summary>
        /// Atomically finds a random free seat and claims it in a single pass.
        /// Uses reservoir sampling — zero allocation, no race window.
        /// </summary>
        public bool TryClaimRandomFreeSeat(ulong netObjectId, out SeatAnchor seat)
        {
            seat = null;
            int seen = 0;

            for (int i = 0; i < _seats.Count; i++)
            {
                var s = _seats[i];
                if (s == null || !s.IsFreeServer()) continue;

                seen++;
                // Reservoir sampling: pick this seat with probability 1/seen
                if (Random.Range(0, seen) == 0)
                    seat = s;
            }

            if (seat == null) return false;

            // Attempt atomic claim
            if (!seat.TryClaimServer(netObjectId))
            {
                seat = null;
                return false;
            }

            return true;
        }

        public Transform GetRandomEntry()
        {
            if (entryPoints != null && entryPoints.Length > 0)
                return entryPoints[Random.Range(0, entryPoints.Length)];
            return transform; // fallback
        }
    }
}
