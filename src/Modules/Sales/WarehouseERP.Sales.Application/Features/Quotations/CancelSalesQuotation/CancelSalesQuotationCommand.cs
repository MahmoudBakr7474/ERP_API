namespace WarehouseERP.Sales.Application.Features.Quotations.CancelSalesQuotation;

/// <param name="Reason">Optional; kept in its history.</param>
public sealed record CancelSalesQuotationCommand(Guid QuotationId, string? Reason, string RowVersion);
