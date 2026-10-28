using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

// A posting reads the freezes after taking its warehouse locks: a count either started before (its scope is committed
// and seen here) or waits for the posting to end before it can take the warehouse exclusively.
internal sealed class StockFreezes(DbContext db) : IStockFreezes
{
    public async Task<FrozenScope?> FindFrozenAsync(IReadOnlyCollection<StockPlace> places, Guid? exceptCountId, CancellationToken ct)
    {
        if (places.Count == 0)
            return null;
        var warehouseIds = places.Select(p => p.WarehouseId).Distinct().ToArray();
        var locationIds = places.Select(p => p.LocationId).Distinct().ToArray();
        var frozen = await (
                from scope in db.Set<StockCountScope>().AsNoTracking()
                where scope.IsActive && warehouseIds.Contains(scope.WarehouseId)
                      && (scope.LocationId == null || locationIds.Contains(scope.LocationId.Value))
                      && (exceptCountId == null || scope.CountId != exceptCountId)
                join count in db.Set<StockCount>().AsNoTracking() on scope.CountId equals count.Id
                select new { count.Id, count.DraftNumber })
            .FirstOrDefaultAsync(ct);
        return frozen is null ? null : new FrozenScope(frozen.Id, StockDocumentNumbers.CountReference(frozen.DraftNumber));
    }
}
