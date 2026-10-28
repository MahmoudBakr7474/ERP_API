namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <summary>
/// SQL Server application locks owned by the current transaction (sp_getapplock), released when it ends. Each returns
/// false when the lock could not be taken in time.
/// </summary>
public interface IInventoryLocks
{
    /// <summary>Requests with one idempotency key run one after the other, so a retry finds the first one's record.</summary>
    Task<bool> TryLockRequestAsync(Guid idempotencyKey, CancellationToken ct);

    /// <summary>
    /// The warehouses' inventory locks in a fixed order: shared for ordinary posting, exclusive to keep everything else
    /// out (opening stock, a count freeze, deactivation).
    /// </summary>
    Task<bool> TryLockWarehousesAsync(IReadOnlyCollection<Guid> warehouseIds, bool exclusive, CancellationToken ct);
}
