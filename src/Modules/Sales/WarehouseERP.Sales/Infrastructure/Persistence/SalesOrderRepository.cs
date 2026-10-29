using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class SalesOrderRepository(DbContext db) : ISalesOrderRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid orderId, CancellationToken ct) =>
        db.Set<SalesOrder>().AsNoTracking().Where(x => x.Id == orderId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction, a second confirmation, delivery, invoice, return or reduction of the same order waits
    // here, then sees the outcome. An order already loaded in this unit of work is returned as it is.
    public async Task<SalesOrder?> GetForUpdateAsync(Guid orderId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var order = await db.Set<SalesOrder>()
            .FromSqlInterpolated($"SELECT * FROM [sales].[SalesOrders] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {orderId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (order is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(order).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return order;
    }

    public void Add(SalesOrder order) => db.Add(order);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
