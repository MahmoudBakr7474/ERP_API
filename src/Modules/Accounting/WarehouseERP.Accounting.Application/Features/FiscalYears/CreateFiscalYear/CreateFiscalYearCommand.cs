namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CreateFiscalYear;

/// <param name="StartYear">The calendar year the fiscal year starts in; the month comes from the settings.</param>
public sealed record CreateFiscalYearCommand(int StartYear);
