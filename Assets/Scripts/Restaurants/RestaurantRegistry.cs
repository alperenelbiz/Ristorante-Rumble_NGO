using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure static registry of all active Restaurant instances.
/// No MonoBehaviour/DontDestroyOnLoad — restaurants register/unregister themselves.
/// </summary>
public static class RestaurantRegistry
{
    private static readonly List<Restaurant> _restaurants = new List<Restaurant>();

    public static void Register(Restaurant r)
    {
        if (r != null && !_restaurants.Contains(r))
            _restaurants.Add(r);
    }

    public static void Unregister(Restaurant r)
    {
        _restaurants.Remove(r);
    }

    /// <summary>
    /// Clears the entire registry. Call on scene transitions to avoid stale references.
    /// </summary>
    public static void Clear()
    {
        _restaurants.Clear();
    }

    public static Restaurant GetRandomRestaurant(System.Func<Restaurant, bool> predicate = null)
    {
        // Defensive null filtering
        _restaurants.RemoveAll(r => r == null);

        if (_restaurants.Count == 0) return null;

        if (predicate != null)
        {
            var filtered = _restaurants.FindAll(r => predicate(r));
            if (filtered.Count == 0) return null;
            return filtered[Random.Range(0, filtered.Count)];
        }

        return _restaurants[Random.Range(0, _restaurants.Count)];
    }

    public static IReadOnlyList<Restaurant> All
    {
        get
        {
            // Defensive null filtering before returning
            _restaurants.RemoveAll(r => r == null);
            return _restaurants.AsReadOnly();
        }
    }
}
