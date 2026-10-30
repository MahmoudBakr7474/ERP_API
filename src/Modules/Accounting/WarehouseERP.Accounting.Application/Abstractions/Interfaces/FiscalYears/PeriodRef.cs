using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;

/// <summary>A period as postings see it: its year (whose start year numbers journals), its id and its state when read.</summary>
public sealed record PeriodRef(Guid FiscalYearId, DateOnly FiscalYearStart, Guid PeriodId, FiscalPeriodStatus Status);
