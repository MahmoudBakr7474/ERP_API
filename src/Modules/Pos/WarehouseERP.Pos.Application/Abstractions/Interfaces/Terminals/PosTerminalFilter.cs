namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
public sealed record PosTerminalFilter(IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, bool? IsActive, string? Search);
