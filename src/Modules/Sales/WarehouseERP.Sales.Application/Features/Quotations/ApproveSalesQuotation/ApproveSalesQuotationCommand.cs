namespace WarehouseERP.Sales.Application.Features.Quotations.ApproveSalesQuotation;

public sealed record ApproveSalesQuotationCommand(Guid QuotationId, string RowVersion);
