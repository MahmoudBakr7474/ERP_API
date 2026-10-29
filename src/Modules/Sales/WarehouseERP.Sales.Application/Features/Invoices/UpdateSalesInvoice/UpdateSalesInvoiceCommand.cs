using WarehouseERP.Sales.Application.Contracts.Invoices;

namespace WarehouseERP.Sales.Application.Features.Invoices.UpdateSalesInvoice;

/// <summary>Replaces a draft invoice's date, references and lines.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSalesInvoiceCommand(
    Guid InvoiceId, DateOnly? InvoiceDate, string? CustomerReference, string? Notes, IReadOnlyList<SalesInvoiceLineRequest> Lines,
    string RowVersion);
