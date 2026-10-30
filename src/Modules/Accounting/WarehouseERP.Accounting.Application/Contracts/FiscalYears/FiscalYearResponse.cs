using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.FiscalYears;

/// <summary>A fiscal year with its twelve periods and its history.</summary>
public sealed record FiscalYearResponse(
    Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, FiscalYearStatus Status, DateTime? ClosedAtUtc, Guid? ClosedByUserId,
    Guid? ClosingJournalId, string? ClosingJournalNumber, IReadOnlyList<FiscalPeriodResponse> Periods,
    IReadOnlyList<FiscalYearEventResponse> Events, string RowVersion);
