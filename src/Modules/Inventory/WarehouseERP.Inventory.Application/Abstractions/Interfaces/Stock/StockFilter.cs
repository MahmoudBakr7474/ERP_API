namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for company-wide users.</param>
/// <param name="IncludeZero">Also list rows whose quantity is zero.</param>
public sealed record StockFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, Guid? LocationId, Guid? ProductId, bool IncludeZero);
