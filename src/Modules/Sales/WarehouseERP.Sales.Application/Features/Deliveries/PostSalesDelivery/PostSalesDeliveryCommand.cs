namespace WarehouseERP.Sales.Application.Features.Deliveries.PostSalesDelivery;

/// <param name="RowVersion">The version of the draft the user saw.</param>
/// <param name="IdempotencyKey">From the Idempotency-Key header: a new GUID per click, sent again unchanged on a retry.</param>
public sealed record PostSalesDeliveryCommand(Guid DeliveryId, string RowVersion, Guid IdempotencyKey);
