using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

internal sealed class InventoryLocks(DbContext db) : IInventoryLocks
{
    public Task<bool> TryLockRequestAsync(Guid idempotencyKey, CancellationToken ct) =>
        AppLocks.TryTakeAsync(db, AppLocks.Request(idempotencyKey), exclusive: true, ct);

    // Always in ascending id order, so two postings that touch the same warehouses wait instead of deadlocking.
    public async Task<bool> TryLockWarehousesAsync(IReadOnlyCollection<Guid> warehouseIds, bool exclusive, CancellationToken ct)
    {
        foreach (var warehouseId in warehouseIds.Distinct().Order())
            if (!await AppLocks.TryTakeAsync(db, AppLocks.Warehouse(warehouseId), exclusive, ct))
                return false;
        return true;
    }
}
