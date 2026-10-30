using WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;
using WarehouseERP.Accounting.Application.Contracts.BankReconciliations;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations.ListBankReconciliations;

public sealed class ListBankReconciliationsHandler(IBankReconciliationReadStore reconciliations, ICurrentUser currentUser)
    : IQueryHandler<ListBankReconciliationsQuery, IReadOnlyList<BankReconciliationSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<BankReconciliationSummaryResponse>>> HandleAsync(ListBankReconciliationsQuery query, CancellationToken ct)
    {
        var access = BankReconciliationAccess.Check(currentUser);
        if (access.IsFailure)
            return access.Error;
        return Result.Success(await reconciliations.ListAsync(query.CashAccountId, ct));
    }
}
