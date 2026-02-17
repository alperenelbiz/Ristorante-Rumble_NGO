using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkObject))]
public class CustomerSpawner : NetworkBehaviour
{
    [Header("Spawning")]
    [SerializeField] private NetworkObject customerPrefab;
    [SerializeField] private float spawnIntervalSeconds = 2.0f;
    [SerializeField] private int burstPerInterval = 2;
    [SerializeField] private int maxAliveCustomers = 50;

    [Header("Placement")]
    [SerializeField] private Transform[] spawnPoints;

    private readonly List<NetworkObject> _alive = new List<NetworkObject>();
    private Coroutine _spawnLoop;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _spawnLoop = StartCoroutine(SpawnLoop());
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (IsServer)
        {
            if (_spawnLoop != null) StopCoroutine(_spawnLoop);
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong obj)
    {
        // No client-owned customers here; all are server-owned; nothing required.
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnIntervalSeconds);
        while (true)
        {
            // Cull null/despawned references
            _alive.RemoveAll(n => n == null || !n.IsSpawned);

            if (_alive.Count < maxAliveCustomers && customerPrefab != null)
            {
                int canSpawn = Mathf.Min(burstPerInterval, maxAliveCustomers - _alive.Count);
                for (int i = 0; i < canSpawn; i++)
                {
                    SpawnOne();
                }
            }

            yield return wait;
        }
    }

    private void SpawnOne()
    {
        var spawnT = ChooseSpawnPoint();
        var no = Instantiate(customerPrefab, spawnT.position, spawnT.rotation);
        no.Spawn(true); // server spawns; visible to all clients

        _alive.Add(no);
    }

    private Transform ChooseSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        return transform;
    }
}
