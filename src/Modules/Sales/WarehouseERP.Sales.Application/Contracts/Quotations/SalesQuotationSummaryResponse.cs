using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Quotations;

/// <param name="DraftReference">DQT-000123, its reference before it is numbered.</param>
/// <param name="Number">QT-…, once issued.</param>
public sealed record SalesQuotationSummaryResponse(
    Guid Id, string DraftReference, string? Number, SalesQuotationStatus Status, bool IsExpired, Guid CustomerId, string CustomerCode,
    string CustomerName, Guid WarehouseId, string WarehouseCode, string CurrencyCode, decimal TotalAmount, DateOnly QuotationDate,
    DateOnly ValidUntil, DateTime CreatedAtUtc, string? CreatedBy);
