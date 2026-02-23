using System;
using Unity.Netcode;

public enum OrderStatus : byte
{
    Waiting = 0,
    Completed = 1,
    Expired = 2
}

public enum CookingStatus : byte
{
    Idle = 0,
    Cooking = 1,
    Done = 2,
    Burned = 3
}

public enum CarriedItemType : byte
{
    None = 0,
    Ingredient = 1,
    Dish = 2
}

public struct OrderData : INetworkSerializable, IEquatable<OrderData>
{
    public int OrderId;
    public int RecipeIndex;
    public float PatienceTimer;
    public OrderStatus Status;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref OrderId);
        serializer.SerializeValue(ref RecipeIndex);
        serializer.SerializeValue(ref PatienceTimer);
        byte status = (byte)Status;
        serializer.SerializeValue(ref status);
        Status = (OrderStatus)status;
    }

    public bool Equals(OrderData other) => OrderId == other.OrderId;
    public override int GetHashCode() => OrderId;
}

public struct CookingStationData : INetworkSerializable, IEquatable<CookingStationData>
{
    public int StationId;
    /// <summary>-1 = idle</summary>
    public int RecipeIndex;
    /// <summary>0-1 progress</summary>
    public float CookProgress;
    public CookingStatus Status;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref StationId);
        serializer.SerializeValue(ref RecipeIndex);
        serializer.SerializeValue(ref CookProgress);
        byte status = (byte)Status;
        serializer.SerializeValue(ref status);
        Status = (CookingStatus)status;
    }

    public bool Equals(CookingStationData other) => StationId == other.StationId;
    public override int GetHashCode() => StationId;
}

public struct CarriedItemData : INetworkSerializable, IEquatable<CarriedItemData>
{
    public CarriedItemType ItemType;
    /// <summary>Index into RecipeDatabase (ingredient or recipe)</summary>
    public int ItemIndex;

    public bool IsEmpty => ItemType == CarriedItemType.None;

    public static CarriedItemData Empty => new() { ItemType = CarriedItemType.None, ItemIndex = -1 };

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        byte itemType = (byte)ItemType;
        serializer.SerializeValue(ref itemType);
        ItemType = (CarriedItemType)itemType;
        serializer.SerializeValue(ref ItemIndex);
    }

    public bool Equals(CarriedItemData other) => ItemType == other.ItemType && ItemIndex == other.ItemIndex;
    public override int GetHashCode() => HashCode.Combine(ItemType, ItemIndex);
}
