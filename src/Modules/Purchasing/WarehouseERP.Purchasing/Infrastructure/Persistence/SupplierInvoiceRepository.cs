using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

internal sealed class SupplierInvoiceRepository(DbContext db) : ISupplierInvoiceRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid invoiceId, CancellationToken ct) =>
        db.Set<SupplierInvoice>().AsNoTracking().Where(x => x.Id == invoiceId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    public Task<bool> InvoiceNumberTakenAsync(Guid supplierId, string invoiceNumberKey, Guid? exceptInvoiceId, CancellationToken ct) =>
        db.Set<SupplierInvoice>().AnyAsync(x =>
            x.SupplierId == supplierId && x.SupplierInvoiceNumberKey == invoiceNumberKey && x.Status != SupplierInvoiceStatus.Cancelled
            && (exceptInvoiceId == null || x.Id != exceptInvoiceId), ct);

    public async Task<SupplierInvoice?> GetForUpdateAsync(Guid invoiceId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var invoice = await db.Set<SupplierInvoice>()
            .FromSqlInterpolated($"SELECT * FROM [purchasing].[SupplierInvoices] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {invoiceId}")
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (invoice is not null && expectedRowVersion is not null)
            db.Entry(invoice).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return invoice;
    }

    public void Add(SupplierInvoice invoice) => db.Add(invoice);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PurchasingSaves.SaveAsync(db, ct);
}
