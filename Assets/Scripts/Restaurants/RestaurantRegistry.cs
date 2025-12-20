using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class RestaurantRegistry : MonoBehaviour
{
    private static RestaurantRegistry _instance;
    public static RestaurantRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("RestaurantRegistry");
                _instance = go.AddComponent<RestaurantRegistry>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly List<Restaurant> _restaurants = new List<Restaurant>();

    public static void Register(Restaurant r)
    {
        if (!Instance._restaurants.Contains(r))
            Instance._restaurants.Add(r);
    }

    public static void Unregister(Restaurant r)
    {
        if (Instance == null) return;
        Instance._restaurants.Remove(r);
    }

    public static Restaurant GetRandomRestaurant(System.Func<Restaurant, bool> predicate = null)
    {
        var list = Instance._restaurants;
        if (list.Count == 0) return null;

        if (predicate != null)
        {
            var filtered = list.FindAll(r => predicate(r));
            if (filtered.Count == 0) return null;
            return filtered[Random.Range(0, filtered.Count)];
        }

        return list[Random.Range(0, list.Count)];
    }

    public static IReadOnlyList<Restaurant> All => Instance._restaurants.AsReadOnly();
}
