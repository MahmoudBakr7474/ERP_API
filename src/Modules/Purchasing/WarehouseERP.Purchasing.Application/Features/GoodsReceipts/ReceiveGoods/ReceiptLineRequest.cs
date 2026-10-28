namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ReceiveGoods;

/// <summary>Goods received for one order line.</summary>
/// <param name="PurchaseOrderLineId">A line of the order; each at most once per receipt.</param>
/// <param name="Quantity">In the order line's unit; at most what remains to be received.</param>
/// <param name="LocationId">A location of the order's warehouse; its DEFAULT location when left out.</param>
public sealed record ReceiptLineRequest(Guid PurchaseOrderLineId, decimal Quantity, Guid? LocationId = null);
