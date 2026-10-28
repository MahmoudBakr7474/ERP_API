using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

namespace WarehouseERP.Purchasing.Presentation.Contracts.SupplierInvoices;

public sealed record UpdateSupplierInvoiceRequest(
    string SupplierInvoiceNumber, DateOnly InvoiceDate, DateOnly? DueDate, string? Notes, IReadOnlyList<InvoiceLineRequest> Lines,
    string RowVersion);
