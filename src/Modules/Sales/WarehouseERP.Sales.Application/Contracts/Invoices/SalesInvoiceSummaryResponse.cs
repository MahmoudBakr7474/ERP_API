using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Invoices;

/// <param name="DraftReference">DINV-000123, its reference before it is posted.</param>
/// <param name="Number">INV-…, once posted.</param>
public sealed record SalesInvoiceSummaryResponse(
    Guid Id, string DraftReference, string? Number, SalesInvoiceStatus Status, Guid OrderId, string? OrderNumber, Guid CustomerId,
    string CustomerName, Guid WarehouseId, string WarehouseCode, string CurrencyCode, DateOnly InvoiceDate, DateOnly DueDate,
    decimal TotalAmount, DateTime CreatedAtUtc, DateTime? PostedAtUtc);
