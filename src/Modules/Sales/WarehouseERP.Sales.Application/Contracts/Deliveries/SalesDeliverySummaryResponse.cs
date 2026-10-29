using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Deliveries;

/// <param name="DraftReference">DDN-000123, its reference before it is posted.</param>
/// <param name="Number">DN-…, once posted.</param>
public sealed record SalesDeliverySummaryResponse(
    Guid Id, string DraftReference, string? Number, SalesDeliveryStatus Status, Guid OrderId, string? OrderNumber, Guid CustomerId,
    string CustomerName, Guid WarehouseId, string WarehouseCode, DateTime CreatedAtUtc, DateTime? PostedAtUtc);
