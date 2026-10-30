namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// What a posted delivery means to the ledger. StockDocumentId: The SalesDelivery stock document Inventory took the
/// goods out with (their cost). RevenueAmount: The revenue its goods earned, net of VAT: per order line, the change of
/// the not-yet-invoiced quantity's value at the agreed price and discount (rounded for the whole quantity, so an order
/// line's unbilled value returns exactly to zero once it is all invoiced or returned).
/// </summary>
public sealed record SalesDeliveryAccounting(
    Guid Id, string Number, Guid CustomerId, Guid BranchId, DateTime PostedAtUtc, Guid StockDocumentId, decimal RevenueAmount);
