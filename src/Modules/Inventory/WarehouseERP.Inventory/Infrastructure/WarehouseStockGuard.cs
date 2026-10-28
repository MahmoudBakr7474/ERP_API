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
        if (await db.Set<StockDocument>().AnyAsync(x => x.WarehouseId == warehouseId && x.Status == StockDocumentStatus.Submitted, ct)
            || await db.Set<StockTransfer>().AnyAsync(t =>
                (t.SourceWarehouseId == warehouseId || t.DestinationWarehouseId == warehouseId || t.TransitWarehouseId == warehouseId)
                && OpenTransferStatuses.Contains(t.Status), ct)
            || await db.Set<StockCountScope>().AnyAsync(s => s.WarehouseId == warehouseId && s.IsActive, ct))
            return StockOccupancy.HasOpenDocuments;
        return StockOccupancy.Free;
    }

    // A transfer waiting for approval, on its way or being closed still needs its warehouses.
    private static readonly StockTransferStatus[] OpenTransferStatuses =
    [
        StockTransferStatus.Submitted, StockTransferStatus.InTransit, StockTransferStatus.PartiallyReceived, StockTransferStatus.Closing,
    ];

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
        open = open
            || await db.Set<StockTransfer>().AnyAsync(t =>
                t.SourceWarehouseId == warehouseId && t.Status == StockTransferStatus.Submitted
                && t.Lines.Any(l => l.SourceLocationId == locationId), ct)
            || await db.Set<StockCountScope>().AnyAsync(s =>
                s.WarehouseId == warehouseId && s.IsActive && (s.LocationId == null || s.LocationId == locationId), ct);
        return open ? StockOccupancy.HasOpenDocuments : StockOccupancy.Free;
    }
}
