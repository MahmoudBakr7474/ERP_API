using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Orders;

/// <param name="DraftReference">DSO-000123, its reference before it is numbered.</param>
/// <param name="Number">SO-…, once confirmed.</param>
public sealed record SalesOrderSummaryResponse(
    Guid Id, string DraftReference, string? Number, SalesOrderStatus Status, Guid CustomerId, string CustomerCode, string CustomerName,
    Guid WarehouseId, string WarehouseCode, string CurrencyCode, decimal TotalAmount, decimal InvoicedAmount, DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate, DateTime CreatedAtUtc, string? CreatedBy, DateTime? ConfirmedAtUtc);
