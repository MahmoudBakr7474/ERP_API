using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.ListAccounts;

public sealed class ListAccountsHandler(IAccountReadStore accounts, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListAccountsQuery, PagedResult<AccountSummaryResponse>>
{
    public async Task<Result<PagedResult<AccountSummaryResponse>>> HandleAsync(ListAccountsQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await accounts.ListAsync(
            new AccountFilter(Text.Optional(query.Search), query.Type, query.IsActive, query.IsPosting, query.ParentId),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
