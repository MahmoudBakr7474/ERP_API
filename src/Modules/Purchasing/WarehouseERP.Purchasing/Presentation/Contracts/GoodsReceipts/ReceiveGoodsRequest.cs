using WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ReceiveGoods;

namespace WarehouseERP.Purchasing.Presentation.Contracts.GoodsReceipts;

/// <summary>Goods received against the order in the route; the Idempotency-Key header identifies the click.</summary>
public sealed record ReceiveGoodsRequest(string? SupplierDeliveryNote, string? Notes, IReadOnlyList<ReceiptLineRequest> Lines);
