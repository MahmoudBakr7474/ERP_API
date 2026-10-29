using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Invoices;

/// <param name="CustomerTaxNumber">The customer's tax registration number now (for printing; e-invoicing is not built yet).</param>
/// <param name="DueDate">Invoice date plus the order's payment terms.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record SalesInvoiceDetailsResponse(
    Guid Id, string DraftReference, string? Number, SalesInvoiceStatus Status, Guid OrderId, string? OrderNumber, Guid CustomerId,
    string CustomerCode, string CustomerName, string? CustomerTaxNumber, Guid WarehouseId, string WarehouseCode, Guid BranchId,
    string CurrencyCode, DateOnly InvoiceDate, int PaymentTermDays, DateOnly DueDate, string? CustomerReference, string? Notes,
    decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount, DateTime CreatedAtUtc,
    string? CreatedBy, DateTime? PostedAtUtc, string? PostedBy, string RowVersion, IReadOnlyList<SalesInvoiceLineResponse> Lines,
    IReadOnlyList<SalesDocumentEventResponse> Events);
