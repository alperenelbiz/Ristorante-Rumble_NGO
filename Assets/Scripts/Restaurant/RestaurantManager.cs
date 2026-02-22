using Unity.Netcode;
using UnityEngine;

public class RestaurantManager : NetworkBehaviour
{
    public static RestaurantManager Instance { get; private set; }

    [SerializeField] private Restaurant teamARestaurant;
    [SerializeField] private Restaurant teamBRestaurant;

    private void Awake()
    {
        Instance = this;
    }

    public Restaurant GetRestaurant(int teamId)
    {
        return teamId == TeamManager.TEAM_A ? teamARestaurant : teamBRestaurant;
    }
}
