using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.GetAccountTree;

// The chart is small (hundreds of accounts), so the tree view and account pickers load it whole.
public sealed class GetAccountTreeHandler(IAccountReadStore accounts, ICurrentUser currentUser)
    : IQueryHandler<GetAccountTreeQuery, IReadOnlyList<AccountSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<AccountSummaryResponse>>> HandleAsync(GetAccountTreeQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return Result.Success(await accounts.ListAllAsync(ct));
    }
}
