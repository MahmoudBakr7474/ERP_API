using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalPeriod;

// One transaction: the year's lock, then the period's lock exclusively, so a posting into the period is either committed
// before the close or refused after it. Drafts dated in the period are counted, not blocked: they wait for a reopen or a
// new date.
public sealed partial class CloseFiscalPeriodHandler(
    ITransactionRunner transactions, IFiscalYearRepository years, IJournalRepository journals, IAccountingLocks locks,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CloseFiscalPeriodHandler> logger)
    : ICommandHandler<CloseFiscalPeriodCommand, FiscalPeriodClosedResponse>
{
    public async Task<Result<FiscalPeriodClosedResponse>> HandleAsync(CloseFiscalPeriodCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => CloseOnceAsync(command.PeriodId, token), ct);
    }

    private async Task<Result<FiscalPeriodClosedResponse>> CloseOnceAsync(Guid periodId, CancellationToken ct)
    {
        if (await years.FindYearIdOfPeriodAsync(periodId, ct) is not { } yearId)
            return FiscalYearErrors.PeriodNotFound;
        if (!await locks.TryLockYearAsync(yearId, ct) || !await locks.TryLockPeriodAsync(periodId, exclusive: true, ct))
            return FiscalYearErrors.Busy;
        var year = await years.GetForUpdateAsync(yearId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Fiscal year {yearId} disappeared while it was locked.");
        var closed = year.ClosePeriod(periodId, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (closed.IsFailure)
            return closed.Error;
        var saved = await years.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        var period = year.Periods.Single(p => p.Id == periodId);
        var open = await journals.CountOpenAsync(period.StartDate, period.EndDate, ct);
        PeriodClosed(logger, year.Name, period.Number, open);
        return new FiscalPeriodClosedResponse(periodId, open);
    }

    [LoggerMessage(EventId = 9031, Level = LogLevel.Information,
        Message = "Fiscal year {Name} period {Number} closed; {OpenJournals} unposted journals are dated in it")]
    private static partial void PeriodClosed(ILogger logger, string name, int number, int openJournals);
}
