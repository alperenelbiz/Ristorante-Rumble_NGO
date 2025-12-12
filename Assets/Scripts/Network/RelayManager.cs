using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    
    public string CurrentJoinCode { get; private set; }
    public bool IsRelayActive { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task<string> CreateRelay(int maxConnections = 9)
    {
        try
        {
            Debug.Log($"[RelayManager] Creating relay for {maxConnections} connections...");
            
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            CurrentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            
            IsRelayActive = true;
            Debug.Log($"[RelayManager] Relay created! Join Code: {CurrentJoinCode}");
            
            return CurrentJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayManager] Create relay failed: {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"[RelayManager] Joining relay with code: {joinCode}...");
            
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
            
            CurrentJoinCode = joinCode;
            IsRelayActive = true;
            Debug.Log("[RelayManager] Successfully joined relay!");
            
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayManager] Join relay failed: {e.Message}");
            return false;
        }
    }

    public void Cleanup()
    {
        CurrentJoinCode = null;
        IsRelayActive = false;
        Debug.Log("[RelayManager] Relay cleaned up");
    }
}
