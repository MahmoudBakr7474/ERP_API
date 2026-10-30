namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalYear;

/// <param name="ClosingJournalId">The posted closing journal; null when no revenue or expense account had a balance.</param>
/// <param name="NetProfit">Revenue minus expenses of the year (negative for a loss), moved into retained earnings.</param>
public sealed record FiscalYearClosedResponse(
    Guid FiscalYearId, Guid? ClosingJournalId, string? ClosingJournalNumber, decimal NetProfit, string RowVersion);
