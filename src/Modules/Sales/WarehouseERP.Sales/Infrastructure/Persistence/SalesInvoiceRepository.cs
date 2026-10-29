using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class SalesInvoiceRepository(DbContext db) : ISalesInvoiceRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid invoiceId, CancellationToken ct) =>
        db.Set<SalesInvoice>().AsNoTracking().Where(x => x.Id == invoiceId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction, a second writer of the same invoice waits here, then sees the outcome.
    public async Task<SalesInvoice?> GetForUpdateAsync(Guid invoiceId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var invoice = await db.Set<SalesInvoice>()
            .FromSqlInterpolated($"SELECT * FROM [sales].[SalesInvoices] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {invoiceId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (invoice is not null && expectedRowVersion is not null)
            db.Entry(invoice).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return invoice;
    }

    public void Add(SalesInvoice invoice) => db.Add(invoice);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
