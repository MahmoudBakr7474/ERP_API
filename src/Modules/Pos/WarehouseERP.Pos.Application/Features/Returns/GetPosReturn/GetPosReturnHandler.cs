using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Returns.GetPosReturn;

public sealed class GetPosReturnHandler(IPosReturnReadStore returns, ICurrentUser currentUser) : IQueryHandler<GetPosReturnQuery, PosReturnResponse>
{
    public async Task<Result<PosReturnResponse>> HandleAsync(GetPosReturnQuery query, CancellationToken ct)
    {
        var posReturn = await returns.GetAsync(query.ReturnId, ct);
        var access = PosAccess.Check(currentUser, posReturn?.WarehouseId, ReturnErrors.NotFound);
        return access.IsFailure ? access.Error : posReturn!;
    }
}
