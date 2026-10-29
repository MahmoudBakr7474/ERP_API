using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class SalesQuotationRepository(DbContext db) : ISalesQuotationRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid quotationId, CancellationToken ct) =>
        db.Set<SalesQuotation>().AsNoTracking().Where(x => x.Id == quotationId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction, a second writer of the same quotation waits here, then sees the outcome.
    public async Task<SalesQuotation?> GetForUpdateAsync(Guid quotationId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var quotation = await db.Set<SalesQuotation>()
            .FromSqlInterpolated($"SELECT * FROM [sales].[SalesQuotations] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {quotationId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (quotation is not null && expectedRowVersion is not null)
            db.Entry(quotation).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return quotation;
    }

    public void Add(SalesQuotation quotation) => db.Add(quotation);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
