namespace WarehouseERP.Pos.Contracts;

/// <summary>A card charged at a till for a checkout POS could not record, still waiting for its reversal or settlement.</summary>
public sealed record PosOpenCardIncident(
    Guid Id, Guid BranchId, Guid TerminalId, Guid? ShiftId, decimal CardAmount, string References, DateTime OccurredAtUtc);
