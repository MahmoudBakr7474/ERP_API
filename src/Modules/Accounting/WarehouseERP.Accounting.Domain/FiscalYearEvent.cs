using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>One step in a fiscal year's history: who closed or reopened what, when and why. Never changes.</summary>
public sealed class FiscalYearEvent
{
    private FiscalYearEvent() { } // EF Core

    public long Id { get; private set; }
    public Guid FiscalYearId { get; private set; }
    public FiscalYearAction Action { get; private set; }

    /// <summary>The period it concerns; null for the year itself.</summary>
    public int? PeriodNumber { get; private set; }

    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? Note { get; private set; }

    internal static FiscalYearEvent Create(FiscalYearAction action, int? periodNumber, Guid? userId, DateTime atUtc, string? note) =>
        new() { Action = action, PeriodNumber = periodNumber, UserId = userId, AtUtc = atUtc, Note = note };
}
