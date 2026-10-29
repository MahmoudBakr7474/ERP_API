using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>One step in a shift's history: who did what and when, with the amount and note. Never changes.</summary>
public sealed class PosShiftEvent
{
    private PosShiftEvent() { } // EF Core

    public long Id { get; private set; }
    public Guid ShiftId { get; private set; }
    public PosShiftAction Action { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }

    /// <summary>The float, the cash moved or the cash counted.</summary>
    public decimal? Amount { get; private set; }

    public string? Note { get; private set; }

    internal static PosShiftEvent Create(PosShiftAction action, Guid? userId, DateTime atUtc, decimal? amount, string? note) =>
        new() { Action = action, UserId = userId, AtUtc = atUtc, Amount = amount, Note = note };
}
