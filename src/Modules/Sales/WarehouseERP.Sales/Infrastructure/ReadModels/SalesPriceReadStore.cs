using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Application.Contracts.Prices;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

internal sealed class SalesPriceReadStore(DbContext db, ReferenceNames names) : ISalesPriceReadStore
{
    public async Task<PagedResult<SalesPriceResponse>> ListAsync(SalesPriceFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SalesPrice>().AsNoTracking().Where(x => x.IsActive);
        if (filter.ProductId is { } productId)
            query = query.Where(x => x.ProductId == productId);
        if (filter.ProductUnitId is { } productUnitId)
            query = query.Where(x => x.ProductUnitId == productUnitId);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).ThenBy(x => x.ProductUnitId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(productIds: rows.Select(r => r.ProductId), productUnitIds: rows.Select(r => r.ProductUnitId), ct: ct);
        return new PagedResult<SalesPriceResponse>(
            rows.Select(r => new SalesPriceResponse(
                r.Id, r.ProductId, lookup.Sku(r.ProductId), lookup.ProductName(r.ProductId), r.ProductUnitId, lookup.UnitCode(r.ProductUnitId),
                lookup.ConversionFactor(r.ProductUnitId), r.UnitPrice, r.CreatedAtUtc, r.UpdatedAtUtc)).ToList(),
            page, pageSize, total);
    }
}
