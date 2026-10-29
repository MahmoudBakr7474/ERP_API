namespace WarehouseERP.Sales.Application.Features.Quotations.ConvertSalesQuotation;

/// <summary>The customer accepted an issued quotation: it becomes a draft sales order, once.</summary>
public sealed record ConvertSalesQuotationCommand(Guid QuotationId, string RowVersion);
