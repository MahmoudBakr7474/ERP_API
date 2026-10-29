using WarehouseERP.Pos.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// One cashier's session at one terminal, and its drawer. Opened with a counted float; while open, its cashier checks out
/// sales and refunds returns on it, and cash may be put in or taken out with a reason. Each sale adds the cash it actually
/// left in the drawer (tendered less change) and its card amount; each return the cash and card it refunded. Closing
/// compares the counted cash with the expected drawer (float + cash sales − cash refunds + cash in − cash out); a
/// difference needs a note. Card amounts never count as drawer cash. A closed shift never changes. A terminal and a
/// cashier each have at most one open shift (database-enforced).
/// </summary>
public sealed class PosShift : IAuditable
{
    public const int MaxReasonLength = 200;
    public const int MaxNoteLength = 500;

    private readonly List<PosCashMovement> _cashMovements = [];
    private readonly List<PosShiftEvent> _events = [];

    private PosShift() { } // EF Core

    public Guid Id { get; private set; }
    public string Number { get; private set; } = null!;
    public short FiscalYear { get; private set; }
    public int SequenceNumber { get; private set; }
    public Guid TerminalId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid BranchId { get; private set; }

    /// <summary>The only user who may sell on it.</summary>
    public Guid CashierUserId { get; private set; }

    public PosShiftStatus Status { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public decimal OpeningFloat { get; private set; }

    /// <summary>Cash sales left in the drawer: tendered less change.</summary>
    public decimal CashSalesAmount { get; private set; }

    public decimal CashRefundsAmount { get; private set; }
    public decimal CashInAmount { get; private set; }
    public decimal CashOutAmount { get; private set; }
    public decimal CardSalesAmount { get; private set; }
    public decimal CardRefundsAmount { get; private set; }

    /// <summary>What its sales came to, all payment methods.</summary>
    public decimal SalesAmount { get; private set; }

    /// <summary>What its returns refunded, all payment methods.</summary>
    public decimal RefundsAmount { get; private set; }

    public int SalesCount { get; private set; }
    public int ReturnsCount { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    /// <summary>True when a shift manager closed it instead of its cashier.</summary>
    public bool IsForceClosed { get; private set; }

    public decimal? CountedCash { get; private set; }

    /// <summary>The expected drawer when it closed.</summary>
    public decimal? ExpectedCashAtClose { get; private set; }

    /// <summary>Counted less expected: positive is cash over, negative cash short.</summary>
    public decimal? CashDifference { get; private set; }

    public string? ClosingNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<PosCashMovement> CashMovements => _cashMovements;
    public IReadOnlyList<PosShiftEvent> Events => _events;

    /// <summary>The cash that should be in the drawer now.</summary>
    public decimal ExpectedCash => OpeningFloat + CashSalesAmount - CashRefundsAmount + CashInAmount - CashOutAmount;

    public bool IsOpen => Status == PosShiftStatus.Open;

    public static Result<PosShift> Open(
        DocumentNumber number, PosTerminal terminal, Guid cashierUserId, decimal openingFloat, int currencyDecimals, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(terminal);
        if (!Money.IsAmount(openingFloat, currencyDecimals, allowZero: true))
            return PosShiftDomainErrors.AmountInvalid;
        var shift = new PosShift
        {
            Number = number.Number,
            FiscalYear = number.FiscalYear,
            SequenceNumber = number.SequenceNumber,
            TerminalId = terminal.Id,
            WarehouseId = terminal.WarehouseId,
            BranchId = terminal.BranchId,
            CashierUserId = cashierUserId,
            Status = PosShiftStatus.Open,
            OpenedAtUtc = nowUtc,
            OpeningFloat = openingFloat,
        };
        shift._events.Add(PosShiftEvent.Create(PosShiftAction.Opened, cashierUserId, nowUtc, openingFloat, note: null));
        return shift;
    }

    /// <summary>Adds a checked-out sale: the cash it left in the drawer, its card amount and its total.</summary>
    public Result RecordSale(decimal cashReceived, decimal cardAmount, decimal total)
    {
        if (!IsOpen)
            return PosShiftDomainErrors.NotOpen;
        CashSalesAmount += cashReceived;
        CardSalesAmount += cardAmount;
        SalesAmount += total;
        SalesCount++;
        return Result.Success();
    }

    /// <summary>Adds a refunded return; cash refunds come out of this drawer, so never more than it holds.</summary>
    public Result RecordReturn(decimal cashRefund, decimal cardRefund, decimal total)
    {
        if (!IsOpen)
            return PosShiftDomainErrors.NotOpen;
        if (cashRefund > ExpectedCash)
            return PosShiftDomainErrors.DrawerShort(ExpectedCash);
        CashRefundsAmount += cashRefund;
        CardRefundsAmount += cardRefund;
        RefundsAmount += total;
        ReturnsCount++;
        return Result.Success();
    }

    /// <summary>Records cash put into or taken out of the drawer, with a reason; no more out than the drawer holds.</summary>
    public Result MoveCash(PosCashMovementType type, decimal amount, string? reason, int currencyDecimals, Guid? userId, DateTime nowUtc)
    {
        if (!IsOpen)
            return PosShiftDomainErrors.NotOpen;
        if (!Enum.IsDefined(type))
            return PosShiftDomainErrors.CashMovementTypeInvalid;
        if (!Money.IsAmount(amount, currencyDecimals))
            return PosShiftDomainErrors.AmountInvalid;
        var why = Text.Optional(reason);
        if (why is null || why.Length > MaxReasonLength)
            return PosShiftDomainErrors.ReasonRequired;
        if (type == PosCashMovementType.CashOut && amount > ExpectedCash)
            return PosShiftDomainErrors.DrawerShort(ExpectedCash);

        if (type == PosCashMovementType.CashIn)
            CashInAmount += amount;
        else
            CashOutAmount += amount;
        _cashMovements.Add(PosCashMovement.Create(type, amount, why, userId, nowUtc));
        _events.Add(PosShiftEvent.Create(
            type == PosCashMovementType.CashIn ? PosShiftAction.CashIn : PosShiftAction.CashOut, userId, nowUtc, amount, why));
        return Result.Success();
    }

    /// <summary>
    /// Closes it with the counted cash. A difference from the expected drawer needs a note; a shift manager closing someone
    /// else's shift (<paramref name="forced"/>) always gives one.
    /// </summary>
    public Result Close(decimal countedCash, string? note, bool forced, int currencyDecimals, Guid? userId, DateTime nowUtc)
    {
        if (!IsOpen)
            return PosShiftDomainErrors.NotOpen;
        if (!Money.IsAmount(countedCash, currencyDecimals, allowZero: true))
            return PosShiftDomainErrors.AmountInvalid;
        var text = Text.Optional(note);
        if (text?.Length > MaxNoteLength)
            return PosShiftDomainErrors.NoteTooLong;
        var expected = ExpectedCash;
        if (countedCash != expected && text is null)
            return PosShiftDomainErrors.DifferenceNoteRequired(countedCash - expected);
        if (forced && text is null)
            return PosShiftDomainErrors.ForceCloseNoteRequired;

        Status = PosShiftStatus.Closed;
        ClosedAtUtc = nowUtc;
        ClosedByUserId = userId;
        IsForceClosed = forced;
        CountedCash = countedCash;
        ExpectedCashAtClose = expected;
        CashDifference = countedCash - expected;
        ClosingNote = text;
        _events.Add(PosShiftEvent.Create(forced ? PosShiftAction.ForceClosed : PosShiftAction.Closed, userId, nowUtc, countedCash, text));
        return Result.Success();
    }
}
