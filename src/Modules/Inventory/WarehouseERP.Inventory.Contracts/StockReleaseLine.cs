namespace WarehouseERP.Inventory.Contracts;

/// <summary>One order line's reservation to give back.</summary>
/// <param name="SourceLineId">The sales order line.</param>
/// <param name="BaseQuantity">Base units to release; null releases everything the reservation still holds.</param>
public sealed record StockReleaseLine(Guid SourceLineId, decimal? BaseQuantity);
