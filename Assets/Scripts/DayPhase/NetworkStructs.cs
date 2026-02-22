using System;
using Unity.Netcode;

public struct OrderData : INetworkSerializable, IEquatable<OrderData>
{
    public int OrderId;
    public int RecipeIndex;
    public float PatienceTimer;
    /// <summary>0 = waiting, 1 = completed, 2 = expired</summary>
    public byte Status;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref OrderId);
        serializer.SerializeValue(ref RecipeIndex);
        serializer.SerializeValue(ref PatienceTimer);
        serializer.SerializeValue(ref Status);
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
    /// <summary>0 = idle, 1 = cooking, 2 = done, 3 = burned</summary>
    public byte Status;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref StationId);
        serializer.SerializeValue(ref RecipeIndex);
        serializer.SerializeValue(ref CookProgress);
        serializer.SerializeValue(ref Status);
    }

    public bool Equals(CookingStationData other) => StationId == other.StationId;
    public override int GetHashCode() => StationId;
}

public struct CarriedItemData : INetworkSerializable, IEquatable<CarriedItemData>
{
    /// <summary>0 = none, 1 = ingredient, 2 = dish</summary>
    public byte ItemType;
    /// <summary>Index into RecipeDatabase (ingredient or recipe)</summary>
    public int ItemIndex;

    public bool IsEmpty => ItemType == 0;

    public static CarriedItemData Empty => new() { ItemType = 0, ItemIndex = -1 };

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemType);
        serializer.SerializeValue(ref ItemIndex);
    }

    public bool Equals(CarriedItemData other) => ItemType == other.ItemType && ItemIndex == other.ItemIndex;
    public override int GetHashCode() => HashCode.Combine(ItemType, ItemIndex);
}
