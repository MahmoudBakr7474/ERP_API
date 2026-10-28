using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Orderable">Only approved requests with something left to order.</param>
public sealed record PurchaseRequestFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, PurchaseRequestStatus? Status, bool Orderable, string? Search);
