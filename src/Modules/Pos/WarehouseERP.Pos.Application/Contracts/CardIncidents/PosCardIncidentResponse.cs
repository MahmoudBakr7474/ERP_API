using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.CardIncidents;

/// <summary>A card charge no sale was recorded for: what to reverse on the card terminal, and how it was settled.</summary>
public sealed record PosCardIncidentResponse(
    Guid Id, Guid IdempotencyKey, Guid TerminalId, string TerminalCode, Guid WarehouseId, string WarehouseCode, Guid? ShiftId,
    Guid? CashierUserId, string? CashierName, decimal CardAmount, string References, string ErrorCode, string ErrorMessage,
    PosCardIncidentStatus Status, int Attempts, DateTime OccurredAtUtc, DateTime LastAttemptAtUtc, Guid? SaleId,
    DateTime? ResolvedAtUtc, Guid? ResolvedByUserId, string? ResolvedByName, string? ResolutionNote, string RowVersion);
