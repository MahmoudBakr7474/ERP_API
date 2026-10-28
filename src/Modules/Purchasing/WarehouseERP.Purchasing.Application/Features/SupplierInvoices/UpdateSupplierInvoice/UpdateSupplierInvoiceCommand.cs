using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.UpdateSupplierInvoice;

/// <summary>Replaces a draft's supplier invoice number, dates and lines; its order stays.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSupplierInvoiceCommand(
    Guid SupplierInvoiceId, string SupplierInvoiceNumber, DateOnly InvoiceDate, DateOnly? DueDate, string? Notes,
    IReadOnlyList<InvoiceLineRequest> Lines, string RowVersion);
