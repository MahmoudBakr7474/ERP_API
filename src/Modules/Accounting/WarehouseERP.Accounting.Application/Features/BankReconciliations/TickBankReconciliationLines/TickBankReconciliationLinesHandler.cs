using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.TickBankReconciliationLines;

// Only posted lines of this bank's ledger account dated on or before the statement that no other reconciliation holds; a
// unique index on the line decides when two reconciliations tick it at once.
public sealed class TickBankReconciliationLinesHandler(
    IBankReconciliationRepository reconciliations, ICashAccountRepository cashAccounts, ICurrentUser currentUser)
    : ICommandHandler<TickBankReconciliationLinesCommand, string>
{
    public async Task<Result<string>> HandleAsync(TickBankReconciliationLinesCommand command, CancellationToken ct)
    {
        var access = BankReconciliationAccess.Check(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var reconciliation = await reconciliations.GetForUpdateAsync(command.ReconciliationId, expected, ct);
        if (reconciliation is null)
            return BankReconciliationErrors.NotFound;
        var bank = await cashAccounts.FindAsync(reconciliation.CashAccountId, ct)
            ?? throw new InvalidOperationException($"Reconciliation {reconciliation.Id} names a missing bank account.");
        var free = await reconciliations.FreeLinesAsync(bank.AccountId, reconciliation.StatementDate, reconciliation.Id, command.JournalLineIds, ct);
        if (free.Count != command.JournalLineIds.Count)
            return BankReconciliationErrors.LineNotFree;
        var ticked = reconciliation.Tick(free.Select(l => (l.JournalLineId, l.Amount)).ToList());
        if (ticked.IsFailure)
            return ticked.Error;
        var saved = await reconciliations.SaveChangesAsync(ct);
        return saved.IsFailure ? saved.Error : Convert.ToBase64String(reconciliation.RowVersion);
    }
}
