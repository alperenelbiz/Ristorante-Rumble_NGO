using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace RistoranteRumble
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAgent : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float sitSnapDistance = 0.35f;
        [SerializeField] private float repathInterval = 0.5f;
        [SerializeField] private float arriveThreshold = 0.6f;

        [Header("Behavior")]
        [SerializeField] private float giveUpIfNoSeatAfterSeconds = 10f;

        [Header("Seated Timer")]
        [SerializeField] private float seatedDurationSeconds = 12f;

        [Header("Exit / Destroy")]
        [SerializeField] private Transform[] destroyPoints;
        [SerializeField] private float maxExitTravelSeconds = 20f;

        [Header("Timeouts")]
        [SerializeField] private float maxWaitUntilReachedSeconds = 30f;

        private const float ENTRY_ARRIVE_THRESHOLD = 1.25f;
        private const float MIN_SEATED_DURATION = 0.1f;

        private NavMeshAgent _agent;
        private Animator _animator;
        private SeatAnchor _reservedSeat;
        private Restaurant _targetRestaurant;
        private float _seatSearchStart;
        private Coroutine _mainRoutine;

        private ulong _netObjectId;

        private static Transform _cachedTaggedDestroyPoint;
        private static bool _destroyPointLookedUp;
        private static readonly WaitForSeconds _seatRetryWait = new WaitForSeconds(0.5f);

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            _netObjectId = GetComponent<NetworkObject>().NetworkObjectId;

            if (IsServer)
            {
                StartServerBehavior();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                if (_mainRoutine != null)
                {
                    StopCoroutine(_mainRoutine);
                    _mainRoutine = null;
                }

                if (_reservedSeat != null)
                {
                    _reservedSeat.ReleaseServer(_netObjectId);
                    _reservedSeat = null;
                }
            }
        }

        private void StartServerBehavior()
        {
            if (_mainRoutine != null) StopCoroutine(_mainRoutine);
            _mainRoutine = StartCoroutine(ServerLifecycle());
        }

        private IEnumerator ServerLifecycle()
        {
            _seatSearchStart = Time.time;

            // Pick a restaurant that currently has a free seat (claim immediately).
            if (!TryPickRestaurantWithFreeSeat(out _targetRestaurant, out _reservedSeat))
            {
                // No free seats anywhere; wait and keep re-evaluating globally.
                yield return WaitForAnySeatThenMove();
            }

            // If we still have no seat after waiting (timeout despawned us), stop.
            if (_reservedSeat == null)
                yield break;

            // Move to seat
            yield return MoveToSeatRoutine();

            // Seated timer
            yield return SeatedRoutine();

            // Leave to destroy point and despawn
            yield return LeaveAndDespawnRoutine();
        }

        private IEnumerator ServerDespawnNextFrame()
        {
            yield return null;
            if (this != null && NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
        }

        // Atomic seat selection using Restaurant.TryClaimRandomFreeSeat.
        // Single-pass with reservoir sampling across all restaurants — no race condition.
        private bool TryPickRestaurantWithFreeSeat(out Restaurant restaurant, out SeatAnchor seat)
        {
            restaurant = null;
            seat = null;

            var all = RestaurantRegistry.All;
            if (all == null || all.Count == 0) return false;

            // Single pass: try to atomically claim a seat in each restaurant.
            // Reservoir sampling across successes to distribute customers evenly.
            int claimedCount = 0;

            for (int i = 0; i < all.Count; i++)
            {
                var r = all[i];
                if (r == null) continue;

                if (!r.TryClaimRandomFreeSeat(_netObjectId, out var candidateSeat))
                    continue;

                claimedCount++;
                if (Random.Range(0, claimedCount) == 0)
                {
                    // Release previous pick if we had one
                    if (seat != null)
                        seat.ReleaseServer(_netObjectId);

                    restaurant = r;
                    seat = candidateSeat;
                }
                else
                {
                    // Release this one, keeping the earlier pick
                    candidateSeat.ReleaseServer(_netObjectId);
                }
            }

            return seat != null;
        }

        private IEnumerator WaitForAnySeatThenMove()
        {
            while (Time.time - _seatSearchStart < giveUpIfNoSeatAfterSeconds)
            {
                if (TryPickRestaurantWithFreeSeat(out _targetRestaurant, out _reservedSeat))
                    yield break;

                yield return _seatRetryWait;
            }

            // Timeout: no seats anywhere; despawn.
            yield return ServerDespawnNextFrame();
        }

        private IEnumerator MoveToSeatRoutine()
        {
            // Optional entry flow
            var entry = _targetRestaurant != null ? _targetRestaurant.GetRandomEntry() : null;
            if (entry != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(entry.position);
                yield return WaitUntilReached(entry.position, ENTRY_ARRIVE_THRESHOLD);
            }

            float nextRepath = 0f;
            float seatNavDeadline = Time.time + maxWaitUntilReachedSeconds;

            while (true)
            {
                if (_reservedSeat == null)
                {
                    // Seat removed; reselect globally.
                    _seatSearchStart = Time.time;
                    if (!TryPickRestaurantWithFreeSeat(out _targetRestaurant, out _reservedSeat))
                    {
                        yield return WaitForAnySeatThenMove();
                        if (_reservedSeat == null)
                            yield break; // despawned already
                    }
                    nextRepath = 0f;
                    seatNavDeadline = Time.time + maxWaitUntilReachedSeconds;
                }

                // Timeout: can't reach seat — release it and despawn
                if (Time.time >= seatNavDeadline)
                {
                    _agent.isStopped = true;
                    if (_reservedSeat != null)
                    {
                        _reservedSeat.ReleaseServer(_netObjectId);
                        _reservedSeat = null;
                    }
                    yield return ServerDespawnNextFrame();
                    yield break;
                }

                Vector3 seatPos = _reservedSeat.SitPoint.position;

                if (Time.time >= nextRepath)
                {
                    nextRepath = Time.time + repathInterval;
                    _agent.isStopped = false;
                    _agent.SetDestination(seatPos);
                }

                if (Vector3.Distance(transform.position, seatPos) <= sitSnapDistance)
                {
                    // Arrive and sit
                    transform.position = seatPos;
                    _agent.isStopped = true;
                    transform.rotation = _reservedSeat.SitPoint.rotation;

                    SitClientRpc(true);
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator SeatedRoutine()
        {
            // Stay seated for duration
            float end = Time.time + Mathf.Max(MIN_SEATED_DURATION, seatedDurationSeconds);
            while (Time.time < end)
            {
                yield return null;
            }
        }

        private IEnumerator LeaveAndDespawnRoutine()
        {
            // Stand up (client anim)
            SitClientRpc(false);

            // Release seat before leaving so others can use it
            if (_reservedSeat != null)
            {
                _reservedSeat.ReleaseServer(_netObjectId);
                _reservedSeat = null;
            }

            // Pick destroy point
            Transform destroyT = ChooseDestroyPoint();
            if (destroyT == null)
            {
                // No destroy point configured -> just despawn safely
                yield return ServerDespawnNextFrame();
                yield break;
            }

            _agent.isStopped = false;
            _agent.SetDestination(destroyT.position);

            float giveUpAt = Time.time + Mathf.Max(1f, maxExitTravelSeconds);

            // Move until reached (or timeout)
            while (Time.time < giveUpAt)
            {
                if (Vector3.Distance(transform.position, destroyT.position) <= arriveThreshold)
                {
                    yield return ServerDespawnNextFrame();
                    yield break;
                }
                yield return null;
            }

            // If pathing fails, force despawn
            yield return ServerDespawnNextFrame();
        }

        private Transform ChooseDestroyPoint()
        {
            if (destroyPoints != null && destroyPoints.Length > 0)
                return destroyPoints[Random.Range(0, destroyPoints.Length)];

            // Fallback: cache the tag lookup so it only runs once across all customers
            if (!_destroyPointLookedUp)
            {
                _destroyPointLookedUp = true;
                var tagged = GameObject.FindGameObjectWithTag("CustomerDestroyPoint");
                _cachedTaggedDestroyPoint = tagged != null ? tagged.transform : null;
            }
            return _cachedTaggedDestroyPoint;
        }

        private IEnumerator WaitUntilReached(Vector3 target, float threshold)
        {
            float deadline = Time.time + maxWaitUntilReachedSeconds;
            while (Vector3.Distance(transform.position, target) > threshold)
            {
                if (Time.time >= deadline)
                {
                    // Timeout: stop agent and bail out
                    _agent.isStopped = true;
                    yield break;
                }
                yield return null;
            }
        }

        [ClientRpc]
        private void SitClientRpc(bool isSitting)
        {
            if (_animator != null)
                _animator.SetBool("IsSitting", isSitting);
        }

        // Cancel main coroutine before starting despawn to prevent two
        // coroutines fighting over the same NavMeshAgent.
        public void ForceLeaveServer()
        {
            if (!IsServer) return;

            // Stop the main lifecycle coroutine first
            if (_mainRoutine != null)
            {
                StopCoroutine(_mainRoutine);
                _mainRoutine = null;
            }

            // Release and despawn now
            if (_reservedSeat != null)
            {
                _reservedSeat.ReleaseServer(_netObjectId);
                _reservedSeat = null;
            }

            StartCoroutine(ServerDespawnNextFrame());
        }

        // Guard against accessing IsServer after network teardown.
        private void OnDestroy()
        {
            if (IsSpawned && IsServer && _reservedSeat != null)
            {
                _reservedSeat.ReleaseServer(_netObjectId);
            }
        }
    }
}
