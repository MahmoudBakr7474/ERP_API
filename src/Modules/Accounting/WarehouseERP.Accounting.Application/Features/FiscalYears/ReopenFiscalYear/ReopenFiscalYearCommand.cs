namespace WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalYear;

/// <param name="Reason">Why it reopens; kept in the year's history.</param>
public sealed record ReopenFiscalYearCommand(Guid FiscalYearId, string Reason, string RowVersion);
