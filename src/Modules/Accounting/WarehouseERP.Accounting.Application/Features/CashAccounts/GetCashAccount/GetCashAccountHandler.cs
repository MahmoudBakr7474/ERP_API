using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Contracts.CashAccounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.GetCashAccount;

public sealed class GetCashAccountHandler(ICashAccountReadStore cashAccounts, ICurrentUser currentUser)
    : IQueryHandler<GetCashAccountQuery, CashAccountResponse>
{
    public async Task<Result<CashAccountResponse>> HandleAsync(GetCashAccountQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await cashAccounts.GetAsync(query.CashAccountId, ct) is { } cash ? cash : CashAccountErrors.NotFound;
    }
}
