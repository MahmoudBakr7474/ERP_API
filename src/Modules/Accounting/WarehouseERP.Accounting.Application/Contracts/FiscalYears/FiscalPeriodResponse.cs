using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.FiscalYears;

public sealed record FiscalPeriodResponse(
    Guid Id, int Number, DateOnly StartDate, DateOnly EndDate, FiscalPeriodStatus Status, DateTime? ClosedAtUtc, Guid? ClosedByUserId);
