using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>One month of a fiscal year. Journals post only into an open period whose dates hold their posting date.</summary>
public sealed class FiscalPeriod
{
    private FiscalPeriod() { } // EF Core

    public Guid Id { get; private set; }
    public Guid FiscalYearId { get; private set; }

    /// <summary>1 to 12 within its year.</summary>
    public int Number { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FiscalPeriodStatus Status { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    public bool IsOpen => Status == FiscalPeriodStatus.Open;

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    internal static FiscalPeriod Create(int number, DateOnly startDate, DateOnly endDate) =>
        new() { Number = number, StartDate = startDate, EndDate = endDate, Status = FiscalPeriodStatus.Open };

    internal void Close(Guid? userId, DateTime nowUtc)
    {
        Status = FiscalPeriodStatus.Closed;
        ClosedAtUtc = nowUtc;
        ClosedByUserId = userId;
    }

    internal void Reopen()
    {
        Status = FiscalPeriodStatus.Open;
        ClosedAtUtc = null;
        ClosedByUserId = null;
    }
}
