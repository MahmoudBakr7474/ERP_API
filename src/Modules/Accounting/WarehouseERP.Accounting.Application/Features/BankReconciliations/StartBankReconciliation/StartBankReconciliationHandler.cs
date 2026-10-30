using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.StartBankReconciliation;

// One draft per bank account at a time (a filtered unique index decides races), dated on or after the last completed one.
public sealed partial class StartBankReconciliationHandler(
    IBankReconciliationRepository reconciliations, ICashAccountRepository cashAccounts, ICurrentUser currentUser,
    ILogger<StartBankReconciliationHandler> logger)
    : ICommandHandler<StartBankReconciliationCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(StartBankReconciliationCommand command, CancellationToken ct)
    {
        var access = BankReconciliationAccess.Check(currentUser);
        if (access.IsFailure)
            return access.Error;
        var bank = await cashAccounts.FindAsync(command.CashAccountId, ct);
        if (bank is null)
            return CashAccountErrors.NotFound;
        if (bank.Kind != CashAccountKind.Bank)
            return BankReconciliationErrors.NotBank;
        if (await reconciliations.HasDraftAsync(bank.Id, ct))
            return BankReconciliationErrors.DraftExists;
        if (await reconciliations.LastCompletedStatementDateAsync(bank.Id, ct) is { } last && command.StatementDate < last)
            return BankReconciliationErrors.BeforeLastStatement;
        var reconciliation = BankReconciliation.Start(bank.Id, command.StatementDate, command.StatementBalance);
        reconciliations.Add(reconciliation);
        var saved = await reconciliations.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        Started(logger, reconciliation.Id, bank.Code, command.StatementDate);
        return reconciliation.Id;
    }

    [LoggerMessage(EventId = 9080, Level = LogLevel.Information, Message = "Bank reconciliation {ReconciliationId} of {Code} started for {StatementDate}")]
    private static partial void Started(ILogger logger, Guid reconciliationId, string code, DateOnly statementDate);
}
