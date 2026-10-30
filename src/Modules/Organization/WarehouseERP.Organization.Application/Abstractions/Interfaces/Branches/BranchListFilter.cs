using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;

/// <param name="Search">Matches the code or either name.</param>
/// <param name="ThroughWarehouseIds">Only branches holding one of these warehouses; null means no restriction. Warehouse
/// counts then count only these warehouses too.</param>
/// <param name="HasWarehouses">True: only branches with a warehouse (in scope); false: only those without.</param>
public sealed record BranchListFilter(
    string? Search, bool? IsActive, IReadOnlySet<Guid>? ThroughWarehouseIds, bool? HasWarehouses = null,
    OrganizationSort Sort = OrganizationSort.Code, bool Descending = false);
