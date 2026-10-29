using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Shifts;

public sealed record PosShiftEventResponse(
    long Id, PosShiftAction Action, decimal? Amount, string? Note, Guid? UserId, string? UserName, DateTime AtUtc);
