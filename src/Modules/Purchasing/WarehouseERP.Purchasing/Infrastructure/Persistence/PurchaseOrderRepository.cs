using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

internal sealed class PurchaseOrderRepository(DbContext db) : IPurchaseOrderRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid orderId, CancellationToken ct) =>
        db.Set<PurchaseOrder>().AsNoTracking().Where(x => x.Id == orderId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction, a second receipt, invoice or return of the same order waits here, then sees the outcome.
    public async Task<PurchaseOrder?> GetForUpdateAsync(Guid orderId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var order = await db.Set<PurchaseOrder>()
            .FromSqlInterpolated($"SELECT * FROM [purchasing].[PurchaseOrders] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {orderId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (order is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(order).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return order;
    }

    public void Add(PurchaseOrder order) => db.Add(order);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PurchasingSaves.SaveAsync(db, ct);
}
