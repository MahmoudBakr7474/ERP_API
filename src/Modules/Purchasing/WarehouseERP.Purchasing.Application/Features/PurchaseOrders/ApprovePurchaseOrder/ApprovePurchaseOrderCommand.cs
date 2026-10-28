namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ApprovePurchaseOrder;

public sealed record ApprovePurchaseOrderCommand(Guid PurchaseOrderId, string RowVersion);
