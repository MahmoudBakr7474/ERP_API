using WarehouseERP.Sales.Application.Contracts.Documents;

namespace WarehouseERP.Sales.Application.Features.Quotations.UpdateSalesQuotation;

/// <summary>Replaces a draft quotation's customer, terms and lines; its warehouse stays.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSalesQuotationCommand(
    Guid QuotationId, Guid CustomerId, DateOnly QuotationDate, DateOnly ValidUntil, string? CustomerReference, int PaymentTermDays,
    string? Notes, IReadOnlyList<SalesLineRequest> Lines, string RowVersion);
