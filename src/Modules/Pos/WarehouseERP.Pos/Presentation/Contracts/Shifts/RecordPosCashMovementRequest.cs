using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Presentation.Contracts.Shifts;

/// <summary>Cash put into or taken out of the drawer, with a reason; the shift's row version read just before.</summary>
public sealed record RecordPosCashMovementRequest(PosCashMovementType MovementType, decimal Amount, string? Reason, string RowVersion);
