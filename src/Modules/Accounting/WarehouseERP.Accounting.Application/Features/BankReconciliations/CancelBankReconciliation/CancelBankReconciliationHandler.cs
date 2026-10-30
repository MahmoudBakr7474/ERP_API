using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.CancelBankReconciliation;

// A draft is dropped and its ticked lines are free again; a completed reconciliation stays.
public sealed class CancelBankReconciliationHandler(IBankReconciliationRepository reconciliations, ICurrentUser currentUser)
    : ICommandHandler<CancelBankReconciliationCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelBankReconciliationCommand command, CancellationToken ct)
    {
        var access = BankReconciliationAccess.Check(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var reconciliation = await reconciliations.GetForUpdateAsync(command.ReconciliationId, expected, ct);
        if (reconciliation is null)
            return BankReconciliationErrors.NotFound;
        var cancelled = reconciliation.Cancel();
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await reconciliations.SaveChangesAsync(ct);
        return saved.IsFailure ? saved.Error : Convert.ToBase64String(reconciliation.RowVersion);
    }
}
