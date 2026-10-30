using WarehouseERP.Accounting.Application.Contracts.Cutover;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Cutover.GetCutoverReadiness;

public sealed class GetCutoverReadinessHandler(CutoverChecks checks, ICurrentUser currentUser)
    : IQueryHandler<GetCutoverReadinessQuery, CutoverReadinessResponse>
{
    public async Task<Result<CutoverReadinessResponse>> HandleAsync(GetCutoverReadinessQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await checks.CheckAsync(ct);
    }
}
