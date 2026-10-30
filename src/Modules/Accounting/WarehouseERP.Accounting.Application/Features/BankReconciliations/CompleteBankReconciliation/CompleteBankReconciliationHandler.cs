using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.CompleteBankReconciliation;

// Under the bank account's lock: the balance earlier reconciliations cleared plus the ticked lines must equal the
// statement exactly; differences (bank charges, interest) are posted and ticked first, never absorbed.
public sealed partial class CompleteBankReconciliationHandler(
    ITransactionRunner transactions, IBankReconciliationRepository reconciliations, IAccountingLocks locks, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CompleteBankReconciliationHandler> logger)
    : ICommandHandler<CompleteBankReconciliationCommand, string>
{
    public async Task<Result<string>> HandleAsync(CompleteBankReconciliationCommand command, CancellationToken ct)
    {
        var access = BankReconciliationAccess.Check(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        return await transactions.RunAsync(token => CompleteOnceAsync(command.ReconciliationId, expected, token), ct);
    }

    private async Task<Result<string>> CompleteOnceAsync(Guid reconciliationId, byte[] expected, CancellationToken ct)
    {
        var reconciliation = await reconciliations.GetForUpdateAsync(reconciliationId, expected, ct);
        if (reconciliation is null)
            return BankReconciliationErrors.NotFound;
        if (!await locks.TryLockCashAccountAsync(reconciliation.CashAccountId, ct))
            return JournalErrors.Busy;
        var opening = await reconciliations.ClearedBalanceAsync(reconciliation.CashAccountId, ct);
        var completed = reconciliation.Complete(opening, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (completed.IsFailure)
            return completed.Error;
        var saved = await reconciliations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        Completed(logger, reconciliation.Id, reconciliation.StatementDate, reconciliation.StatementBalance);
        return Convert.ToBase64String(reconciliation.RowVersion);
    }

    [LoggerMessage(EventId = 9081, Level = LogLevel.Information, Message = "Bank reconciliation {ReconciliationId} for {StatementDate} completed at {Balance}")]
    private static partial void Completed(ILogger logger, Guid reconciliationId, DateOnly statementDate, decimal balance);
}
