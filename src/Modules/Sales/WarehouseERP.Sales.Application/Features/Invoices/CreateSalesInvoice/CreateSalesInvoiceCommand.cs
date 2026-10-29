using WarehouseERP.Sales.Application.Contracts.Invoices;

namespace WarehouseERP.Sales.Application.Features.Invoices.CreateSalesInvoice;

/// <summary>A draft invoice for delivered goods of one confirmed order, billed at the order's agreed prices.</summary>
/// <param name="InvoiceDate">Today (company time zone) when left out; never after today.</param>
/// <param name="CustomerReference">The order's customer reference when left out.</param>
public sealed record CreateSalesInvoiceCommand(
    Guid OrderId, DateOnly? InvoiceDate, string? CustomerReference, string? Notes, IReadOnlyList<SalesInvoiceLineRequest> Lines);
