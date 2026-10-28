namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// Places part of a line's base quantity: out of a location (From), into one (To), or both for a move. A lot goes here
/// once lot tracking exists. Posting turns each side into a movement with a non-null location.
/// </summary>
public sealed class StockAllocation
{
    private StockAllocation() { } // EF Core

    public Guid Id { get; private set; }
    public Guid LineId { get; private set; }
    public Guid? FromWarehouseId { get; private set; }
    public Guid? FromLocationId { get; private set; }
    public Guid? ToWarehouseId { get; private set; }
    public Guid? ToLocationId { get; private set; }

    /// <summary>Always null until lot tracking is built (a pre-production dependency for batch-tracked goods).</summary>
    public Guid? LotId { get; private set; }

    public decimal BaseQuantity { get; private set; }

    internal static StockAllocation Into(Guid warehouseId, Guid locationId, decimal baseQuantity) =>
        new() { ToWarehouseId = warehouseId, ToLocationId = locationId, BaseQuantity = baseQuantity };

    internal static StockAllocation OutOf(Guid warehouseId, Guid locationId, decimal baseQuantity) =>
        new() { FromWarehouseId = warehouseId, FromLocationId = locationId, BaseQuantity = baseQuantity };

    /// <summary>The same quantity moved the other way.</summary>
    internal StockAllocation Reversed() => new()
    {
        FromWarehouseId = ToWarehouseId,
        FromLocationId = ToLocationId,
        ToWarehouseId = FromWarehouseId,
        ToLocationId = FromLocationId,
        LotId = LotId,
        BaseQuantity = BaseQuantity,
    };
}
