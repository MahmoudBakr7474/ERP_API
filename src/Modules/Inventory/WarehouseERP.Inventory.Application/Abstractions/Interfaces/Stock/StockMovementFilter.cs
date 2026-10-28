namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;

/// <param name="FromUtc">Inclusive.</param>
/// <param name="ToUtc">Exclusive.</param>
public sealed record StockMovementFilter(Guid ProductId, Guid WarehouseId, Guid? LocationId, DateTime? FromUtc, DateTime? ToUtc);
