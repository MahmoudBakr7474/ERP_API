namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalPeriod;

/// <param name="OpenJournals">Drafts and submitted journals still dated in the period: they can't post until it reopens or they are redated.</param>
public sealed record FiscalPeriodClosedResponse(Guid PeriodId, int OpenJournals);
