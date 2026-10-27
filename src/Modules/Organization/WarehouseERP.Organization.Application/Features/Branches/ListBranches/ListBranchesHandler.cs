using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Branches.ListBranches;

// A company-wide user sees every branch; anyone else only the branches that hold one of their warehouses.
public sealed class ListBranchesHandler(
    IBranchReadStore branches, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListBranchesQuery, PagedResult<BranchSummaryResponse>>
{
    public async Task<Result<PagedResult<BranchSummaryResponse>>> HandleAsync(ListBranchesQuery query, CancellationToken ct)
    {
        var filter = new BranchListFilter(
            Text.Optional(query.Search), query.IsActive, currentUser.HasAllWarehouses ? null : currentUser.WarehouseIds);
        return await branches.ListAsync(filter, query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
