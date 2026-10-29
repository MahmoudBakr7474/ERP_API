using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Shifts.GetCurrentPosShift;

// What a till shows: the caller's own open shift there (the X report), or 404 when they have none.
public sealed class GetCurrentPosShiftHandler(TillAccess tills, IPosShiftReadStore shifts)
    : IQueryHandler<GetCurrentPosShiftQuery, PosShiftResponse>
{
    public async Task<Result<PosShiftResponse>> HandleAsync(GetCurrentPosShiftQuery query, CancellationToken ct)
    {
        var terminal = await tills.AccessibleAsync(query.TerminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        var shiftId = await shifts.FindOpenAsync(terminal.Value.Id, tills.UserId, ct);
        return shiftId is { } id && await shifts.GetAsync(id, ct) is { } shift ? shift : ShiftErrors.NotFound;
    }
}
