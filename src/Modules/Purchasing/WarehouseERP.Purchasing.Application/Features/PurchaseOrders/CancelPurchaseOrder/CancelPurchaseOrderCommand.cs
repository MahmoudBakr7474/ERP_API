namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CancelPurchaseOrder;

/// <param name="Reason">Optional; kept in its history.</param>
public sealed record CancelPurchaseOrderCommand(Guid PurchaseOrderId, string? Reason, string RowVersion);
