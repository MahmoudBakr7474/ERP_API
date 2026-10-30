using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// Twelve monthly periods from the settings' start month. Periods close and reopen one by one (reopening needs a reason);
/// the year closes once every period is closed, with its closing journal moving revenue and expenses into retained
/// earnings, and reopening it (with a reason) reverses that journal. Every step is a <see cref="FiscalYearEvent"/>.
/// </summary>
public sealed class FiscalYear : IAuditable
{
    public const int PeriodCount = 12;
    public const int MaxNoteLength = 500;

    private readonly List<FiscalPeriod> _periods = [];
    private readonly List<FiscalYearEvent> _events = [];

    private FiscalYear() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>"2027" for a calendar year, "2026/2027" for a year starting in another month.</summary>
    public string Name { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FiscalYearStatus Status { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    /// <summary>The posted closing journal of the current close; null while open or when there was nothing to close.</summary>
    public Guid? ClosingJournalId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyList<FiscalPeriod> Periods => _periods;
    public IReadOnlyList<FiscalYearEvent> Events => _events;

    public static Result<FiscalYear> Create(DateOnly startDate, Guid? userId, DateTime nowUtc)
    {
        if (startDate.Day != 1)
            return FiscalYearDomainErrors.StartNotMonthStart;
        var year = new FiscalYear
        {
            StartDate = startDate,
            EndDate = startDate.AddYears(1).AddDays(-1),
            Name = startDate.Month == 1 ? $"{startDate.Year}" : $"{startDate.Year}/{startDate.Year + 1}",
            Status = FiscalYearStatus.Open,
        };
        for (var i = 0; i < PeriodCount; i++)
        {
            var start = startDate.AddMonths(i);
            year._periods.Add(FiscalPeriod.Create(i + 1, start, start.AddMonths(1).AddDays(-1)));
        }
        year.Record(FiscalYearAction.Created, null, userId, nowUtc, null);
        return year;
    }

    public bool IsClosed => Status == FiscalYearStatus.Closed;

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    public FiscalPeriod? PeriodOf(DateOnly date) => _periods.SingleOrDefault(p => p.Contains(date));

    public FiscalPeriod? Period(int number) => _periods.SingleOrDefault(p => p.Number == number);

    public Result ClosePeriod(Guid periodId, Guid? userId, DateTime nowUtc)
    {
        var period = _periods.SingleOrDefault(p => p.Id == periodId) ?? throw new InvalidOperationException($"Period {periodId} is not in year {Id}.");
        if (!period.IsOpen)
            return FiscalYearDomainErrors.PeriodNotOpen;
        period.Close(userId, nowUtc);
        Record(FiscalYearAction.PeriodClosed, period.Number, userId, nowUtc, null);
        return Result.Success();
    }

    public Result ReopenPeriod(Guid periodId, string reason, Guid? userId, DateTime nowUtc)
    {
        var period = _periods.SingleOrDefault(p => p.Id == periodId) ?? throw new InvalidOperationException($"Period {periodId} is not in year {Id}.");
        var note = Note(reason);
        if (note.IsFailure)
            return note.Error;
        if (IsClosed)
            return FiscalYearDomainErrors.YearClosed;
        if (period.IsOpen)
            return FiscalYearDomainErrors.PeriodNotClosed;
        period.Reopen();
        Record(FiscalYearAction.PeriodReopened, period.Number, userId, nowUtc, note.Value);
        return Result.Success();
    }

    /// <summary>Closes the year once every period is; the closing journal (null when nothing had a balance) was posted with it.</summary>
    public Result Close(Guid? closingJournalId, string? closingJournalNumber, Guid? userId, DateTime nowUtc)
    {
        if (IsClosed)
            return FiscalYearDomainErrors.YearClosed;
        if (_periods.Any(p => p.IsOpen))
            return FiscalYearDomainErrors.PeriodsStillOpen;
        Status = FiscalYearStatus.Closed;
        ClosedAtUtc = nowUtc;
        ClosedByUserId = userId;
        ClosingJournalId = closingJournalId;
        Record(FiscalYearAction.Closed, null, userId, nowUtc, closingJournalNumber);
        return Result.Success();
    }

    /// <summary>Reopens a closed year; the caller reversed its closing journal in the same transaction. Periods stay closed.</summary>
    public Result Reopen(string reason, Guid? userId, DateTime nowUtc)
    {
        var note = Note(reason);
        if (note.IsFailure)
            return note.Error;
        if (!IsClosed)
            return FiscalYearDomainErrors.YearNotClosed;
        Status = FiscalYearStatus.Open;
        ClosedAtUtc = null;
        ClosedByUserId = null;
        ClosingJournalId = null;
        Record(FiscalYearAction.Reopened, null, userId, nowUtc, note.Value);
        return Result.Success();
    }

    private static Result<string> Note(string reason)
    {
        var note = (reason ?? "").Trim();
        return note.Length is 0 or > MaxNoteLength ? FiscalYearDomainErrors.ReasonRequired : note;
    }

    private void Record(FiscalYearAction action, int? periodNumber, Guid? userId, DateTime nowUtc, string? note) =>
        _events.Add(FiscalYearEvent.Create(action, periodNumber, userId, nowUtc, note));
}
