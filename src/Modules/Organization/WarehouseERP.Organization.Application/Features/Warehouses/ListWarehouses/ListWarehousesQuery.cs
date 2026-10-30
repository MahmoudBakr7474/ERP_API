using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;

/// <param name="Search">Matches the code or either name.</param>
/// <param name="AllowsSelling">True: only warehouses that may sell; false: only those that may not.</param>
public sealed record ListWarehousesQuery(
    int Page, int? PageSize, Guid? BranchId, string? Search, bool? IsActive, WarehouseType? Type = null,
    bool? AllowsSelling = null, OrganizationSort? Sort = null, bool Descending = false) : IPagedQuery;
