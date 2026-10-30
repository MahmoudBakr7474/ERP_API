namespace WarehouseERP.Purchasing.Contracts;

/// <summary>
/// What a goods receipt means to the ledger. StockDocumentId: The PurchaseReceipt stock document Inventory valued the
/// goods in. ReceivedValue: What it added to goods received not invoiced: per order line, the change of the uninvoiced
/// quantity's value at the order's net unit price (rounded for the whole quantity, so an order line's value returns
/// exactly to zero once it is all invoiced or returned).
/// </summary>
public sealed record GoodsReceiptAccounting(
    Guid Id, string Number, Guid SupplierId, Guid BranchId, DateTime ReceivedAtUtc, Guid StockDocumentId, decimal ReceivedValue);
