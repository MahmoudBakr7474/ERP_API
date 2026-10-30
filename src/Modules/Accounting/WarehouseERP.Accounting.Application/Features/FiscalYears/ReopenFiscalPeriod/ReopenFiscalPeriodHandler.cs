using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalPeriod;

// accounting.periods.reopen (the endpoint), a reason, and an open year: a closed year is reopened as a whole first. Same
// locks as a close.
public sealed partial class ReopenFiscalPeriodHandler(
    ITransactionRunner transactions, IFiscalYearRepository years, IAccountingLocks locks, ICurrentUser currentUser, TimeProvider clock,
    ILogger<ReopenFiscalPeriodHandler> logger)
    : ICommandHandler<ReopenFiscalPeriodCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(ReopenFiscalPeriodCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ReopenOnceAsync(command, token), ct);
    }

    private async Task<Result<Done>> ReopenOnceAsync(ReopenFiscalPeriodCommand command, CancellationToken ct)
    {
        if (await years.FindYearIdOfPeriodAsync(command.PeriodId, ct) is not { } yearId)
            return FiscalYearErrors.PeriodNotFound;
        if (!await locks.TryLockYearAsync(yearId, ct) || !await locks.TryLockPeriodAsync(command.PeriodId, exclusive: true, ct))
            return FiscalYearErrors.Busy;
        var year = await years.GetForUpdateAsync(yearId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Fiscal year {yearId} disappeared while it was locked.");
        var reopened = year.ReopenPeriod(command.PeriodId, command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (reopened.IsFailure)
            return reopened.Error;
        var saved = await years.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        var number = year.Periods.Single(p => p.Id == command.PeriodId).Number;
        PeriodReopened(logger, year.Name, number);
        return Done.Value;
    }

    [LoggerMessage(EventId = 9032, Level = LogLevel.Information, Message = "Fiscal year {Name} period {Number} reopened")]
    private static partial void PeriodReopened(ILogger logger, string name, int number);
}
