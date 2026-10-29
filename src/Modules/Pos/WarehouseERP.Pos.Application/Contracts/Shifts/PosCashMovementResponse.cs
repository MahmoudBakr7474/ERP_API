using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Shifts;

public sealed record PosCashMovementResponse(
    Guid Id, PosCashMovementType MovementType, decimal Amount, string Reason, Guid? UserId, string? UserName, DateTime AtUtc);
