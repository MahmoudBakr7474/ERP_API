using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Inventory.Infrastructure.Persistence;

namespace WarehouseERP.Inventory.Infrastructure;

// Organization's deactivation asks here, inside its own transaction. The exclusive warehouse lock waits for postings in
// progress and keeps new ones out until the deactivation commits, so the answer still holds when it does; a posting
// that comes later finds the warehouse or location inactive.
internal sealed class WarehouseStockGuard(DbContext db) : IWarehouseStockGuard
{
    public async Task<StockOccupancy> LockAndCheckWarehouseAsync(Guid warehouseId, CancellationToken ct)
    {
        if (!await AppLocks.TryTakeAsync(db, AppLocks.Warehouse(warehouseId), exclusive: true, ct))
            return StockOccupancy.Busy;
        if (await db.Set<StockBalance>().AnyAsync(x => x.WarehouseId == warehouseId && x.Quantity != 0, ct)
            || await db.Set<ProductWarehouseStock>().AnyAsync(
                x => x.WarehouseId == warehouseId && (x.OnHandSellable != 0 || x.ReservedQuantity != 0), ct))
            return StockOccupancy.HoldsStock;
        if (await db.Set<StockDocument>().AnyAsync(x => x.WarehouseId == warehouseId && x.Status == StockDocumentStatus.Submitted, ct))
            return StockOccupancy.HasOpenDocuments;
        return StockOccupancy.Free;
    }

    public async Task<StockOccupancy> LockAndCheckLocationAsync(Guid warehouseId, Guid locationId, CancellationToken ct)
    {
        if (!await AppLocks.TryTakeAsync(db, AppLocks.Warehouse(warehouseId), exclusive: true, ct))
            return StockOccupancy.Busy;
        if (await db.Set<StockBalance>().AnyAsync(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.Quantity != 0, ct))
            return StockOccupancy.HoldsStock;
        var open = await (
            from document in db.Set<StockDocument>()
            where document.WarehouseId == warehouseId && document.Status == StockDocumentStatus.Submitted
            from line in document.Lines
            from allocation in line.Allocations
            where allocation.FromLocationId == locationId || allocation.ToLocationId == locationId
            select document.Id).AnyAsync(ct);
        return open ? StockOccupancy.HasOpenDocuments : StockOccupancy.Free;
    }
}
