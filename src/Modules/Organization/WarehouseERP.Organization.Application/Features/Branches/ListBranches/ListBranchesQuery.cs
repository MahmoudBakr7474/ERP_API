using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.Branches.ListBranches;

/// <param name="Search">Matches the code or either name.</param>
public sealed record ListBranchesQuery(int Page, int? PageSize, string? Search, bool? IsActive) : IPagedQuery;
