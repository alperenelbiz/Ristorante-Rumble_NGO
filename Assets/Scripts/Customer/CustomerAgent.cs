using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

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

    private NavMeshAgent _agent;
    private SeatAnchor _reservedSeat;
    private Restaurant _targetRestaurant;
    private float _seatSearchStart;
    private Coroutine _mainRoutine;

    private ulong _netObjectId;

    private enum State { SeekingSeat, MovingToSeat, Seated, Leaving }
    private State _state = State.SeekingSeat;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        _netObjectId = GetComponent<NetworkObject>().NetworkObjectId;

        if (IsServer)
        {
            StartServerBehavior();
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
        _state = State.SeekingSeat;

        // Pick a restaurant that currently has a free seat (claim immediately).
        if (!TryPickRestaurantWithFreeSeat(out _targetRestaurant, out _reservedSeat))
        {
            // No free seats anywhere; wait and keep re-evaluating globally.
            yield return WaitForAnySeatThenMove();
        }

        // Fix #2: If we still have no seat after waiting (timeout despawned us), stop.
        if (_reservedSeat == null)
            yield break;

        // Move to seat
        _state = State.MovingToSeat;
        yield return MoveToSeatRoutine();

        // Seated timer
        _state = State.Seated;
        yield return SeatedRoutine();

        // Leave to destroy point and despawn
        _state = State.Leaving;
        yield return LeaveAndDespawnRoutine();
    }

    private IEnumerator ServerDespawnNextFrame()
    {
        yield return null;
        if (this != null && NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    // Fix #1: Atomic seat selection using Restaurant.TryClaimRandomFreeSeat.
    // Single-pass with reservoir sampling across all restaurants — no race condition.
    private bool TryPickRestaurantWithFreeSeat(out Restaurant restaurant, out SeatAnchor seat)
    {
        restaurant = null;
        seat = null;

        var all = RestaurantRegistry.All;
        if (all == null || all.Count == 0) return false;

        // Shuffle iteration order via reservoir sampling across restaurants
        // to avoid all customers picking the same restaurant.
        int restaurantsSeen = 0;
        Restaurant chosenRestaurant = null;

        for (int i = 0; i < all.Count; i++)
        {
            var r = all[i];
            if (r == null) continue;

            // Check if this restaurant has any free seat at all (quick check)
            if (r.GetRandomFreeSeat() == null) continue;

            restaurantsSeen++;
            if (Random.Range(0, restaurantsSeen) == 0)
                chosenRestaurant = r;
        }

        if (chosenRestaurant == null) return false;

        // Atomically claim a random free seat in the chosen restaurant
        if (!chosenRestaurant.TryClaimRandomFreeSeat(_netObjectId, out var claimedSeat))
            return false;

        restaurant = chosenRestaurant;
        seat = claimedSeat;
        return true;
    }

    private IEnumerator WaitForAnySeatThenMove()
    {
        while (Time.time - _seatSearchStart < giveUpIfNoSeatAfterSeconds)
        {
            if (TryPickRestaurantWithFreeSeat(out _targetRestaurant, out _reservedSeat))
                yield break;

            yield return new WaitForSeconds(0.5f);
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
            yield return WaitUntilReached(entry.position, 1.25f);
        }

        float nextRepath = 0f;

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
        float end = Time.time + Mathf.Max(0.1f, seatedDurationSeconds);
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

        var tagged = GameObject.FindGameObjectWithTag("CustomerDestroyPoint");
        return tagged != null ? tagged.transform : null;
    }

    // Fix #6: Added timeout to prevent infinite loop when target is unreachable.
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
        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetBool("IsSitting", isSitting);
    }

    // Fix #5: Cancel main coroutine before starting despawn to prevent two
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

    // Fix #7: Guard against accessing IsServer after network teardown.
    private void OnDestroy()
    {
        if (IsSpawned && IsServer && _reservedSeat != null)
        {
            _reservedSeat.ReleaseServer(_netObjectId);
        }
    }
}
