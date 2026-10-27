using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Branches.GetBranch;

// Scope is checked before existence, as for warehouses, so a user outside a branch can't learn whether it exists.
public sealed class GetBranchHandler(IBranchReadStore branches, ICurrentUser currentUser)
    : IQueryHandler<GetBranchQuery, BranchDetailsResponse>
{
    public async Task<Result<BranchDetailsResponse>> HandleAsync(GetBranchQuery query, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses && !await branches.HoldsAnyWarehouseAsync(query.BranchId, currentUser.WarehouseIds, ct))
            return BranchErrors.AccessDenied;

        return await branches.GetAsync(query.BranchId, ct) is { } branch
            ? branch
            : BranchErrors.NotFound;
    }
}
