namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ReceiveGoods;

/// <param name="SupplierDeliveryNote">The supplier's delivery note or waybill number.</param>
/// <param name="IdempotencyKey">A new GUID per receiving click, sent again unchanged when the client retries.</param>
public sealed record ReceiveGoodsCommand(
    Guid PurchaseOrderId, string? SupplierDeliveryNote, string? Notes, IReadOnlyList<ReceiptLineRequest> Lines, Guid IdempotencyKey);
