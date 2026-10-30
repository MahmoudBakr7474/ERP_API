using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Branches.ListBranches;

/// <param name="Search">Matches the code or either name.</param>
/// <param name="HasWarehouses">True: only branches holding a warehouse; false: only branches without one.</param>
public sealed record ListBranchesQuery(
    int Page, int? PageSize, string? Search, bool? IsActive, bool? HasWarehouses = null, OrganizationSort? Sort = null,
    bool Descending = false) : IPagedQuery;
