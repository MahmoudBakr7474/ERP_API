namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalPeriod;

/// <param name="OpenJournals">Drafts and submitted journals still dated in the period: they can't post until it reopens or they are redated.</param>
/// <param name="UnpostedDocuments">Operational documents dated in the period whose journal is still pending or failed: they wait in the queue until it reopens.</param>
public sealed record FiscalPeriodClosedResponse(Guid PeriodId, int OpenJournals, int UnpostedDocuments);
