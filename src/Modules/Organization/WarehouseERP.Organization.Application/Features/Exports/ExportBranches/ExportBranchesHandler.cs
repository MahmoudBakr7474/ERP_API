using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportBranches;

// The same read as the branch list (scope, filters, order, counts), over the whole result.
public sealed class ExportBranchesHandler(IBranchReadStore branches, ICurrentUser user)
    : IQueryHandler<ExportBranchesQuery, IReadOnlyList<BranchSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<BranchSummaryResponse>>> HandleAsync(ExportBranchesQuery query, CancellationToken ct)
    {
        var filter = new BranchListFilter(
            Text.Optional(query.Search), query.IsActive, WarehouseScope.Of(user), query.HasWarehouses,
            query.Sort ?? OrganizationSort.Code, query.Descending);
        var rows = await branches.ListAsync(filter, 1, query.MaxRows + 1, ct);
        return ExportLimit.Within(rows.Items, query.MaxRows);
    }
}
