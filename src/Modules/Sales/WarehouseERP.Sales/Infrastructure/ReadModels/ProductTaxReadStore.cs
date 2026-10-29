using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;
using WarehouseERP.Sales.Application.Contracts.ProductTaxes;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

internal sealed class ProductTaxReadStore(DbContext db, ReferenceNames names) : IProductTaxReadStore
{
    public async Task<PagedResult<ProductTaxResponse>> ListAsync(ProductTaxFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<ProductTax>().AsNoTracking().Where(x => x.IsActive);
        if (filter.ProductId is { } productId)
            query = query.Where(x => x.ProductId == productId);
        if (filter.TaxTreatment is { } treatment)
            query = query.Where(x => x.TaxTreatment == treatment);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).ThenBy(x => x.ProductId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var lookup = await names.LoadAsync(productIds: rows.Select(r => r.ProductId), ct: ct);
        return new PagedResult<ProductTaxResponse>(
            rows.Select(r => new ProductTaxResponse(
                r.Id, r.ProductId, lookup.Sku(r.ProductId), lookup.ProductName(r.ProductId), r.TaxTreatment, r.TaxRate,
                r.CreatedAtUtc, r.UpdatedAtUtc)).ToList(),
            page, pageSize, total);
    }
}
