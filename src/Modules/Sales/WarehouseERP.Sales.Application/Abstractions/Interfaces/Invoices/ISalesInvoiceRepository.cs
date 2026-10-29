using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;

/// <summary>Write side of sales invoices with their lines and history (one aggregate).</summary>
public interface ISalesInvoiceRepository
{
    /// <summary>The invoice's warehouse, which decides who may see it; null when no invoice has this id.</summary>
    Task<Guid?> FindWarehouseAsync(Guid invoiceId, CancellationToken ct);

    /// <summary>
    /// The invoice with its lines, tracked and locked against other writers until the transaction ends, or null. With a
    /// row version, the save fails with a conflict unless the invoice still has it.
    /// </summary>
    Task<SalesInvoice?> GetForUpdateAsync(Guid invoiceId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(SalesInvoice invoice);

    /// <summary>The use case's save point; an invoice whose lines or history changed moves its row version too.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
