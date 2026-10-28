namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// Inventory's part in deactivating a warehouse or a location (Organization). Call it inside the database transaction
/// that deactivates: it takes the warehouse's exclusive inventory lock, held until that transaction ends, so no
/// posting can change the answer before the deactivation commits.
/// </summary>
public interface IWarehouseStockGuard
{
    /// <summary>Locks the warehouse for Inventory and says whether anything still ties it to stock.</summary>
    Task<StockOccupancy> LockAndCheckWarehouseAsync(Guid warehouseId, CancellationToken ct);

    /// <summary>Locks the location's warehouse for Inventory and says whether anything still ties the location to stock.</summary>
    Task<StockOccupancy> LockAndCheckLocationAsync(Guid warehouseId, Guid locationId, CancellationToken ct);
}
