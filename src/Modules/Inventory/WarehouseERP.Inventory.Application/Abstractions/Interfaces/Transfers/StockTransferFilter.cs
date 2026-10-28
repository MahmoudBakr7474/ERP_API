using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;

/// <param name="ScopeWarehouseIds">The user's warehouses (a transfer shows when its source or destination is one); null for company-wide users.</param>
/// <param name="WarehouseId">Source or destination.</param>
/// <param name="Search">Matches the number, the reference or the external reference.</param>
public sealed record StockTransferFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, StockTransferStatus? Status, string? Search);
