using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for company-wide users.</param>
public sealed record StockCountFilter(IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, StockCountStatus? Status);
