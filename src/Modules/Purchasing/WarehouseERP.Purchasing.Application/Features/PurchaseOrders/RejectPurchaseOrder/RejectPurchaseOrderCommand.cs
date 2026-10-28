namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.RejectPurchaseOrder;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectPurchaseOrderCommand(Guid PurchaseOrderId, string Reason, string RowVersion);
