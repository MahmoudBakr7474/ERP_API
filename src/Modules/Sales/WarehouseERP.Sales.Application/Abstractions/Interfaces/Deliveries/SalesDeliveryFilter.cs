using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">Matches the number or the draft reference.</param>
public sealed record SalesDeliveryFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, Guid? OrderId, Guid? CustomerId, SalesDeliveryStatus? Status,
    string? Search);
