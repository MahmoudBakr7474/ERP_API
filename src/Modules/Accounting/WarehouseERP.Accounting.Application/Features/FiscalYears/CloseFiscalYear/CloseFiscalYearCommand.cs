namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalYear;

public sealed record CloseFiscalYearCommand(Guid FiscalYearId, string RowVersion);
