using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">Matches the number, the draft reference or the supplier's reference.</param>
public sealed record PurchaseOrderFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, Guid? SupplierId, PurchaseOrderStatus? Status, string? Search);
