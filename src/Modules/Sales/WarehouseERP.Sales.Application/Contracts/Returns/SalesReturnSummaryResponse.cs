using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Returns;

/// <param name="DraftReference">DSR-000123, its reference before it is approved.</param>
/// <param name="Number">SR-…, once approved.</param>
public sealed record SalesReturnSummaryResponse(
    Guid Id, string DraftReference, string? Number, SalesReturnStatus Status, Guid DeliveryId, string? DeliveryNumber, Guid OrderId,
    string? OrderNumber, Guid CustomerId, string CustomerName, Guid WarehouseId, string WarehouseCode, decimal CreditTotalAmount,
    DateTime CreatedAtUtc, DateTime? ApprovedAtUtc);
