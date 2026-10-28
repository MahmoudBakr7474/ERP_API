using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Valuation;
using WarehouseERP.Inventory.Application.Contracts.Valuation;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

internal sealed class ValuationReadStore(DbContext db, ReferenceNames names) : IValuationReadStore
{
    public async Task<PagedResult<InventoryValuationResponse>> ListAsync(
        Guid? productId, bool includeZero, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<InventoryCostState>().AsNoTracking();
        if (productId is { } id)
            query = query.Where(x => x.ProductId == id);
        if (!includeZero)
            query = query.Where(x => x.TotalQuantity != 0 || x.TotalValue != 0);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.ProductId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var lookup = await names.LoadAsync(productIds: rows.Select(r => r.ProductId), ct: ct);

        return new PagedResult<InventoryValuationResponse>(
            rows.Select(r => new InventoryValuationResponse(
                r.ProductId, lookup.Sku(r.ProductId), lookup.ProductName(r.ProductId), lookup.BaseUnitCode(r.ProductId),
                r.TotalQuantity, r.TotalValue, r.AverageUnitCost, r.UpdatedAtUtc))
                .ToList(),
            page, pageSize, total);
    }

    public async Task<PagedResult<InventoryCostEntryResponse>> ListEntriesAsync(Guid productId, int page, int pageSize, CancellationToken ct)
    {
        var total = await db.Set<InventoryCostEntry>().CountAsync(x => x.ProductId == productId, ct);
        var items = await (
                from entry in db.Set<InventoryCostEntry>().AsNoTracking()
                where entry.ProductId == productId
                join document in db.Set<StockDocument>() on entry.DocumentId equals document.Id
                orderby entry.Id descending
                select new InventoryCostEntryResponse(
                    entry.Id, entry.PostedAtUtc, entry.DocumentId, document.Number ?? "", document.DocumentType,
                    entry.QuantityDelta, entry.ValueDelta, entry.UnitCost, entry.QuantityAfter, entry.ValueAfter,
                    entry.AverageCostAfter))
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return new PagedResult<InventoryCostEntryResponse>(items, page, pageSize, total);
    }
}
