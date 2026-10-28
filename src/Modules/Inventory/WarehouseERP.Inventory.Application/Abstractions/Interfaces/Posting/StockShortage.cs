namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <param name="LocationId">Null when the warehouse's available (unreserved) stock was short, set when the location's was.</param>
public sealed record StockShortage(Guid ProductId, Guid WarehouseId, Guid? LocationId);
