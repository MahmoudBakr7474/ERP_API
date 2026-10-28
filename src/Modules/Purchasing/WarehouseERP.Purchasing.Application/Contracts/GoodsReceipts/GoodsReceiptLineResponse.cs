namespace WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;

/// <param name="Quantity">In the order line's unit.</param>
/// <param name="UnitCost">The order's net unit price it entered stock at (provisional cost).</param>
/// <param name="ReturnableQuantity">What supplier returns may still send back.</param>
public sealed record GoodsReceiptLineResponse(
    Guid Id, int LineNumber, Guid PurchaseOrderLineId, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId,
    string UnitCode, decimal Quantity, decimal UnitCost, Guid LocationId, string LocationCode, decimal ReturnedQuantity,
    decimal ReturnableQuantity);
