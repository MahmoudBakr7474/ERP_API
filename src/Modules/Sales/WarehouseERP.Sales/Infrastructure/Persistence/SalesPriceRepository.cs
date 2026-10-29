using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class SalesPriceRepository(DbContext db) : ISalesPriceRepository
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> FindActivePricesAsync(IReadOnlyCollection<Guid> productUnitIds, CancellationToken ct) =>
        await db.Set<SalesPrice>().AsNoTracking()
            .Where(x => x.IsActive && productUnitIds.Contains(x.ProductUnitId))
            .ToDictionaryAsync(x => x.ProductUnitId, x => x.UnitPrice, ct);

    public Task<SalesPrice?> GetForUpdateAsync(Guid productUnitId, CancellationToken ct) =>
        db.Set<SalesPrice>().SingleOrDefaultAsync(x => x.ProductUnitId == productUnitId, ct);

    public void Add(SalesPrice price) => db.Add(price);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
