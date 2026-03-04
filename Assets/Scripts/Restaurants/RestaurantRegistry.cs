using System.Collections.Generic;

namespace RistoranteRumble
{
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

        public static IReadOnlyList<Restaurant> All
        {
            get
            {
                // Defensive null filtering before returning
                _restaurants.RemoveAll(r => r == null);
            return _restaurants;
            }
        }
    }
}
