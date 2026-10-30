using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;

/// <param name="OnlyWarehouseIds">Restricts the list to these warehouses; null means no restriction.</param>
/// <param name="Search">Matches the code or either name.</param>
public sealed record WarehouseListFilter(
    Guid? BranchId, IReadOnlySet<Guid>? OnlyWarehouseIds, string? Search, bool? IsActive, WarehouseType? Type = null,
    bool? AllowsSelling = null, OrganizationSort Sort = OrganizationSort.Code, bool Descending = false);
