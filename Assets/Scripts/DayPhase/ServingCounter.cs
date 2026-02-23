using Unity.Netcode;
using UnityEngine;

public class ServingCounter : NetworkBehaviour
{
    [SerializeField] private int maxDishes = 3;
    [SerializeField] private int teamId;

    public int TeamId => teamId;

    private NetworkList<int> platedDishes;

    private void Awake()
    {
        platedDishes = new NetworkList<int>();
    }

    public int GetDishCount() => platedDishes.Count;

    public bool HasDish(int recipeIndex)
    {
        for (int i = 0; i < platedDishes.Count; i++)
            if (platedDishes[i] == recipeIndex) return true;
        return false;
    }

    public bool AddDish(int recipeIndex)
    {
        if (!IsServer) return false;
        if (platedDishes.Count >= maxDishes) return false;

        platedDishes.Add(recipeIndex);
        Debug.Log($"[ServingCounter] Dish added: recipe {recipeIndex} (total: {platedDishes.Count})");
        return true;
    }

    public bool RemoveDish(int recipeIndex)
    {
        if (!IsServer) return false;

        for (int i = 0; i < platedDishes.Count; i++)
        {
            if (platedDishes[i] == recipeIndex)
            {
                platedDishes.RemoveAt(i);
                Debug.Log($"[ServingCounter] Dish removed: recipe {recipeIndex} (total: {platedDishes.Count})");
                return true;
            }
        }
        return false;
    }

    public void ClearAll()
    {
        if (!IsServer) return;
        platedDishes.Clear();
    }
}
