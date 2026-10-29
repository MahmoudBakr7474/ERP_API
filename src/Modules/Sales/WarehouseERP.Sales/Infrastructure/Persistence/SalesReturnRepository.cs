using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class SalesReturnRepository(DbContext db) : ISalesReturnRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid returnId, CancellationToken ct) =>
        db.Set<SalesReturn>().AsNoTracking().Where(x => x.Id == returnId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction, a second writer of the same return waits here, then sees the outcome.
    public async Task<SalesReturn?> GetForUpdateAsync(Guid returnId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var salesReturn = await db.Set<SalesReturn>()
            .FromSqlInterpolated($"SELECT * FROM [sales].[SalesReturns] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {returnId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (salesReturn is not null && expectedRowVersion is not null)
            db.Entry(salesReturn).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return salesReturn;
    }

    public void Add(SalesReturn salesReturn) => db.Add(salesReturn);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
