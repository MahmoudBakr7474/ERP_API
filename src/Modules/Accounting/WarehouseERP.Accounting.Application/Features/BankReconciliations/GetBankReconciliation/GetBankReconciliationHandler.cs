using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Contracts.BankReconciliations;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.GetBankReconciliation;

public sealed class GetBankReconciliationHandler(IBankReconciliationReadStore reconciliations, ICurrentUser currentUser)
    : IQueryHandler<GetBankReconciliationQuery, BankReconciliationResponse>
{
    public async Task<Result<BankReconciliationResponse>> HandleAsync(GetBankReconciliationQuery query, CancellationToken ct)
    {
        var access = BankReconciliationAccess.Check(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await reconciliations.GetAsync(query.ReconciliationId, ct) is { } reconciliation ? reconciliation : BankReconciliationErrors.NotFound;
    }
}
