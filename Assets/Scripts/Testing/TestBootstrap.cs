using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Auto-starts host on Play for test scenes. Drop into any test scene.
/// </summary>
public class TestBootstrap : MonoBehaviour
{
    [SerializeField] private bool autoStartHost = true;
    [SerializeField] private bool autoSpawnPlayer = true;
    [SerializeField] private bool autoAssignTeam = true;

    private IEnumerator Start()
    {
        GameEvents.ResetAll();
        Debug.Log("[TestBootstrap] Events reset");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[TestBootstrap] No NetworkManager in scene");
            yield break;
        }

        if (autoStartHost)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("[TestBootstrap] Host started");
        }

        // Wait one frame for NetworkBehaviours to spawn
        yield return null;

        if (autoAssignTeam && TeamManager.Instance != null)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            TeamManager.Instance.AssignPlayerToTeam(localId, TeamManager.TEAM_A);
            Debug.Log($"[TestBootstrap] Local client {localId} assigned to TeamA");
        }

        if (autoSpawnPlayer && PlayerSpawnManager.Instance != null)
        {
            Debug.Log("[TestBootstrap] PlayerSpawnManager active — auto-spawn handled via OnClientConnected");
        }

        Debug.Log("[TestBootstrap] Host started, ready for testing");
    }
}
