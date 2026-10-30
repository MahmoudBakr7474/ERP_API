using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.FiscalYears;

public sealed record FiscalYearEventResponse(
    FiscalYearAction Action, int? PeriodNumber, Guid? UserId, string? UserName, DateTime AtUtc, string? Note);
