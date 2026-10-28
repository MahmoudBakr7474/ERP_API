namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// What a count freezes: one location, or the whole warehouse when the location is null. Active from the start of the
/// count until it completes or is cancelled; every posting checks active scopes after taking its warehouse lock.
/// </summary>
public sealed class StockCountScope
{
    private StockCountScope() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CountId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LocationId { get; private set; }
    public bool IsActive { get; private set; }

    internal static StockCountScope For(Guid warehouseId, Guid? locationId) =>
        new() { WarehouseId = warehouseId, LocationId = locationId };

    internal void Activate() => IsActive = true;

    internal void Release() => IsActive = false;
}
