using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;

/// <summary>Write side of supplier invoices with their lines and history.</summary>
public interface ISupplierInvoiceRepository
{
    Task<Guid?> FindWarehouseAsync(Guid invoiceId, CancellationToken ct);

    /// <summary>True when another invoice of the supplier that is not cancelled has this normalized number.</summary>
    Task<bool> InvoiceNumberTakenAsync(Guid supplierId, string invoiceNumberKey, Guid? exceptInvoiceId, CancellationToken ct);

    /// <summary>
    /// The invoice with its lines, tracked and locked against other writers until the transaction ends, or null. With a
    /// row version, the save fails with a conflict unless the invoice still has it.
    /// </summary>
    Task<SupplierInvoice?> GetForUpdateAsync(Guid invoiceId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(SupplierInvoice invoice);

    /// <summary>The use case's save point; a duplicate supplier invoice number comes back as its conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
