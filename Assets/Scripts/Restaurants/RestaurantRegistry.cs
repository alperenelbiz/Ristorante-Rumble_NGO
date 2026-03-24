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

        /// <summary>
        /// Find restaurant by team ID. Returns null if not found.
        /// Replaces RestaurantManager.GetRestaurant().
        /// </summary>
        public static Restaurant GetByTeamId(int teamId)
        {
            for (int i = 0; i < _restaurants.Count; i++)
            {
                var r = _restaurants[i];
                if (r != null && r.TeamId == teamId)
                    return r;
            }
            return null;
        }

        private static readonly System.Predicate<Restaurant> _isNull = r => r == null;

        public static IReadOnlyList<Restaurant> All
        {
            get
            {
                // Defensive null filtering before returning
                _restaurants.RemoveAll(_isNull);
            return _restaurants;
            }
        }
    }
}
