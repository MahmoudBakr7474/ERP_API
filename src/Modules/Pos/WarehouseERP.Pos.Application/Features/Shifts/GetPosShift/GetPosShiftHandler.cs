using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Shifts.GetPosShift;

public sealed class GetPosShiftHandler(IPosShiftReadStore shifts, ICurrentUser currentUser) : IQueryHandler<GetPosShiftQuery, PosShiftResponse>
{
    public async Task<Result<PosShiftResponse>> HandleAsync(GetPosShiftQuery query, CancellationToken ct)
    {
        var shift = await shifts.GetAsync(query.ShiftId, ct);
        var access = PosAccess.Check(currentUser, shift?.WarehouseId, ShiftErrors.NotFound);
        return access.IsFailure ? access.Error : shift!;
    }
}
