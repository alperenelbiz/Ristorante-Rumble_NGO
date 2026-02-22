using UnityEngine;

public class Table : MonoBehaviour
{
    [SerializeField] private Transform seatPosition;

    public Transform SeatPosition => seatPosition;
    public bool IsOccupied { get; private set; }

    public void AssignCustomer()
    {
        IsOccupied = true;
    }

    public void ReleaseCustomer()
    {
        IsOccupied = false;
    }
}
