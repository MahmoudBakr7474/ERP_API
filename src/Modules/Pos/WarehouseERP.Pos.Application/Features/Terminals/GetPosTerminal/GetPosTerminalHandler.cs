using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Contracts.Terminals;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Terminals.GetPosTerminal;

public sealed class GetPosTerminalHandler(IPosTerminalReadStore terminals, ICurrentUser currentUser)
    : IQueryHandler<GetPosTerminalQuery, PosTerminalResponse>
{
    public async Task<Result<PosTerminalResponse>> HandleAsync(GetPosTerminalQuery query, CancellationToken ct)
    {
        var terminal = await terminals.GetAsync(query.TerminalId, ct);
        var access = PosAccess.Check(currentUser, terminal?.WarehouseId, TerminalErrors.NotFound);
        return access.IsFailure ? access.Error : terminal!;
    }
}
