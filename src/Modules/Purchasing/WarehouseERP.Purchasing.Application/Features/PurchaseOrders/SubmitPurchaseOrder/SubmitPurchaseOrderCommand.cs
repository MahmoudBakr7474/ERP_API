namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.SubmitPurchaseOrder;

/// <summary>Submits a draft for approval, or approves and numbers it at once when its policy needs no approval.</summary>
public sealed record SubmitPurchaseOrderCommand(Guid PurchaseOrderId, string RowVersion);
