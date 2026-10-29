namespace WarehouseERP.Sales.Application.Features.Quotations.DeclineSalesQuotation;

/// <param name="Reason">Why the customer declined; kept on the quotation.</param>
public sealed record DeclineSalesQuotationCommand(Guid QuotationId, string Reason, string RowVersion);
