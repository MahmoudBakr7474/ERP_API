using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

// A till's stock hint: the warehouse's availability row and the location's available balance, read without locks.
internal sealed class StockAvailabilityLookup(DbContext db) : IStockAvailabilityLookup
{
    public async Task<IReadOnlyList<StockAvailability>> GetAsync(
        Guid warehouseId, Guid locationId, IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToArray();
        var warehouse = await db.Set<ProductWarehouseStock>().AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && ids.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, ct);
        var location = await db.Set<StockBalance>().AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.LocationId == locationId && x.LotId == null
                        && x.StockStatus == StockStatus.Available && ids.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, ct);

        return ids.Select(id => new StockAvailability(
                id,
                warehouse.TryGetValue(id, out var stock) ? stock.OnHandSellable : 0m,
                stock?.ReservedQuantity ?? 0m,
                location.GetValueOrDefault(id)))
            .ToList();
    }
}
