namespace WarehouseERP.Sales.Application.Features.Quotations.RejectSalesQuotation;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectSalesQuotationCommand(Guid QuotationId, string Reason, string RowVersion);
