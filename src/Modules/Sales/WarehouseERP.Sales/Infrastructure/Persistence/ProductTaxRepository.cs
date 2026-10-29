using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class ProductTaxRepository(DbContext db) : IProductTaxRepository
{
    public async Task<IReadOnlyDictionary<Guid, ProductTax>> FindActiveAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct) =>
        await db.Set<ProductTax>().AsNoTracking()
            .Where(x => x.IsActive && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, ct);

    public Task<ProductTax?> GetForUpdateAsync(Guid productId, CancellationToken ct) =>
        db.Set<ProductTax>().SingleOrDefaultAsync(x => x.ProductId == productId, ct);

    public void Add(ProductTax tax) => db.Add(tax);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
