namespace WarehouseERP.Sales.Application.Features.Deliveries.CancelSalesDelivery;

public sealed record CancelSalesDeliveryCommand(Guid DeliveryId, string RowVersion);
