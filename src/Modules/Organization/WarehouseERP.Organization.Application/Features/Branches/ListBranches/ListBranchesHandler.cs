using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Branches.ListBranches;

// A company-wide user sees every branch; anyone else only the branches that hold one of their warehouses, with
// warehouse counts of those warehouses only.
public sealed class ListBranchesHandler(
    IBranchReadStore branches, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListBranchesQuery, PagedResult<BranchSummaryResponse>>
{
    public async Task<Result<PagedResult<BranchSummaryResponse>>> HandleAsync(ListBranchesQuery query, CancellationToken ct)
    {
        var filter = new BranchListFilter(
            Text.Optional(query.Search), query.IsActive, currentUser.HasAllWarehouses ? null : currentUser.WarehouseIds,
            query.HasWarehouses, query.Sort ?? OrganizationSort.Code, query.Descending);
        return await branches.ListAsync(filter, query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
