namespace WarehouseERP.Inventory.Application.Features.Counts.CreateStockCount;

/// <param name="LocationIds">The locations to count; the whole warehouse when empty or left out.</param>
public sealed record CreateStockCountCommand(Guid WarehouseId, IReadOnlyList<Guid>? LocationIds, string? Notes);
