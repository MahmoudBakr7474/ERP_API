namespace WarehouseERP.Inventory.Contracts;

/// <summary>One posted line of a POS sale or return.</summary>
/// <param name="StockLineId">The stock document line; a return names a sale's line to come back from.</param>
/// <param name="BaseQuantity">Base units moved.</param>
/// <param name="CostAmount">Value at cost in the base currency: what left stock (sale) or came back (return), positive.</param>
public sealed record PosStockLine(Guid StockLineId, decimal BaseQuantity, decimal CostAmount);
