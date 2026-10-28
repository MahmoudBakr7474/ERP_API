using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

internal sealed class SupplierReturnRepository(DbContext db) : ISupplierReturnRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid returnId, CancellationToken ct) =>
        db.Set<SupplierReturn>().AsNoTracking().Where(x => x.Id == returnId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    public async Task<SupplierReturn?> GetForUpdateAsync(Guid returnId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var supplierReturn = await db.Set<SupplierReturn>()
            .FromSqlInterpolated($"SELECT * FROM [purchasing].[SupplierReturns] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {returnId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (supplierReturn is not null && expectedRowVersion is not null)
            db.Entry(supplierReturn).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return supplierReturn;
    }

    public void Add(SupplierReturn supplierReturn) => db.Add(supplierReturn);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PurchasingSaves.SaveAsync(db, ct);
}
