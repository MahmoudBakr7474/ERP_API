namespace WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalPeriod;

/// <param name="Reason">Why it reopens; kept in the year's history.</param>
public sealed record ReopenFiscalPeriodCommand(Guid PeriodId, string Reason);
