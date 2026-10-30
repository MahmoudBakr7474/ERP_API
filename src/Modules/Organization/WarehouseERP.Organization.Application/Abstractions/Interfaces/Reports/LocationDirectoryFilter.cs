using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;

/// <param name="OnlyWarehouseIds">The caller's warehouse scope; null means every warehouse.</param>
/// <param name="Search">Matches the location's code or either name.</param>
/// <param name="Sort">Within a warehouse: by code (default first) or a name; the list is always grouped by branch and
/// warehouse code.</param>
public sealed record LocationDirectoryFilter(
    IReadOnlySet<Guid>? OnlyWarehouseIds, Guid? BranchId, Guid? WarehouseId, WarehouseType? WarehouseType, string? Search,
    bool? IsActive, OrganizationSort Sort = OrganizationSort.Code, bool Descending = false);
