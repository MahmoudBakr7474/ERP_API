using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class SalesDeliveryRepository(DbContext db) : ISalesDeliveryRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid deliveryId, CancellationToken ct) =>
        db.Set<SalesDelivery>().AsNoTracking().Where(x => x.Id == deliveryId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction, posting a delivery and approving returns from it wait for each other here.
    public async Task<SalesDelivery?> GetForUpdateAsync(Guid deliveryId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var delivery = await db.Set<SalesDelivery>()
            .FromSqlInterpolated($"SELECT * FROM [sales].[SalesDeliveries] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {deliveryId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (delivery is not null && expectedRowVersion is not null)
            db.Entry(delivery).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return delivery;
    }

    public void Add(SalesDelivery delivery) => db.Add(delivery);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
