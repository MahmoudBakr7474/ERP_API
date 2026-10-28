namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ClosePurchaseOrder;

/// <param name="Reason">Why the rest will not be received; kept on the order.</param>
public sealed record ClosePurchaseOrderCommand(Guid PurchaseOrderId, string Reason, string RowVersion);
