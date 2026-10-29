using WarehouseERP.Sales.Application.Contracts.Invoices;

namespace WarehouseERP.Sales.Presentation.Contracts.Invoices;

/// <summary>A draft invoice's new date, references and lines.</summary>
/// <param name="InvoiceDate">Today when left out; never after today.</param>
public sealed record UpdateSalesInvoiceRequest(
    DateOnly? InvoiceDate, string? CustomerReference, string? Notes, IReadOnlyList<SalesInvoiceLineRequest> Lines, string RowVersion);
