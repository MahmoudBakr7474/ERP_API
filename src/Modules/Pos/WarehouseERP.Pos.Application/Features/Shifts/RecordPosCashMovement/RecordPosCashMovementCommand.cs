using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Features.Shifts.RecordPosCashMovement;

/// <summary>Cash put into or taken out of an open shift's drawer, with a reason.</summary>
/// <param name="RowVersion">The shift's version the user saw; every sale changes it, so read the shift just before.</param>
public sealed record RecordPosCashMovementCommand(Guid ShiftId, PosCashMovementType MovementType, decimal Amount, string? Reason, string RowVersion);
