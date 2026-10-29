using WarehouseERP.Sales.Application.Contracts.Documents;

namespace WarehouseERP.Sales.Presentation.Contracts.Quotations;

/// <summary>A draft quotation's new customer, terms and lines; its warehouse stays.</summary>
public sealed record UpdateSalesQuotationRequest(
    Guid CustomerId, DateOnly QuotationDate, DateOnly ValidUntil, string? CustomerReference, int PaymentTermDays, string? Notes,
    IReadOnlyList<SalesLineRequest> Lines, string RowVersion);
