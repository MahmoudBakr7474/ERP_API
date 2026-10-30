using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Contracts.CashAccounts;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.ListCashAccounts;

// A company has a handful of cashboxes and banks, so they come whole, each with its ledger account's posted balance.
public sealed class ListCashAccountsHandler(ICashAccountReadStore cashAccounts, ICurrentUser currentUser)
    : IQueryHandler<ListCashAccountsQuery, IReadOnlyList<CashAccountResponse>>
{
    public async Task<Result<IReadOnlyList<CashAccountResponse>>> HandleAsync(ListCashAccountsQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return Result.Success(await cashAccounts.ListAsync(query.Kind, query.BranchId, query.IsActive, query.AsOfDate, ct));
    }
}
