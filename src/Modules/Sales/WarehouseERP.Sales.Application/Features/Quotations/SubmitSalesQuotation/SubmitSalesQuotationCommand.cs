namespace WarehouseERP.Sales.Application.Features.Quotations.SubmitSalesQuotation;

public sealed record SubmitSalesQuotationCommand(Guid QuotationId, string RowVersion);
