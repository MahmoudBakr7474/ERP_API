using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.ListLocationDirectory;

/// <summary>Locations across every warehouse in the caller's scope, with their warehouse and branch.</summary>
/// <param name="Search">Matches the location's code or either name.</param>
/// <param name="WarehouseType">Physical for storage locations, Transit for the locations open transfers use.</param>
public sealed record ListLocationDirectoryQuery(
    int Page, int? PageSize, Guid? BranchId, Guid? WarehouseId, WarehouseType? WarehouseType, string? Search, bool? IsActive,
    OrganizationSort? Sort = null, bool Descending = false) : IPagedQuery;
