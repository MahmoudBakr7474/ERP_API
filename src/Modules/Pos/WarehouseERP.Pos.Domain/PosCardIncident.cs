using WarehouseERP.Pos.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// A card charge POS could not match to a sale: the cashier confirmed card payments on a standalone card terminal, then the
/// checkout was refused (for example the last unit was sold at another till) and rolled back. The charge on the card
/// terminal is outside the database transaction, so POS keeps its references here, recorded on its own after the rollback,
/// until someone reverses it on the card terminal and resolves it. One per checkout Idempotency-Key: a retry that records
/// the sale after all marks it so.
/// </summary>
public sealed class PosCardIncident
{
    public const int MaxReferencesLength = 500;
    public const int MaxErrorLength = 1000;
    public const int MaxNoteLength = 500;

    private PosCardIncident() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>The checkout's Idempotency-Key; unique.</summary>
    public Guid IdempotencyKey { get; private set; }

    public Guid TerminalId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid? ShiftId { get; private set; }
    public Guid? CashierUserId { get; private set; }

    /// <summary>The card payments of the refused checkout.</summary>
    public decimal CardAmount { get; private set; }

    /// <summary>Their references (and last four digits when given), as entered.</summary>
    public string References { get; private set; } = null!;

    /// <summary>Why the checkout was refused (the last attempt's error).</summary>
    public string ErrorCode { get; private set; } = null!;

    public string ErrorMessage { get; private set; } = null!;
    public PosCardIncidentStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime LastAttemptAtUtc { get; private set; }

    /// <summary>The sale a later retry recorded.</summary>
    public Guid? SaleId { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNote { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PosCardIncident Record(
        Guid idempotencyKey, PosTerminal terminal, Guid? shiftId, Guid? cashierUserId, decimal cardAmount, string references,
        string errorCode, string errorMessage, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        var incident = new PosCardIncident
        {
            IdempotencyKey = idempotencyKey,
            TerminalId = terminal.Id,
            WarehouseId = terminal.WarehouseId,
            BranchId = terminal.BranchId,
            ShiftId = shiftId,
            CashierUserId = cashierUserId,
            Status = PosCardIncidentStatus.ReversalRequired,
            OccurredAtUtc = nowUtc,
        };
        incident.Again(cardAmount, references, errorCode, errorMessage, nowUtc);
        return incident;
    }

    /// <summary>Another refused attempt with the same key; nothing changes once the sale was recorded or it was resolved.</summary>
    public void Again(decimal cardAmount, string references, string errorCode, string errorMessage, DateTime nowUtc)
    {
        if (Status != PosCardIncidentStatus.ReversalRequired)
            return;
        CardAmount = cardAmount;
        References = Clip(references, MaxReferencesLength);
        ErrorCode = Clip(errorCode, 100);
        ErrorMessage = Clip(errorMessage, MaxErrorLength);
        Attempts++;
        LastAttemptAtUtc = nowUtc;
    }

    /// <summary>A retry with the same key recorded the sale: the card charge is matched.</summary>
    public void MarkSaleRecorded(Guid saleId, DateTime nowUtc)
    {
        if (Status != PosCardIncidentStatus.ReversalRequired)
            return;
        Status = PosCardIncidentStatus.SaleRecorded;
        SaleId = saleId;
        ResolvedAtUtc = nowUtc;
    }

    /// <summary>Someone confirmed the charge was reversed or otherwise settled, and says how.</summary>
    public Result Resolve(string? note, Guid? userId, DateTime nowUtc)
    {
        if (Status != PosCardIncidentStatus.ReversalRequired)
            return PosCardIncidentDomainErrors.NotOpen;
        var text = Text.Optional(note);
        if (text is null || text.Length > MaxNoteLength)
            return PosCardIncidentDomainErrors.NoteRequired;
        Status = PosCardIncidentStatus.Resolved;
        ResolvedAtUtc = nowUtc;
        ResolvedByUserId = userId;
        ResolutionNote = text;
        return Result.Success();
    }

    private static string Clip(string value, int length) => value.Length <= length ? value : value[..length];
}
